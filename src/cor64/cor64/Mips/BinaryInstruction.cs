using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public struct BinaryInstruction
    {
        public readonly uint inst;
        public readonly ushort imm;
        public readonly uint target;
        public readonly byte op;
        public readonly byte rs;
        public readonly byte rt;
        public readonly byte rd;
        public readonly byte sa;
        public readonly byte fmt;
        public readonly byte fc;
        public readonly byte func;
        public readonly byte cond;
        public readonly FpuValueType fmtType;
        public readonly byte fs;
        public readonly byte ft;
        public readonly byte fd;

        /// <summary>
        /// Used as code in trap instructions (software params)
        /// </summary>
        public readonly ushort tc;

        public static readonly BinaryInstruction NOP = new BinaryInstruction(0);

        public BinaryInstruction(UInt32 inst)
        {
            this.inst = inst;

            /* This is assuming little endian memory model */
            ushort l = (ushort)(inst);
            ushort h = (ushort)(inst >> 16);
            rt = (byte)(h & 0x1F);
            rs = (byte)(h >> 5 & 0x1F);
            rd = (byte)(l >> 11);
            sa = (byte)(l >> 6 & 0x1F);
            imm = l;
            fmt = rd;
            target = inst & 0x3FFFFFF;
            op = (byte)(h >> 10);
            func = (byte)(l & 0x3F);
            fc = (byte)((l >> 4) & 3);
            cond = (byte)(1 & 0xF);
            ft = rt;
            fs = rd;
            fd = sa;
            tc = (ushort)(imm >> 6);

            switch (rd) {
                case 16: fmtType = FpuValueType.FSingle; break;
                case 17: fmtType = FpuValueType.FDouble; break;
                case 20: fmtType = FpuValueType.Word; break;
                case 21: fmtType = FpuValueType.Doubleword; break;
                default: fmtType = FpuValueType.Reserved; break;
            }
        }
    }
}
