using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    [Serializable]
    public class MipsException : Exception
    {
        private ExceptionType m_Exception;

        public MipsException(ExceptionType type) : base()
        {
            m_Exception = type;
        }

        public ExceptionType Exception => m_Exception;

        // TODO: Lazy, need to support the different exception base constructors and serializable ones
    }
}
