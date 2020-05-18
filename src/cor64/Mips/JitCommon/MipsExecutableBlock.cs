using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.Mips.R4300I;

namespace cor64.Mips.JitCommon
{
    public abstract class MipsExecutableBlock : IDynamicBlock
    {
        private ulong m_Address;
        private RuntimeBasicBlock m_Shadow;
        private bool m_WillJump;
        private ulong m_JumpAddress;
        private int m_Size;

        protected MipsExecutableBlock(ulong address, int size, RuntimeBasicBlock shadow)
        {
            m_Address = address;
            m_Shadow = shadow;
            m_Size = size;
        }

        public abstract void Execute(IDynamicMips owner);

        public void SetJump(ulong address)
        {
            m_WillJump = true;
            m_JumpAddress = address;
        }

        public void ClearJump()
        {
            m_WillJump = false;
        }

        public RuntimeBasicBlock Shadow => m_Shadow;

        public ulong Address => m_Address;

        public String CompileLog { get; set; }

        public bool WillJump => m_WillJump;

        public ulong JumpAddress => m_JumpAddress;

        public int Size => m_Size;
    }
}
