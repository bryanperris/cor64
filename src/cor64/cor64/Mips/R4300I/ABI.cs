using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    internal static class ABI
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
            "fpr0", "fpr1",   "fpr2",  "fpr3",  "fpr4",  "fpr5",  "fpr6",  "fpr7",
            "fpr8", "fpr9",   "fpr10", "fpr11", "fpr12", "fpr13", "fpr14", "fpr15",
            "fpr16", "fpr17", "fpr18", "fpr19", "fpr20", "fpr21", "fpr22", "fpr23",
            "fpr24", "fpr25", "fpr26", "fpr27", "fpr28", "fpr29", "fpr30", "fpr31"
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

        public enum RegType
        {
            GPR,
            Cop0,
            Cop1
        }

        public static String GetLabel(String abi, RegType reg, int index)
        {
            switch (reg)
            {
                case RegType.GPR: return abi == "o32" ? RegisterLabelO32[index] : RegisterLabel[index];
                case RegType.Cop0: return RegisterLabelCop0[index];
                case RegType.Cop1: return RegisterLabelCop1[index];
                default: return index.ToString("X2");
            }
        }

        // TODO: A function that dumps out stack managed by the MIPS
    }
}
