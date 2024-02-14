using System;
using cor64.Debugging;
using cor64.Utils;
using NLog;

namespace cor64.IO
{
	public class DummyMemory : N64MemoryDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
		private readonly int m_Size;
        private readonly string m_DummyName;

        public DummyMemory(int size, String dummyName, N64MemoryController controller) : base(controller, size)
        {
            m_DummyName = dummyName;
        }

        public override string Name => m_DummyName;

        // WIP: Attach dummy read or write notification handlers for debugging
	}
}
