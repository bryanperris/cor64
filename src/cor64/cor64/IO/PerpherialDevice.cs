using cor64.Debugging;
using System;
using System.Collections.Generic;
using System.IO;

namespace cor64.IO
{
    public abstract class PerpherialDevice : DmaBlockDevice
    {
        private Dictionary<uint, MemMappedBuffer> m_MemLookupTable = new Dictionary<uint, MemMappedBuffer>();
        private uint m_BuilderOffset;
        private int m_Size;

        protected PerpherialDevice(N64MemoryController controller, int size) : base(controller)
        {
            m_Size = size;
        }

        protected void PrintOffset()
        {
            Console.WriteLine(m_BuilderOffset.ToString("X8"));
        }

        protected void AppendDevice(params MemMappedBuffer[] devices)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                AppendDevice(devices[i]);
            }
        }

        protected void AppendDevice(MemMappedBuffer device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            device.TaggedAddress = m_BuilderOffset;

            for (int i = 0; i < device.Size; i++)
            {
                m_MemLookupTable.Add(m_BuilderOffset++, device);
            }
        }

        protected void AppendUnused(int size)
        {
            for (int i = 0; i < size; i++)
            {
                m_MemLookupTable.Add(m_BuilderOffset++, null);
            }
        }

        protected override void Read(long position, byte[] buffer, int offset, int count)
        {
            MemMappedBuffer currentDevice = null;
            uint localOffset = (uint)position;

            for (int i = 0; i < count; i++)
            {
                if (currentDevice == null || localOffset < currentDevice.TaggedAddress || localOffset > (currentDevice.TaggedAddress + currentDevice.Size))
                {
                    /* Look it up */
                    if (m_MemLookupTable.ContainsKey(localOffset))
                    {
                        var device = m_MemLookupTable[localOffset];

                        if (device == null)
                        {
                            localOffset++;
                            continue;
                        }

                        currentDevice = device;
                    }
                }

                buffer[offset + i] = currentDevice.ReadByte((int)(localOffset - currentDevice.TaggedAddress));
                localOffset++;
            }
        }

        protected override void Write(long position, byte[] buffer, int offset, int count)
        {
            MemMappedBuffer currentDevice = null;
            uint localOffset = (uint)position;

            for (int i = 0; i < count; i++)
            {
                if (currentDevice == null || localOffset < currentDevice.TaggedAddress || localOffset > (currentDevice.TaggedAddress + currentDevice.Size))
                {
                    if (currentDevice != null)
                    {
                        currentDevice.NotifyCPUWrite();
                    }

                    /* Look it up */
                    if (m_MemLookupTable.ContainsKey(localOffset))
                    {
                        var device = m_MemLookupTable[localOffset];

                        if (device == null)
                        {
                            localOffset++;
                            continue;
                        }

                        currentDevice = device;
                    }
                }

                currentDevice.WriteByte((int)(localOffset - currentDevice.TaggedAddress), buffer[offset + i]);
                localOffset++;
            }

            if (currentDevice != null)
            {
                currentDevice.NotifyCPUWrite();
            }
        }

        public override long Size => m_Size;
    }

    /* Base class for device IO functions mounted to memory */
    abstract class MemFunction {
        private int m_Id;
        private int m_Size;

        protected MemFunction(int id, int size) {
            m_Id = id;
            m_Size = size;
        }

        public abstract byte ReadByte(int offset);

        public abstract void WriteByte(int offset, byte value);

        public int Size => m_Size;

        public int Id => m_Id;
    }

    class BufferMemFunction : MemFunction {
        private byte[] m_Buffer;

        public BufferMemFunction(int id, int size) : base(id, size)
        {
            m_Buffer = new byte[size];
        }

        public override byte ReadByte(int offset)
        {
            return m_Buffer[offset];
        }

        public override void WriteByte(int offset, byte value)
        {
            m_Buffer[offset] = value;
        }
    }

    class RegisterMemfunction : MemFunction {
        private PinnedBuffer m_RegStorageA;
        private PinnedBuffer m_RegStorageB;

        public RegisterMemfunction(int id, int size, bool seperateMaps) : base(id, size) {
            m_RegStorageA = new PinnedBuffer(size);

            if (seperateMaps) {
                m_RegStorageB = new PinnedBuffer(size);
            }
        }

        public override byte ReadByte(int offset)
        {
            return m_RegStorageA[offset];
        }

        public override void WriteByte(int offset, byte value)
        {
            var pinned = m_RegStorageB != null ? m_RegStorageB : m_RegStorageA;
            pinned[offset] = value;
        }

        public IntPtr GetStorageAPtr() => m_RegStorageA.GetPointer();

        public IntPtr GetStorageBPtr() => m_RegStorageB.GetPointer();
    }
}
