﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public abstract class DmaBlockDevice : BlockDevice
    {
        private readonly N64MemoryController m_Controller;

        protected DmaBlockDevice(N64MemoryController controller)
        {
            m_Controller = controller;
        }

        public uint SourceAddress { get; protected set; }

        public uint DestAddress { get; protected set; }

        protected Task<int> TransferBytesAsync(int length)
        {
            return m_Controller.MemoryCopyAsync(SourceAddress, DestAddress, length);
        }

        protected int TransferBytes(int length) {
            return m_Controller.MemoryCopy(SourceAddress, DestAddress, length);
        }

        protected int TransferBytesUnaligned(int length) {
            return m_Controller.MemoryCopyUnaligned(SourceAddress, DestAddress, length);
        }
        
        protected N64MemoryController ParentController => m_Controller;
    }
}
