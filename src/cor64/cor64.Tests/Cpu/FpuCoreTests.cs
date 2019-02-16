using cor64.IO;
using cor64.Mips;
using cor64.Mips.R4300I;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static cor64.Tests.Cpu.Helper.ParamType;
using static cor64.Mips.ExceptionType;
using static cor64.Tests.Cpu.Helper;
using cor64.Mips.R4300I.CP0;
using cor64.Mips.R4300I.CP1;

using static cor64.Mips.FpuValueType;

namespace cor64.Tests.Cpu
{
    public class FpuCoreTests
    {

        [Fact]
        [Logging]
        public void AdditionTest()
        {
            "add.s".FPUTest(0.0f, 0.0f).Expect(FSingle, 0.0f).Run();
        }
    }
}
