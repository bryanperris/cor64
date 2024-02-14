using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NLog;

/* This class provides 2 ways of mapping memory
   StaticMap: Fixed mapping at construction time of memory devices (eg: MMIO register)
   DynamicMap: Dynamically mapping memory at runtime (eg: cartridge)

   Dynamic maps cannot override static mappings
*/

namespace cor64.IO
{
    public abstract unsafe class N64MemoryDevice : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private N64MemoryController m_ParentController;
        protected delegate N64AlignedPtr GetPtr(int offset);
        private static readonly UnmanagedBuffer DUMMY_READ = new(4);
        private static readonly UnmanagedBuffer DUMMY_WRITE = new(4);
        private readonly int m_EntryCount;
        private readonly nint[] m_ReadTable, m_WriteTable;
        private readonly Action[] m_ReadNotify, m_WriteNotify;
        private int m_BuilderOffset;
        private readonly GetPtr m_GetReadPtrFunc;
        private readonly GetPtr m_GetWritePtrFunc;
        private MemMappedBuffer[] m_MappedDevices;
        private readonly bool[] m_IsPreReadMapped;
        private readonly bool[] m_IsPreWriteMapped;
        private bool m_Initialized = false;


        protected struct N64AlignedPtr {
            internal IntPtr? Pointer;
            internal Action NotifyHandler;

            public readonly static N64AlignedPtr DUMMY = new() { Pointer = null, NotifyHandler = null };

            public static N64AlignedPtr FromPtr(IntPtr ptr) {
                return new N64AlignedPtr {
                    Pointer = ptr,
                    NotifyHandler = null
                };
            }

            public static N64AlignedPtr FromNotifyingPtr(IntPtr ptr, Action notifyHandler) {
                return new N64AlignedPtr {
                    Pointer = ptr,
                    NotifyHandler = notifyHandler
                };
            }
        }

        protected N64MemoryDevice(N64MemoryController parentController, int size) {
            Size = size;
            m_ParentController = parentController;
            m_EntryCount = size / 4;
            m_ReadTable = new nint[size / 4];
            m_WriteTable = new nint[size / 4];
            m_ReadNotify = new Action[size / 4];
            m_WriteNotify = new Action[size / 4];
            m_IsPreReadMapped = new bool[size / 4];
            m_IsPreWriteMapped = new bool[size / 4];

            for (int i = 0; i < m_EntryCount; i++) {
                m_ReadTable[i] = DUMMY_READ.GetPointer();
                m_WriteTable[i] = DUMMY_WRITE.GetPointer();
            }
        }

        public int Size { get; protected set; }

        public void Init() {
            if (m_Initialized) {
                throw new InvalidOperationException("Already initialized");
            }

            m_Initialized = true;

            // Populate entries that are dynamically resolved
            for (int i = 0; i < m_EntryCount; i++) {
                if (!m_IsPreReadMapped[i]) {
                    var readResult = DynamicReadMap(i * 4);
                    if (readResult.Pointer != null) {
                        m_ReadNotify[i] = readResult.NotifyHandler;
                        m_ReadTable[i] = readResult.Pointer.Value;
                    }
                }

                if (!m_IsPreWriteMapped[i]) {
                    var writeResult = DynamicWriteMap(i * 4);
                    if (writeResult.Pointer != null) {
                        m_WriteNotify[i] = writeResult.NotifyHandler;
                        m_WriteTable[i] = writeResult.Pointer.Value;
                    }
                }
            }
        }

        private static void Fill<T>(T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        protected unsafe void StaticMap(int size) {
            m_BuilderOffset += size;
        }

        // Used for constructing pre-mapped memory
        protected unsafe void StaticMap(params MemMappedBuffer[] devices)
        {
            if (m_Initialized) {
                throw new InvalidOperationException("Cannot map statically after runtime inititialization");
            }

            m_MappedDevices = new MemMappedBuffer[devices.Length];
            Array.Copy(devices, m_MappedDevices, m_MappedDevices.Length);

            for (int i = 0; i < devices.Length; i++)
            {
                var device = devices[i];

                if (device == null)
                {
                    ArgumentNullException argumentNullException = new(paramName: nameof(device));
                    throw argumentNullException;
                }

                if (device.Size % 4 != 0)
                {
                    throw new ArgumentException("size must be word aligned");
                }

                var off = (int)m_BuilderOffset / 4;

                for (int j = 0; j < (device.Size / 4); j++)
                {
                    int ind = off + j;
                    int ptrOff = j * 4;

                    if (device.CanRead) {
                        m_ReadTable[ind] = device.ReadPtr + ptrOff;
                        m_ReadNotify[ind] = device.ReadNotify;
                        m_IsPreReadMapped[ind] = true;
                    }

                    if (device.CanWrite) {
                        m_WriteTable[ind] = device.WritePtr + ptrOff;
                        m_WriteNotify[ind] = device.WriteNotify;
                        m_IsPreWriteMapped[ind] = true;
                    }

                }

                m_BuilderOffset += device.Size;
            }
        }

        public bool Initialized => m_Initialized;

        protected N64MemoryController ParentController => m_ParentController;

        [Conditional("DEBUG_MEMORY_DEVICE_ACCESS")]
        private void DebugWrite(int offset, dynamic v) {
            // // if (offset == 0x0032B330) {
            //     Console.WriteLine("N64 Physical Write: {0:X8} {1:X16}", offset, v);
            // // }
        }

        [Conditional("N64_MEMORY_BOUNDS_CHECK")]
        private void CheckReadBounds(int offset) {
            if (offset >= m_ReadTable.Length) {
                throw new InvalidOperationException($"Bounds out of range: {Name} offset={offset} length={m_ReadTable.Length}");
            }
        }

        [Conditional("N64_MEMORY_BOUNDS_CHECK"), Conditional("TESTING")]
        private void CheckWriteBounds(int offset) {
            if (offset >= m_WriteTable.Length) {
                throw new InvalidOperationException($"Bounds out of range: {Name} offset={offset} length={m_WriteTable.Length}");
            }
        }

        private nint ReadPtr8(int offset) {
            int index = offset >> 2;
            CheckReadBounds(index);
            return m_ReadTable[index] + (offset & 3);
        }

        private nint WritePtr8(int offset) {
            int index = offset >> 2;
            CheckWriteBounds(index);
            return m_ReadTable[index] + (offset & 3);
        }

        private nint ReadPtr16(int offset) {
            int index = offset >> 2;
            CheckReadBounds(index);
            return m_ReadTable[index] + (offset & 3);
        }

        private nint WritePtr16(int offset) {
            int index = offset >> 2;
            CheckWriteBounds(index);
            return m_ReadTable[index] + (offset & 3);
        }

        private nint ReadPtr32(int offset) {
            int index = offset >> 2;
            CheckReadBounds(index);
            return m_ReadTable[index];
        }

        private nint WritePtr32(int offset) {
            int index = offset >> 2;
            CheckWriteBounds(index);
            return m_WriteTable[index];
        }

        public void Read(int address, byte[] buffer, int offset, int count) {
            for (int i = 0; i < count; i++) {
                int addr = address + i;
                if (addr < Size)
                    buffer[i + offset] = Read8(addr);
                else
                    buffer[i + offset] = 0;
            }
        }

        public byte Read8(int offset) => ReadPtr8(offset).AsType_8();
        public ushort Read16(int offset) => ReadPtr16(offset).AsType_16();
        public uint Read32(int offset) => ReadPtr32(offset).AsType_32();

        public ulong Read64(int offset) {
            uint first = ReadPtr32(offset + 0).Offset(offset & 7).AsType_32();
            uint second = ReadPtr32(offset + 4).Offset(offset & 7).AsType_32();
            return N64Endianess.U64From32(first, second);
        }

        public void Write8(int offset, byte value) {
            DebugWrite(offset, value);
            WritePtr8(offset).AsType_8(value);
        }

        public void Write16(int offset, ushort value) {
            DebugWrite(offset, value);
            WritePtr16(offset).AsType_16(value);
        }

        public void Write32(int offset, uint value) {
            DebugWrite(offset, value);
            WritePtr32(offset).AsType_32(value);
        }

        public void Write64(int offset, ulong value) {
            DebugWrite(offset, value);
            var u64 = N64Endianess.U64To32(value);
            WritePtr32(offset + 0).AsType_32(u64.Item1);
            WritePtr32(offset + 4).AsType_32(u64.Item2);
        }

        public void Write(int address, byte[] buffer, int offset, int count) {
            for (int i = 0; i < count; i++) {
                int addr = address + i;
                if (addr < Size) 
                    Write8(addr, buffer[i + offset]);
                else
                    break;
            }
        }

        public void DispatchReadNotifications(int offset, int count) {
            // for (int i = 0; i < count; i++) {
            //     int addr = offset + i;

            //     m_ReadNotify[addr / 4]?.Invoke();

            //     if ((i + 4) <= count) {
            //         i+=3;
            //     }
            //     else {
            //         break;
            //     }
            // }
        }

        public void DispatchWriteNotifications(int offset, int count) {
            // bool print = false;
            // if (this.ToString() == "cor64.RCP.SPInterface" && offset >= 0x40000) {
            //     Console.WriteLine("SP write notify: {0}", s_MemDebugger.GetMemName(0x04000000U + (uint)offset));
            //     print = true;
            // }

            int index = offset / 4;

            if (count == 8) {
                m_WriteNotify[index]?.Invoke();
                m_WriteNotify[index + 1]?.Invoke();
            }
            else {
                m_WriteNotify[index]?.Invoke();
            }
        }

        public void WriteNotify(int offset)
        {
            m_WriteNotify[offset / 4]?.Invoke();
        }

        public void ReadNotify(int offset) {
            m_ReadNotify[offset / 4]?.Invoke();
        }

        public virtual void AttachDma() {
            
        }

        // WIP: To remove all of this after switching to the DMA engine
        public uint SourceAddress { get; protected set; }

        public uint DestAddress { get; protected set; }

        protected virtual N64AlignedPtr DynamicReadMap(int offset) {
            return N64AlignedPtr.DUMMY;
        }

        protected virtual N64AlignedPtr DynamicWriteMap(int offset) {
            return N64AlignedPtr.DUMMY;
        }

        public abstract String Name { get; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BlockDevice() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
