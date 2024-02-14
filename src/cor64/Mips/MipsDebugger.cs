using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace cor64.Mips
{
    /// <summary>
    /// This is used to debug at the Interpreter level rather than inside of the MIPS runtime
    /// </summary>
    public class MipsDebugger
    {
        private readonly HashSet<int> m_GPRWriteBreakpoints = new HashSet<int>();
        private readonly Dictionary<uint, BranchCondition> m_BranchBreakpoints = new Dictionary<uint, BranchCondition>();
        private readonly HashSet<uint> m_InstByAddressBreakpoints = new HashSet<uint>();
        private readonly HashSet<uint> m_InstByCodeBreakpoints = new HashSet<uint>();

        // TODO: Optimizations here using delegates

        private Action<DecodedInstruction> m_Probe_OnInstruction = (_) => {};
        private Action<int> m_Probe_OnGprWrite = (_) => {};
        private Action<uint, bool> m_Probe_OnBranch = (_,_) => {};

        private bool m_BreakActive;

        public MipsDebugger()
        {
        }

        public bool IsBreakActive => m_BreakActive;

        public virtual void ActivateDebugger() {
            // CLEANUP: fix this
            // m_Probe_OnInstruction = (inst) => {
            //     if (m_BreakActive) return;

            //     ThrowBreakpoint(m_InstByCodeBreakpoints.Contains(inst.Inst.inst));
            //     ThrowBreakpoint(m_InstByAddressBreakpoints.Contains((uint)inst.Address));
            // };

            m_Probe_OnGprWrite = (i) => {
                if (m_BreakActive) return;

                ThrowBreakpoint(m_GPRWriteBreakpoints.Contains(i));
            };

            m_Probe_OnBranch = (target, taken) => {
                if (m_BreakActive) return;

                if (m_BranchBreakpoints.ContainsKey(target))
                {
                    var kind = m_BranchBreakpoints[target];

                    if (kind == BranchCondition.Any)
                    {
                        ThrowBreakpoint(true);
                    }
                    else if (kind == BranchCondition.Taken && taken)
                    {
                        ThrowBreakpoint(true);
                    }
                    else if (kind == BranchCondition.NotTaken && !taken)
                    {
                        ThrowBreakpoint(true);
                    }
                }
            };
        }

        public enum BranchCondition {
            Any = 0,
            Taken,
            NotTaken
        }

        private void ThrowBreakpoint(bool condition) {
            if (condition) {
                Break();
                // m_BreakActive = true;
                //throw new VirtualBreakpointException();
            }
        }

        internal void CheckInstructionBreakpoints(DecodedInstruction inst) => m_Probe_OnInstruction(inst);

        internal void CheckGprWriteBreakpoints(int reg) => m_Probe_OnGprWrite(reg);

        internal void CheckBranchBreakpoints(uint target, bool taken) => m_Probe_OnBranch(target, taken);

        public void AppendGprRegisterWriteBreakpoint(int gpr)
        {
            if (!m_GPRWriteBreakpoints.Contains(gpr))
            {
                m_GPRWriteBreakpoints.Add(gpr);
            }
        }

        public void AppendBranchBreakpoint(uint address, BranchCondition condition)
        {
            if (!m_BranchBreakpoints.ContainsKey(address))
            {
                m_BranchBreakpoints.Add(address, condition);
            }
        }

        public void AppendInstBreakpointByAddr(uint address)
        {
            if (!m_InstByAddressBreakpoints.Contains(address))
            {
                m_InstByAddressBreakpoints.Add(address);
            }
        }

        public void AppendInstBreakpointByCode(String asm)
        {
            uint inst = N64Assembler.AssembleSingleInstruction(asm);

            if (!m_InstByCodeBreakpoints.Contains(inst))
            {
                m_InstByCodeBreakpoints.Add(inst);
            }
        }

        public void ClearBreakpoints() {
            m_InstByAddressBreakpoints.Clear();
            m_GPRWriteBreakpoints.Clear();
            m_BranchBreakpoints.Clear();
            m_InstByCodeBreakpoints.Clear();
        }

        public void Continue() {
            m_BreakActive = false;
        }

        public void Break() {
            m_BreakActive = true;
        }
    }
}
