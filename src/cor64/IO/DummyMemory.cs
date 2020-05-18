using System;
using cor64.Debugging;
using cor64.Utils;
using NLog;

namespace cor64.IO
{
	public class DummyMemory : BlockDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
		private readonly long m_Size;
        private readonly string m_DummyName;

        public DummyMemory(long size, String dummyName)
        {
			m_Size = size;
            m_DummyName = dummyName;
        }

		public override long Size => m_Size;

        public sealed override void Read(long position, byte[] buffer, int offset, int count)
		{
			Array.Clear(buffer, offset, count);
            Log.Info("Dummy read access: {0:X8} {1}", position, new MemoryAccessMeta((uint)BaseAddress, false, null).ToString());
		}

        public sealed override void Write(long position, byte[] buffer, int offset, int count)
		{
            var hex = HexTools.ToHex(buffer, offset, count);
            //Log.Debug("Dummy write access: {0:X8} {1} {2}", position, hex, new MemoryAccessMeta((uint)BaseAddress, false).ToString());
        }
	}
}
