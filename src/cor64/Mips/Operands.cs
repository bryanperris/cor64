using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public static class Operands
    {
        public interface IOperandFormat : ICloneable {
            String Format { get; }
            int Size { get; }
            void ModifyFormat(String newFormat);
        }

        private delegate String OperandFormatter(OperandType t, String operands, String abi, BinaryInstruction inst, ISymbolProvider symbolProvider);

        private class OperandFormat : IOperandFormat
        {
            private String format;
            private readonly int size;
            public OperandType type;

            public OperandFormat(int size, String format, OperandFormatter formatter)
            {
                this.size = size;
                this.format = format;
                Formatter = formatter;
            }

            public string Format => format;

            public int Size => size;

            public OperandFormatter Formatter { get; }

            public void ModifyFormat(string newFormat)
            {
                format = newFormat;
            }

            public object Clone()
            {
                return new OperandFormat(size, format, Formatter);
            }
        }

        private static readonly OperandFormatter m_MainFormatter = (t, s, abi, inst, sym) =>
        {
            s = s.Replace("rd", ABI.GetLabel(abi, ABI.RegType.GPR, inst.rd));
            s = s.Replace("rt", ABI.GetLabel(abi, ABI.RegType.GPR, inst.rt));
            s = s.Replace("rs", ABI.GetLabel(abi, ABI.RegType.GPR, inst.rs));
            s = s.Replace("imm", "$" + inst.imm.ToString("X4"));
            s = s.Replace("sa", inst.sa.ToString());
            s = s.Replace("tc", "$" + inst.tc.ToString("X4"));
            return s;
        };

        private static readonly OperandFormatter m_Cop1Formatter = (t, s, abi, inst, sym) =>
        {
            s = m_MainFormatter(t, s, abi, inst, sym);
            s = s.Replace("ft", ABI.GetLabel(abi, ABI.RegType.Cop1, inst.ft));
            s = s.Replace("fd", ABI.GetLabel(abi, ABI.RegType.Cop1, inst.fd));
            s = s.Replace("fs", ABI.GetLabel(abi, ABI.RegType.Cop1, inst.fs));
            s = s.Replace("cp1c", "fcr31");
            return s;
        };

        private static readonly OperandFormatter m_CodeFormatter = (t, s, abi, inst, sym) =>
        {
            uint code = inst.inst >> 6;
            code &= 0xFFFFF;
            return s.Replace("code", "$" + code.ToString("X8"));
        };

        private static readonly OperandFormatter m_Cop0Formatter = (t, s, abi, inst, sym) =>
        {
            s = m_MainFormatter(t, s, abi, inst, sym);
            s = s.Replace("spcop0", ABI.GetLabel(abi, ABI.RegType.SpCop0, inst.rd));
            s = s.Replace("cop0", ABI.GetLabel(abi, ABI.RegType.Cop0, inst.rd));
            return s;
        };

        private static readonly OperandFormatter m_VuFormatter = (t, s, abi, inst, sym) =>
        {
            s = m_MainFormatter(t, s, abi, inst, sym);
            s = s.Replace("vs", ABI.GetLabel(abi, ABI.RegType.VU, inst.vs));
            s = s.Replace("vt", ABI.GetLabel(abi, ABI.RegType.VU, inst.vt));
            s = s.Replace("vd", ABI.GetLabel(abi, ABI.RegType.VU, inst.vd));
            s = s.Replace("offset", "$" + inst.offset.ToString("X2"));
            s = s.Replace("vecctrl", ABI.GetLabel(abi, ABI.RegType.VUCtrl, inst.vs));
            s = s.Replace("[e]", "[e" + inst.e.ToString() + "]");
            s = s.Replace("[de]", "[e" + inst.de.ToString() + "]");
            s = s.Replace("[lsde]", "[e" + inst.lsde.ToString() + "]");
            return s;
        };

        private static readonly OperandFormatter m_BranchFormatter = (t, s, abi, inst, sym) =>
        {
            if (t == OperandType.JUMP)
            {
                var offset = (int)inst.target;

                // offset *= 4;

                return s.Replace("target", "$" + offset.ToString("X4"));
            }
            else
            {
                var offset = (short)inst.imm;

                // offset *= 4;

                s = s.Replace("offset", "$" + offset.ToString("X4"));

                return m_MainFormatter(t, s, abi, inst, sym);
            }
        };

        private static readonly OperandFormatter m_BczFormatter = (t, s, abi, inst, sym) =>
        {
            s = s.Replace("offset", "$" + inst.imm.ToString("X4"));
            return s;
        };

        private static readonly OperandFormat[] s_Formats =
{
            new OperandFormat(0, "", null),
            new OperandFormat(1, "target", m_BranchFormatter),
            new OperandFormat(3, "rs,rt,offset", m_BranchFormatter),
            new OperandFormat(3, "rt,rs,imm", m_MainFormatter),
            new OperandFormat(2, "rt,imm", m_MainFormatter),
            new OperandFormat(3, "rt,imm(rs)", m_MainFormatter),
            new OperandFormat(2, "rs,imm", m_MainFormatter),
            new OperandFormat(3, "ft,imm(rs)", m_Cop1Formatter),
            new OperandFormat(2, "cop0,rt", m_Cop0Formatter),
            new OperandFormat(2, "rt,cop0", m_Cop0Formatter),

            // Signal Processor
            new OperandFormat(2, "spcop0,rt", m_Cop0Formatter),
            new OperandFormat(2, "rt,spcop0", m_Cop0Formatter),

            // Vector Unit
            new OperandFormat(4, "vd,vs,vt[e]", m_VuFormatter),
            new OperandFormat(4, "vd[de], vt[e]", m_VuFormatter),
            new OperandFormat(4, "vt[lsde],offset(rs)", m_VuFormatter),
            new OperandFormat(2, "vs,rt", m_VuFormatter),
            new OperandFormat(2, "rt,vs", m_VuFormatter),
            new OperandFormat(2, "vecctrl,rt", m_VuFormatter),
            new OperandFormat(2, "rt,vecctrl", m_VuFormatter),

            new OperandFormat(0, "", null),

            // BC1*
            new OperandFormat(1, "offset", m_BczFormatter),

            new OperandFormat(3, "fd,fs,ft", m_Cop1Formatter),
            new OperandFormat(2, "fd,fs", m_Cop1Formatter),
            new OperandFormat(2, "fs,ft", m_Cop1Formatter),
            new OperandFormat(2, "rt,fs", m_Cop1Formatter),
            new OperandFormat(2, "ft,rt", m_Cop1Formatter),

            new OperandFormat(2, "cp1c,rt", m_Cop1Formatter),
            new OperandFormat(2, "rt,cp1c", m_Cop1Formatter),

            new OperandFormat(3, "rd,rt,sa", m_MainFormatter),
            new OperandFormat(3, "rd,rt,rs", m_MainFormatter),
            new OperandFormat(1, "rs", m_MainFormatter),
            new OperandFormat(2, "rd,rs", m_MainFormatter),
            new OperandFormat(1, "rd", m_MainFormatter),
            new OperandFormat(2, "rs,rt", m_MainFormatter),
            new OperandFormat(3, "rd,rs,rt", m_MainFormatter),
            new OperandFormat(1, "code", m_CodeFormatter),
            new OperandFormat(0, "", null),
            new OperandFormat(3, "rs,rt,tc", m_MainFormatter),

            new OperandFormat(1, "vector", m_VuFormatter)
        };


        public static IOperandFormat GetFormat(OperandType type) {
            var fmt = (OperandFormat) s_Formats[(int)type].Clone();
            fmt.type = type;
            return fmt;
        }


        public static String Decode(String abi, BinaryInstruction inst, IOperandFormat format, ISymbolProvider symbolProvider)
        {
            if (format is OperandFormat fmt && fmt.Formatter != null && fmt.Size > 0)
            {
                return fmt.Formatter(fmt.type, fmt.Format, abi, inst, symbolProvider);
            }
            else
            {
                return format.Format;
            }
        }
    }
}
