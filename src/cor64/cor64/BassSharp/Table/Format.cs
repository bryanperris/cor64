using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Table
{
    public class Format
    {
        private int m_Argument;

        public enum Type
        {
            Static,
            Absolute,
            Relative,
            Repeat,
            ShiftRight,
            ShiftLeft,
            RelativeShiftRight,
            Negative,
            NegativeShiftRight
        }

        public enum Match
        {
            Exact,
            Strong,
            Weak
        }

        public Type FType { get; set; }
        public Match FMatch { get; set; }
        public uint Data { get; set; }
        public int Bits
        {
            get;
            set;
        }

        public int Argument {
            get { return m_Argument; }
            set {
                m_Argument = value;
            }
        }

        public int Displacement { get; set; }

        public Format()
        {
        }
    }
}
