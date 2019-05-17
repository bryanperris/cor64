using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public class Define : Token
    {
        private String m_Value;

        public Define(String name, String value)
            : base(name)
        {
            m_Value = value;
        }

        public String Value
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
            var t = token as Define;
            m_Value = t.Value;
            base.CloneFrom(token);
        }
    }
}
