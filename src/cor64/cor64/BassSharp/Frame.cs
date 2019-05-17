using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    class Frame
    {
        private int m_Ip;
        private bool m_Scoped;
        private Instruction m_InvokedBy;
        private HashSet<Macro> m_Macros = new HashSet<Macro>();
        private HashSet<Define> m_Defines = new HashSet<Define>();
        private HashSet<Variable> m_Variables = new HashSet<Variable>();

        public Frame()
        {
            
        }

        public Frame (int ip, bool scoped, Instruction invokedBy)
        {
            m_InvokedBy = invokedBy;
            m_Scoped = scoped;
            m_Ip = ip;
        }

        public ISet<Macro> Macros => m_Macros;
        public ISet<Define> Defines => m_Defines;
        public ISet<Variable> Variables => m_Variables;

        public int Ip
        {
            get { return m_Ip; }
            set { m_Ip = value; }
        }

        public bool Scoped
        {
            get { return m_Scoped; }
            set { m_Scoped = value; }
        }

        public Instruction InvokedBy
        {
            get { return m_InvokedBy; }
            set { m_InvokedBy = value; }
        }
    }
}
