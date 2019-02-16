using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Table
{
    public class Opcode
    {
        private List<Prefix> m_PrefixList = new List<Prefix>();
        private List<Number> m_NumberList = new List<Number>();
        private List<Format> m_FormatList = new List<Format>();

        public String Pattern
        {
            get;
            set;
        }

        public Opcode()
        {

        }

        public IList<Prefix> PrefixList => m_PrefixList;
        public IList<Number> NumberList => m_NumberList;
        public IList<Format> FormatList => m_FormatList;

        public override string ToString()
        {
            return Pattern;
        }

        public String GetOpName()
        {
            return m_PrefixList[0].Text.Split(' ')[0];
        }
    }
}
