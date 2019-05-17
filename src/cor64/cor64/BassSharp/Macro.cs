using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    class Macro : Token
    {
        private IList<String> m_Parameters;
        private int m_Ip;
        private bool m_Scoped;

        public Macro(String name, IList<String> parameters, int ip, bool scoped) : base(name)
        {
            this.m_Parameters = parameters;
            this.m_Ip = ip;
            this.m_Scoped = scoped;
        }

        public IList<String> Parameters
        {
            get {
                return m_Parameters;
            }
            set {
                m_Parameters = value;
            }
        }

        public int Ip
        {
            get {
                return m_Ip;
            }
            set {
                m_Ip = value;
            }
        }

        public bool Scoped
        {
            get {
                return m_Scoped;
            }
            set {
                m_Scoped = value;
            }
        }

        public override void CloneFrom(Token token)
        {
            base.CloneFrom(token);
            var t = token as Macro;
            m_Ip = t.m_Ip;
            m_Parameters = t.Parameters;
            m_Scoped = t.Scoped;
        }
    }
}
