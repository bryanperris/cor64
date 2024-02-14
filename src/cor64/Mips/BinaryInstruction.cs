﻿using System;
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

            /* This model assume the data is little-endian */
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

            fmtType = rs switch
            {
                16 => FpuValueType.FSingle,
                17 => FpuValueType.FDouble,
                20 => FpuValueType.Word,
                21 => FpuValueType.Doubleword,
                _ => FpuValueType.Reserved,
            };
        }

        // Vector operands
        public int e => rs & 0b01111; // Source element
        public int vt => rt;
        public int vs => rd;
        public int vd => sa;
        public int de => vs; // Destination Element
        public int lsde => (int)((inst >> 7) & 0xF); // Destination Element for Load/Store
        public int offset => (int)(inst & 0x7F);
    }
}
