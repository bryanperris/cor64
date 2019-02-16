using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    class Instruction
    {
        public String statement;
        public int ip;
        public int fileNumber;
        public int lineNumber;
        public int blockNumber;

        public override string ToString()
        {
            return statement;
        }
    }
}
