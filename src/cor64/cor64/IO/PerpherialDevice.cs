using cor64.Debugging;
using System;
using System.Collections.Generic;
using System.IO;

namespace cor64.IO
{
    public abstract class PerpherialDevice : DmaBlockDevice
    {
        private long m_BuilderOffset;
        private int m_Size;
        private MemMappedBuffer[] m_BufferTable;

        protected PerpherialDevice(N64MemoryController controller, int size) : base(controller)
        {
            m_Size = size;
            m_BufferTable = new MemMappedBuffer[size / 4];
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

            if (device.Size % 4 != 0)
            {
                throw new ArgumentException("size must be word aligned");
            }

            device.AssignedAddress = m_BuilderOffset;

            var count = device.Size / 4;
            var off = m_BuilderOffset / 4;

            for (int i = 0; i < count; i++)
                m_BufferTable[off + i] = device;

            m_BuilderOffset += (uint)device.Size;
        }

        protected void AppendUnused(int size)
        {
            m_BuilderOffset += (uint)size;
        }

        protected override void Read(long position, byte[] buffer, int offset, int count)
        {
            var _buffer = m_BufferTable[(uint)position / 4];

            if (_buffer != null)
            {
                // Assuming word-aligned read

                var _offset = (int)position - (int)_buffer.AssignedAddress;
                _buffer.Read(buffer, _offset, offset, count);
            }
        }

        protected override void Write(long position, byte[] buffer, int offset, int count)
        {
            var _buffer = m_BufferTable[(uint)position / 4];

            if (_buffer != null)
            {
                // Assuming word-aligned write

                var _offset = (int)position - (int)_buffer.AssignedAddress;
                _buffer.Write(buffer, offset, _offset, count);
                _buffer.OnMemWrite();
            }
        }

        public override long Size => m_Size;
    }
}
