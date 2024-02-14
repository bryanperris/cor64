using System.Reflection.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cor64.Mips.Analysis
{
    public enum BlockEnding {
        None,
        End, // hit the end of code memory
        NormalJump,  // eg; either a jump or branch + delayslot
        ExceptionReturn,
        Interrupted
    }
}
