using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public abstract class BasicBlock<TRecord>
    {
        private ulong m_Address;
        private TRecord[] m_RecordSet;
        private int m_Offset;

        protected BasicBlock(ulong address, int size)
        {
            m_Address = address;
            m_RecordSet = new TRecord[size];
        }

        public void Append(TRecord instRecord)
        {
            m_RecordSet[m_Offset++] = instRecord;
        }

        public int Size => m_Offset + 1;

        public ulong Address => m_Address;

        protected void SetAddress(ulong address)
        {
            m_Address = address;
        }

        public IReadOnlyList<TRecord> InstructionList => m_RecordSet;

        protected IList<TRecord> RecordSet => m_RecordSet;

        public TRecord[] GetCodeRecordSet()
        {
            return m_RecordSet.ToArray();
        }

        public override string ToString()
        {
            return m_Address.ToString("X8");
        }
    }
}
