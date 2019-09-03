using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace cor64.Mips.R4300I
{
    /// <summary>
    /// This is used to debug at the Interpreter level rather than inside of the MIPS runtime
    /// </summary>
    public class CoreDebugger
    {
        private HashSet<int> m_GPRWriteBreakpoints = new HashSet<int>();
        private HashSet<int> m_GPRReadBreakpoints = new HashSet<int>();
        private Dictionary<uint, BranchCondition> m_BranchBreakpoints = new Dictionary<uint, BranchCondition>();
        private HashSet<uint> m_InstByAddressBreakpoints = new HashSet<uint>();
        private HashSet<uint> m_InstByCodeBreakpoints = new HashSet<uint>();

        public CoreDebugger()
        {
        }

        public enum BranchCondition {
            Any = 0,
            Taken,
            NotTaken
        }

        public bool SkipBreakpoint { get; set; }

        [Conditional("DEBUG")]
        public void TestForInstBreakpoint(DecodedInstruction inst)
        {
            if (SkipBreakpoint)
                return;

            if (m_InstByCodeBreakpoints.Contains(inst.Inst.inst))
            {
                throw new VirtualBreakpointException();
            }

            if (m_InstByAddressBreakpoints.Contains((uint)inst.Address))
            {
                throw new VirtualBreakpointException();
            }
        }

        [Conditional("DEBUG")]
        public void TestForGprBreakpoint(int reg, bool isWrite)
        {
            if (SkipBreakpoint)
                return;

            if (!isWrite)
            {
                if (m_GPRReadBreakpoints.Contains(reg))
                {
                    throw new VirtualBreakpointException();
                }
            }
            else
            {
                if (m_GPRWriteBreakpoints.Contains(reg))
                {
                    throw new VirtualBreakpointException();
                }
            }
        }

        [Conditional("DEBUG")]
        public void TestForBranchBreakpoint(uint target, bool taken)
        {
            if (SkipBreakpoint)
                return;

            if (m_BranchBreakpoints.ContainsKey(target))
            {
                var kind = m_BranchBreakpoints[target];

                if (kind == BranchCondition.Any)
                {
                    throw new VirtualBreakpointException();
                }
                else if (kind == BranchCondition.Taken && taken)
                {
                    throw new VirtualBreakpointException();
                }
                else if (kind == BranchCondition.NotTaken && !taken)
                {
                    throw new VirtualBreakpointException();
                }
            }
        }

        public void AppendGprRegisterReadBreakpoint(int gpr)
        {
            if (!m_GPRReadBreakpoints.Contains(gpr))
            {
                m_GPRReadBreakpoints.Add(gpr);
            }
        }

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
    }
}
