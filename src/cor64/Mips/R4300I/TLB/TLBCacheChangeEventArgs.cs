

using System;

namespace cor64.Mips.R4300I.TLB
{
    public sealed class TLBCacheChangeEventArgs : EventArgs
    {
        private Int32 m_Index;

        public TLBCacheChangeEventArgs(Int32 index)
        {
            m_Index = index;
        }

        public Int32 Index
        {
            get { return m_Index; }
        }
    }
}