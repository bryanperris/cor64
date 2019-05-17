using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    class Block
    {
        private int m_Ip;
        private String m_Type;

        public Block(int ip, String type)
        {
            m_Ip = ip;
            m_Type = type;
        }

        public int Ip
        {
            get { return m_Ip; }
            set { m_Ip = value; }
        }

        public String Type => m_Type;
    }
}
