using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public static class ABI
    {
        private static readonly String[] RegisterLabel = {
            "r00", "r01", "r02", "r03", "r04", "r05", "r06", "r07",
            "r08", "r09", "r10", "r11", "r12", "r13", "r14", "r15",
            "r16", "r17", "r18", "r19", "r20", "r21", "r22", "r23",
            "r24", "r25", "r26", "r27", "r28", "r29", "r30", "r31"
        };

        private static readonly String[] RegisterLabelO32 = {
            "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3",
            "t0",   "t1", "t2", "t3", "t4", "t5", "t6", "t7",
            "s0",   "s1", "s2", "s3", "s4", "s5", "s6", "s7",
            "t8",   "t9", "k0", "k1", "gp", "sp", "fp", "ra"
        };

        private static readonly String[] RegisterLabelCop1 = {
            "f0", "f1",   "f2",  "f3",  "f4",  "f5",  "f6",  "f7",
            "f8", "f9",   "f10", "f11", "f12", "f13", "f14", "f15",
            "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23",
            "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31"
        };

        private static readonly String[] RegisterLabelVu= {
            "v0", "v1",   "v2",  "v3",  "v4",  "v5",  "v6",  "v7",
            "v8", "v9",   "v10", "v11", "v12", "v13", "v14", "v15",
            "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23",
            "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31"
        };

        private static readonly String[] RegisterLabelCop0 = {
            "Index",    "Random",   "EntryLo0", "EntryLo1",
            "Context",  "PageMask", "Wired",    "Reserved",
            "BadVAddr", "Count",    "EntryHi",  "Compare",
            "Status",   "Cause",    "EPC",      "PRId",
            "Config",   "LLAddr",   "WatchLo",  "WatchHi",
            "XContext", "Reserved", "Reserved", "Reserved",
            "Reserved", "Reserved", "PErr",     "CacheErr",
            "TagLo",    "TagHi",    "ErrorEPC", "Reserved"
        };

        private static readonly String[] RegisterLabelRspCop0 = {
            "RSP 0",       "RSP 1",         "RSP 2",    "RSP 3",
            "RSP 4",       "RSP 5",         "RSP 6",    "RSP 7",
            "DPC_START_REG",  "DPC_END_REG",      "DPC_CURRENT_REG",    "DPC_STATUS_REG",
            "DPC_CLOCK_REG",  "DPC_BUFBUSY_REG",  "DPC_PIPEBUSY_REG",    "DPC_TMEM_REG",
            "Reserved",    "Reserved",      "Reserved", "Reserved",
            "Reserved",    "Reserved",      "Reserved", "Reserved",
            "Reserved",    "Reserved",      "Reserved", "Reserved",
            "Reserved",    "Reserved",      "Reserved", "Reserved"
        };

        private static readonly String[] InterruptName = {
            "Software Interrupt 1",
            "Software Interrupt 2",
            "RCP Interrupt",
            "Cartridge Interrupt",
            "Reset Button",
            "RDB Read",
            "RDB Write",
            "Timer Event"
        };

        private static readonly String[] RegisterLabelVuCtrl = {
            "vco",
            "vcc",
            "vce"
        };

        public enum RegType
        {
            GPR,
            Cop0,
            Cop1,
            SpCop0,
            VU,
            VUCtrl
        }

        public static String GetLabel(String abi, RegType reg, int index)
        {
            if (abi.Equals("strict"))
                return index.ToString();

            switch (reg)
            {
                case RegType.GPR: return abi == "o32" ? RegisterLabelO32[index] : RegisterLabel[index];
                case RegType.Cop0: return RegisterLabelCop0[index];
                case RegType.SpCop0: return RegisterLabelRspCop0[index];
                case RegType.Cop1: return RegisterLabelCop1[index];
                case RegType.VU: return RegisterLabelVu[index];
                case RegType.VUCtrl: {
                    if (index >= 0 && index <= 2)
                        return RegisterLabelVuCtrl[index];
                    else
                        return index.ToString();
                }
                default: return index.ToString("X2");
            }
        }

        public static String GetInterruptType(int index)
        {
            return InterruptName[index];
        }
    }
}
