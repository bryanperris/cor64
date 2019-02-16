using System;
namespace cor64.IO
{
	public class DummyMemory : BlockDevice
    {
		private long m_Size;

        public DummyMemory(long size)
        {
			m_Size = size;
        }

		public override long Size => m_Size;

        protected override void Read(long position, byte[] buffer, int offset, int count)
		{
			Array.Clear(buffer, offset, count);
		}

		protected override void Write(long position, byte[] buffer, int offset, int count)
		{
            // Do nothing
		}
	}
}
