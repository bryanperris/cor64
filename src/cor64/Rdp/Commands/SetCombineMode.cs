using System;
namespace cor64.Rdp.Commands {
    public class SetCombineMode : RdpCommand {
        public SetCombineMode(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int AddAlphaCycle1 => ReadField(0, 0b111);

        public int SubBAlphaCycle1 => ReadField(3, 0b111);

        public int AddColorCycle1 => ReadField(6, 0b111);

        public int AddAlphaCycle0 => ReadField(9, 0b111);

        public int SubBAlphaCycle0 => ReadField(12, 0b111);

        public int AddColorCycle0 => ReadField(15, 0b111);

        public int MulAlphaCycle1 => ReadField(18, 0b111);

        public int SubAAlphaCycle1 => ReadField(21, 0b111);

        public int SubBColorCycle1 => ReadField(24, 0b1111);

        public int SubBColorCycle0 => ReadField(28, 0b1111);

        public int MulColorCycle1 => ReadField(32, 0b11111);

        public int SubAColorCycle1 => ReadField(37, 0b1111);

        public int MulAlphaCycle0 => ReadField(41, 0b111);

        public int SubAAlphaCycle0 => ReadField(44, 0b111);

        public int MulColorCycle0 => ReadField(47, 0b11111);

        public int SubAColorCycle0 => ReadField(52, 0b1111);

        protected override String Params() => String.Format(
            "{0},{1}, {2},{3}, {4},{5}, {6},{7}, {8},{9}, {10},{11},{12}, {13},{14},{15}",
                SubAColorCycle0,
                MulColorCycle0,
                SubAAlphaCycle0,
                MulAlphaCycle0,
                SubAColorCycle1,
                MulColorCycle1,
                SubBColorCycle0,
                SubBColorCycle1,
                SubAAlphaCycle1,
                MulAlphaCycle1,
                AddColorCycle0,
                SubBAlphaCycle0,
                AddAlphaCycle0,
                AddColorCycle1,
                SubBAlphaCycle1,
                AddAlphaCycle1
            );
    }
}