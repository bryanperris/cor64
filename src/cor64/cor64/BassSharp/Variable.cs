using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public class Variable : Token
    {
        private long m_Value;

        public Variable(string name, long value) : base(name)
        {
            m_Value = value;
        }

        public long Value
        {
            get {
                return m_Value;
            }

            set {
                m_Value = value;
            }
        }


        public override void CloneFrom(Token token)
        {
            base.CloneFrom(token);
            var t = token as Variable;
            m_Value = t.m_Value;
        }
    }
}
