

using System;
using System.Text;

namespace cor64.Mips.R4300I.TLB
{
    public sealed class TLBEntryInfo
    {
        private Int32 m_TLBIndex;
        private TLBEntry m_Entry;

        internal TLBEntryInfo(Int32 index, TLBEntry entry)
        {
            m_TLBIndex = index;
            m_Entry = entry;
        }

        public TLBEntry AssociatedEntry
        {
            get { return m_Entry; }
        }

        public Int32 SelectedIndex
        {
            get { return m_TLBIndex; }
        }

        // public override string ToString()
        // {
        //     StringBuilder sb = new StringBuilder();
        //     sb.Append("Entry ");
        //     sb.Append(m_TLBIndex);
        //     sb.Append(" : 0x");
        //     sb.Append(m_Entry.MappedVirtualAddress.ToString("X16"));
        //     return sb.ToString();
        // }
    }
}