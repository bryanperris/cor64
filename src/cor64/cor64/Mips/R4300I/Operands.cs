using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public static class Operands
    {
        public interface IOperandFormat : ICloneable {
            String Format { get; }
            int Size { get; }
            void ModifyFormat(String newFormat);
            RegUses RegisterUses { get; }
        }

        private delegate String OperandFormatter(OperandType t, String operands, String abi, BinaryInstruction inst);

        private class OperandFormat : IOperandFormat
        {
            private String format;
            private readonly int size;
            private OperandFormatter formatter;
            public OperandType type;
            private readonly RegUses m_RegUses;

            public OperandFormat(int size, String format, OperandFormatter formatter, RegUses regUses = RegUses.None)
            {
                this.size = size;
                this.format = format;
                this.formatter = formatter;
                this.m_RegUses = regUses;
            }

            public string Format => format;

            public RegUses RegisterUses => m_RegUses;

            public int Size => size;

            public OperandFormatter Formatter => formatter;

            public void ModifyFormat(string newFormat)
            {
                format = newFormat;
            }

            public object Clone()
            {
                return new OperandFormat(size, format, formatter);
            }
        }

        private static OperandFormatter m_MainFormatter = (t, s, abi, inst) =>
        {
            s = s.Replace("rd", ABI.GetLabel(abi, ABI.RegType.GPR, inst.rd));
            s = s.Replace("rt", ABI.GetLabel(abi, ABI.RegType.GPR, inst.rt));
            s = s.Replace("rs", ABI.GetLabel(abi, ABI.RegType.GPR, inst.rs));
            s = s.Replace("imm", "$" + inst.imm.ToString("X4"));
            s = s.Replace("sa", inst.sa.ToString());
            s = s.Replace("tc", "$" + inst.tc.ToString("X4"));
            return s;
        };

        private static OperandFormatter m_Cop1Formatter = (t, s, abi, inst) =>
        {
            s = m_MainFormatter(t, s, abi, inst);
            s = s.Replace("ft", ABI.GetLabel(abi, ABI.RegType.Cop1, inst.ft));
            s = s.Replace("fd", ABI.GetLabel(abi, ABI.RegType.Cop1, inst.fd));
            s = s.Replace("fs", ABI.GetLabel(abi, ABI.RegType.Cop1, inst.fs));
            s = s.Replace("cp1c", "fcr31");
            return s;
        };

        private static OperandFormatter m_CodeFormatter = (t, s, abi, inst) =>
        {
            uint code = inst.inst >> 6;
            code &= 0xFFFFF;
            s = s.Replace("code", "$" + code.ToString("X8"));
            return s;
        };

        private static OperandFormatter m_Cop0Formatter = (t, s, abi, inst) =>
        {
            s = m_MainFormatter(t, s, abi, inst);
            s = s.Replace("cop0", ABI.GetLabel(abi, ABI.RegType.Cop0, inst.rd));
            return s;
        };

        private static OperandFormatter m_BranchFormatter = (t, s, abi, inst) =>
        {
            if (t == OperandType.JUMP)
            {
                s = s.Replace("target", "$" + inst.target.ToString("X8"));
            }
            else
            {
                s = m_MainFormatter(t, s, abi, inst);
            }

            return s;
        };

        private static OperandFormatter m_BczFormatter = (t, s, abi, inst) =>
        {
            s = s.Replace("offset", "$" + inst.imm.ToString("X4"));
            return s;
        };

        private static readonly OperandFormat[] s_Formats =
{
            new OperandFormat(0, "", null),
            new OperandFormat(1, "target", m_BranchFormatter),
            new OperandFormat(3, "rs,rt,imm", m_MainFormatter,  RegUses.GPRSource | RegUses.GPRTarget),
            new OperandFormat(3, "rt,rs,imm", m_MainFormatter,  RegUses.GPRSource | RegUses.GPRTarget),
            new OperandFormat(2, "rt,imm", m_MainFormatter,     RegUses.GPRTarget),
            new OperandFormat(3, "rt,imm(rs)", m_MainFormatter, RegUses.GPRSource | RegUses.GPRTarget),
            new OperandFormat(2, "rs,imm", m_MainFormatter,     RegUses.GPRSource),
            new OperandFormat(3, "ft,imm(rs)", m_Cop1Formatter, RegUses.FPRTarget | RegUses.GPRSource),
            new OperandFormat(2, "cop0,rt", m_Cop0Formatter,    RegUses.GPRTarget),
            new OperandFormat(2, "rt,cop0", m_Cop0Formatter,    RegUses.GPRTarget),
            new OperandFormat(0, "", null),

            // BC1*
            new OperandFormat(1, "offset", m_BczFormatter),

            new OperandFormat(3, "fd,fs,ft", m_Cop1Formatter, RegUses.FPRDest   | RegUses.FPRSource | RegUses.FPRTarget),
            new OperandFormat(2, "fd,fs", m_Cop1Formatter,    RegUses.FPRDest   | RegUses.FPRSource),
            new OperandFormat(2, "fs,ft", m_Cop1Formatter,    RegUses.FPRSource | RegUses.FPRTarget),
            new OperandFormat(2, "rt,fs", m_Cop1Formatter,    RegUses.GPRTarget | RegUses.FPRSource),
            new OperandFormat(2, "ft,rt", m_Cop1Formatter,    RegUses.FPRTarget | RegUses.GPRTarget),

            new OperandFormat(2, "cp1c,rt", m_Cop1Formatter, RegUses.GPRTarget),
            new OperandFormat(2, "ft,cp1c", m_Cop1Formatter, RegUses.FPRTarget),

            new OperandFormat(3, "rd,rt,sa", m_MainFormatter, RegUses.GPRDest   | RegUses.GPRTarget),
            new OperandFormat(3, "rd,rt,rs", m_MainFormatter, RegUses.GPRDest   | RegUses.GPRSource | RegUses.GPRTarget),
            new OperandFormat(1, "rs", m_MainFormatter,       RegUses.GPRSource),
            new OperandFormat(2, "rs,rd", m_MainFormatter,    RegUses.GPRSource | RegUses.GPRDest),
            new OperandFormat(1, "rd", m_MainFormatter,       RegUses.GPRDest),
            new OperandFormat(2, "rs,rt", m_MainFormatter,    RegUses.GPRSource | RegUses.GPRTarget),
            new OperandFormat(3, "rd,rs,rt", m_MainFormatter, RegUses.GPRDest   | RegUses.GPRSource | RegUses.GPRTarget),
            new OperandFormat(1, "code", m_CodeFormatter),
            new OperandFormat(0, "", null),
            new OperandFormat(3, "rs,rt,tc", m_MainFormatter, RegUses.GPRSource | RegUses.GPRTarget),
        };


        public static IOperandFormat GetFormat(OperandType type) {
            var fmt = (OperandFormat) s_Formats[(int)type].Clone();
            fmt.type = type;
            return fmt;
        }


        public static String Decode(String abi, BinaryInstruction inst, IOperandFormat format)
        {
            var fmt = format as OperandFormat;

            if (fmt != null && fmt.Formatter != null && fmt.Size > 0)
            {
                return fmt.Formatter(fmt.type, fmt.Format, abi, inst);
            }
            else
            {
                return format.Format;
            }
        }

    }
}
