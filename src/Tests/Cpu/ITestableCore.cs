using cor64.Mips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Cpu
{
    internal interface ITestableCore
    {
        void Init(TestCase tester);

        void StepOnce();

        void TestExpectations();

        void SetProcessorMode(ProcessorMode mode);
    }
}
