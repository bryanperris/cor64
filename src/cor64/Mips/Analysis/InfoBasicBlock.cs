using System.Reflection.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using cor64.Debugging;

namespace cor64.Mips.Analysis
{
    public class InfoBasicBlock : BasicBlock<InfoBasicBlockInstruction>
    {
        private readonly ProgramTrace m_ParentTrace;
        private const int MOD_ADLER = 65521;
        private bool m_DelaySlotEmpty = true;
        private bool m_NullifiedDelaySlot = false;
        private bool m_FullFinish = false;
        private readonly List<MemoryAccessMeta> m_MemAccessList = new List<MemoryAccessMeta>();


        public InfoBasicBlock(ProgramTrace programTrace, long address) : base(address)
        {
            m_ParentTrace = programTrace;
        }

        public BlockEnding Ending { get; private set; } = BlockEnding.None;

        public void Finish(BlockEnding ending) {
            if (Ending == BlockEnding.None) {
                Ending = ending;

                if (ending != BlockEnding.NormalJump)
                    m_FullFinish = true;
            }
            else {
                throw new InvalidOperationException("Cannot finish an already finished block");
            }
        }

        public bool IsExceptionHandler { get; set; }

        public BaseDisassembler Disassembler => m_ParentTrace.Disassembler;

        public override void Append(InfoBasicBlockInstruction instRecord)
        {
            if (Ending == BlockEnding.None) {
                base.Append(instRecord);
                m_MemAccessList.Add(null);
            }
            else
                throw new InvalidOperationException("Cannot append to block with an active ending");
        }

        public void AppendDelaySlot(InfoBasicBlockInstruction instRecord) {
            if (m_DelaySlotEmpty) {
                m_DelaySlotEmpty = false;
                base.Append(instRecord);
                m_MemAccessList.Add(null);
                m_FullFinish = true;
            }
            else {
                throw new InvalidOperationException("already have a delay slot");
            }
        }

        public MemoryAccessMeta LookupMemAccess(int index) {
            if (index >= 0 && index < m_MemAccessList.Count) {
                return m_MemAccessList[index];
            }

            return null;
        }

        public void SetMemAccess(int index, MemoryAccessMeta access) {
            if (index >= 0 && index < m_MemAccessList.Count) {
                var value = m_MemAccessList[index];

                if (value != null) {
                    value.AppendValue(access.Value);
                }
                else {
                    m_MemAccessList[index] = access;
                }

            }
        }

        public bool HasDelaySlot => m_DelaySlotEmpty;

        public bool IsDelaySlotNullified => m_NullifiedDelaySlot;

        public int Repeat { get; set; }

        public void SetNullifiedDelaySlot() {
            m_NullifiedDelaySlot = true;
            m_FullFinish = true;
        }

        public bool FullyFinished => m_FullFinish;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            InfoBasicBlock other = (InfoBasicBlock)obj;

            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            uint a = 1;
            uint b = 0;

            // Computes hash using adler32 hash algorithm

            long addr = Address;
            a = (a + (byte)(addr >>  0)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >>  8)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >> 16)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >> 24)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >> 32)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >> 40)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >> 48)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            a = (a + (byte)(addr >> 56)) % MOD_ADLER; b = (b + a) % MOD_ADLER;

            foreach (var inst in RecordSet) {
                uint rawInst = inst.Inst.Inst.inst;
                a = (a + (byte)(rawInst >>  0)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
                a = (a + (byte)(rawInst >>  8)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
                a = (a + (byte)(rawInst >> 16)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
                a = (a + (byte)(rawInst >> 24)) % MOD_ADLER; b = (b + a) % MOD_ADLER;
            }

            return  (int)((b << 16) | a);
        }
    }
}
