using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I.CP1
{
    public class SimpleFpu : FpuInterpreter
    {
        public SimpleFpu(Interpreter cpu, Action<DecodedInstruction>[] parentCallTable) : base(cpu, parentCallTable)
        {
        }

        protected override void Add(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }
    }
}
