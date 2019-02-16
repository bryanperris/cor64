using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    [Serializable]
    public class VirtualBreakpointException : Exception
    {
        // TODO: Lazy, need to support the different exception base constructors and serializable ones
    }
}
