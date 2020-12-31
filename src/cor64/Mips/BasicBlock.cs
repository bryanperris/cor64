﻿using System;
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

        public virtual void Append(TRecord instRecord)
        {
            m_RecordSet.Add(instRecord);
        }

        public TRecord GetLastInst() => m_RecordSet[^1];

        public virtual void UndoLastAppend() {
            m_RecordSet.RemoveAt(m_RecordSet.Count - 1);
        }

        public int Size => m_RecordSet.Count;

        public ulong Address { get; protected set; }

        public ulong EndAddress {
            get {
                return Address + ((ulong)Size * 4);
            }
        }

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
