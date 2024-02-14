using System.Reflection.Metadata.Ecma335;
using System.Text;
using System;
using cor64.IO;

namespace cor64.HLE {
    public class FatalPrintf : CpuCodeHook {
        public FatalPrintf(Mips.R4300I.InterpreterBaseR4300I core) : base(core)
        {
        }

        public override ReturnControl Execute()
        {
            StringBuilder sb = new StringBuilder();

            // Get the string pointer
            var strPointer = (uint)MemHelper.VirtualToPhysical(ReadArg32(0));

            // Iterate the string until null terminator
            while (true) {
                char read = (char)Core.DataRead8(strPointer++);

                sb.Append(read);

                if (read == '\0') {
                    Console.WriteLine("HLE FatalPrintF: {0}", sb.ToString());
                    break;
                }

            }

            return ReturnControl.Normal;
        }
    }
}