using System.Reflection.Metadata.Ecma335;
using cor64.Mips.R4300I;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace cor64.Mips.Analysis
{
    public class RegUsageCollector
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private HashSet<int> m_UniqueGprSet = new HashSet<int>();
        private HashSet<int> m_UniqueCp0Set = new HashSet<int>();
        private HashSet<int> m_UniqueFprDSet = new HashSet<int>();
        private HashSet<int> m_UniqueFprFSet = new HashSet<int>();
        private HashSet<string> m_LoSet = new HashSet<string>();
        private HashSet<string> m_HiSet = new HashSet<string>();
        private readonly BaseDisassembler m_Disassembler;

        public RegUsageCollector(BaseDisassembler disassembler)
        {
            m_Disassembler = disassembler;
            
            m_LoSet.Add(Opcodes.MFLO.Op);
            m_LoSet.Add(Opcodes.MTLO.Op);
            m_LoSet.Add(Opcodes.DMULT.Op);
            m_LoSet.Add(Opcodes.DMULTU.Op);
            m_LoSet.Add(Opcodes.MULT.Op);
            m_LoSet.Add(Opcodes.MULTU.Op);
            m_LoSet.Add(Opcodes.DDIV.Op);
            m_LoSet.Add(Opcodes.DDIVU.Op);
            m_LoSet.Add(Opcodes.DIV.Op);
            m_LoSet.Add(Opcodes.DIVU.Op);

            m_HiSet.Add(Opcodes.MFHI.Op);
            m_HiSet.Add(Opcodes.MTHI.Op);
            m_HiSet.Add(Opcodes.DMULT.Op);
            m_HiSet.Add(Opcodes.DMULTU.Op);
            m_HiSet.Add(Opcodes.MULT.Op);
            m_HiSet.Add(Opcodes.MULTU.Op);
            m_HiSet.Add(Opcodes.DDIV.Op);
            m_HiSet.Add(Opcodes.DDIVU.Op);
            m_HiSet.Add(Opcodes.DIV.Op);
            m_HiSet.Add(Opcodes.DIVU.Op);
        }

        public void Reset()
        {
            m_UniqueGprSet.Clear();
            m_UniqueCp0Set.Clear();
            m_UniqueFprDSet.Clear();
            m_UniqueFprFSet.Clear();
            Hi = false;
            Lo = false;
            Fcr = false;
            Address = false;
        }

        public void Collect(DecodedInstruction inst)
        {
            var type = inst.Op.OperandFmt;
            var op = inst.Op.Op;

            if (inst.Op.Family == OperationFamily.Load ||
                inst.Op.Family == OperationFamily.LoadFpu ||
                inst.Op.Family == OperationFamily.Store ||
                inst.Op.Family == OperationFamily.StoreFpu ||
                inst.IsBranch)
            {
                Address = true;
            }

            if (m_LoSet.Contains(op))
                Lo = true;

            if (m_HiSet.Contains(op))
                Hi = true;

            /* The return address reg */
            if (inst.IsLink())
            {
                AddGprUsage(31);
            }

            switch (type)
            {
                case OperandType.I_TIS:
                case OperandType.I_TSI:
                case OperandType.BRANCH:
                case OperandType.R_ST:
                    {
                        AddGprUsage(inst.Source);
                        AddGprUsage(inst.Target);
                        break;
                    }

                case OperandType.R_DTS:
                case OperandType.R_DST:
                    {
                        AddGprUsage(inst.Source);
                        AddGprUsage(inst.Target);
                        AddGprUsage(inst.Destination);
                        break;
                    }

                case OperandType.I_SI:
                case OperandType.R_S:
                    {
                        AddGprUsage(inst.Source);
                        break;
                    }

                case OperandType.R_DS:
                    {
                        AddGprUsage(inst.Source);
                        AddGprUsage(inst.Destination);
                        break;
                    }

                case OperandType.R_D:
                    {
                        AddGprUsage(inst.Destination);
                        break;
                    }

                case OperandType.R_DTA:
                    {
                        AddGprUsage(inst.Destination);
                        AddGprUsage(inst.Target);
                        break;
                    }

                case OperandType.I_TI:
                    {
                        AddGprUsage(inst.Target);
                        break;
                    }

                case OperandType.Cop0_CT:
                case OperandType.Cop0_TC:
                    {
                        AddGprUsage(inst.Target);
                        AddCp0Usage(inst.Destination);
                        break;
                    }

                case OperandType.Cop1_CT:
                case OperandType.Cop1_TC:
                    {
                        AddGprUsage(inst.Target);
                        AddFprUsage(inst, inst.Destination);
                        break;
                    }

                case OperandType.Cop1_FromCtrl:
                case OperandType.Cop1_ToCtrl:
                    {
                        AddGprUsage(inst.Target);
                        Fcr = true;
                        break;
                    }

                case OperandType.Cop1_DS:
                    {
                        AddFprUsage(inst, inst.FloatSource);
                        AddFprUsage(inst, inst.FloatDest);
                        break;
                    }

                case OperandType.Cop1_DST:
                    {
                        AddFprUsage(inst, inst.FloatDest);
                        AddFprUsage(inst, inst.FloatSource);
                        AddFprUsage(inst, inst.FloatTarget);
                        break;
                    }

                case OperandType.Cop1_TS:
                    {
                        AddFprUsage(inst, inst.FloatTarget);
                        AddFprUsage(inst, inst.FloatSource);
                        break;
                    }

                default: break;
            }
        }

        private void AddGprUsage(int index)
        {
            if (!m_UniqueGprSet.Contains(index))
            {
                m_UniqueGprSet.Add(index);
            }
        }

        private void AddCp0Usage(int index)
        {
            if (!m_UniqueCp0Set.Contains(index))
            {
                m_UniqueCp0Set.Add(index);
            }
        }

        private void AddFprUsage(DecodedInstruction inst, int index)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                if (!m_UniqueFprFSet.Contains(index))
                {
                    m_UniqueFprFSet.Add(index);
                }
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                if (!m_UniqueFprDSet.Contains(index))
                {
                    m_UniqueFprDSet.Add(index);
                }
            }
            else
            {
                Log.Warn("Cannot track FPR Word or Dword usage");
            }
        }

        public IReadOnlyList<int> GprSelects => m_UniqueGprSet.ToList().AsReadOnly();

        public IReadOnlyList<int> FprDSelects => m_UniqueFprDSet.ToList().AsReadOnly();

        public IReadOnlyList<int> FprFSelects => m_UniqueFprFSet.ToList().AsReadOnly();

        public int GprCount => m_UniqueGprSet.Count;

        public int FprCount => m_UniqueFprDSet.Count;

        public int Cp0Count => m_UniqueCp0Set.Count;

        public bool Hi { get; private set; } = false;

        public bool Lo { get; private set; } = false;

        public bool Fcr { get; private set; } = false;

        public bool Address { get; private set; }
    }
}
