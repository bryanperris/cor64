using cor64.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace cor64.IO
{
    public abstract class PerpherialDevice : DmaBlockDevice
    {
        private long m_BuilderOffset;
        private int m_Size;
        private MemMappedBuffer[] m_MappedDevices;
        private PinnedBuffer m_WriteTemp = new PinnedBuffer(4);
        private PinnedBuffer m_ReadTemp = new PinnedBuffer(4);

        private IntPtr[] m_ReadMap;
        private IntPtr[] m_WriteMap;
        private Action[] m_WriteFunc;

        protected PerpherialDevice(N64MemoryController controller, int size) : base(controller)
        {
            m_Size = size;

            var arraySize = size / 4;

            m_ReadTemp.Clear();

            m_ReadMap = new IntPtr[arraySize];
            m_WriteMap = new IntPtr[arraySize];
            m_WriteFunc = new Action[arraySize];

            Fill(m_ReadMap, m_ReadTemp.GetPointer());
            Fill(m_WriteMap, m_WriteTemp.GetPointer());
            Fill(m_WriteFunc, EmptyWriteHandler);
        }

        private void EmptyWriteHandler()
        {
        }

        private void Fill<T>(T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        protected void Map(params MemMappedBuffer[] devices)
        {
            m_MappedDevices = new MemMappedBuffer[devices.Length];
            Array.Copy(devices, m_MappedDevices, m_MappedDevices.Length);

            for (int i = 0; i < devices.Length; i++)
            {
                var device = devices[i];

                if (device == null)
                    throw new ArgumentNullException(nameof(device));

                if (device.Size % 4 != 0)
                {
                    throw new ArgumentException("size must be word aligned");
                }

                var off = (int)m_BuilderOffset / 4;

                for (int j = 0; j < (device.Size / 4); j++)
                {
                    int ind = off + j;
                    int ptrOff = j * 4;

                    if (device.CanRead) m_ReadMap[ind] = IntPtr.Add(device.ReadPtr, ptrOff);
                    if (device.CanWrite) m_WriteMap[ind] = IntPtr.Add(device.WritePtr, ptrOff);

                    m_WriteFunc[ind] = device.WriteNotify;
                }

                m_BuilderOffset += device.Size;
            }
        }

        protected void Map(int size)
        {
            m_BuilderOffset += (uint)size;
        }

        public sealed override void Read(long position, byte[] buffer, int offset, int count)
        {
            //var index = (int)position / 4;
            //var _buffer = m_BufferTable[index];

            //if (_buffer != null)
            //{
            //    // Assuming word-aligned read

            //    //var _offset = (int)position - (int)_buffer.AssignedAddress;
            //    //_buffer.Read(buffer, _offset, offset, count);
            //}

            Marshal.Copy(m_ReadMap[(int)position / 4], buffer, offset, count);
        }

        public sealed override void Write(long position, byte[] buffer, int offset, int count)
        {
            //var index = (int)position / 4;
            //var _buffer = m_BufferTable[index];

            //if (_buffer != null)
            //{
            //    // Assuming word-aligned write

            //    //var _offset = (int)position - (int)_buffer.AssignedAddress;
            //    //_buffer.Write(buffer, offset, _offset, count);
            //   //_buffer.OnMemWrite();
            //}

            int ind = (int)position / 4;
            Marshal.Copy(buffer, offset, m_WriteMap[ind], count);
            m_WriteFunc[ind].Invoke();
        }

        public override void WriteNotify(uint position)
        {
            m_WriteFunc[position].Invoke();
        }

        public override IntPtr[] GetReadPointerMap()
        {
            return m_ReadMap;
        }

        public override IntPtr[] GetWritePointerMap()
        {
            return m_WriteMap;
        }

        public override long Size => m_Size;
    }
}
