using System;
using cor64.Mips.Rsp;

namespace Tests {
    public class RspTestCase {
        public String UCodeName { get; }

        public RspTestCase(String ucodeName) {
            UCodeName = ucodeName;
        }

        public RspVector? ExpectedResult { get; set; } = null;

        public RspVector? SourceA { get; set; } = null;

        public RspVector? SourceB { get; set; } = null;

        public RspVector[] ExpectedAcc { get; set; } = null;

        public ushort? ExpectedCarry { get; set; } = null;

        public ushort? ExpectedCompare { get; set; } = null;

        public byte? ExpectedExtension { get; set; } = null;

        public RspVector[] InjectedAcc { get; set; } = null;

        public ushort? InjectedVcc { get; set; } = null;
    }
}