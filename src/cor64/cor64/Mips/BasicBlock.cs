using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public abstract class BasicBlock<TRecord>
    {
        private List<TRecord> m_RecordSet;

        protected BasicBlock(ulong address)
        {
            Address = address;
            m_RecordSet = new List<TRecord>();
        }

        public void Append(TRecord instRecord)
        {
            m_RecordSet.Add(instRecord);
        }

        public int Size => m_RecordSet.Count;

        public ulong Address { get; protected set; }

        public IReadOnlyList<TRecord> InstructionList => m_RecordSet;

        protected IList<TRecord> RecordSet => m_RecordSet;

        public TRecord[] GetCodeRecordSet()
        {
            return m_RecordSet.ToArray();
        }

        public override string ToString()
        {
            return Address.ToString("X8");
        }
    }
}
