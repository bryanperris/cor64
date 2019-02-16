using cor64;
using cor64.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N64AsmConsole
{
    class Program
    {
        private static Stream _Assemble(params String[] asm)
        {
            N64Assembler assembler = new N64Assembler();

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch n64.cpu";
            source += "endian msb";

            StringBuilder line = new StringBuilder();

            for (int i = 0; i < asm.Length; i++)
            {
                line.Append(asm[i]);
                if (i + 1 < asm.Length) line.Append(" ");
            }

            source += line.ToString();

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);
            assembler.AssembleCode(true);

            /* Get the output */
            var streamOut = assembler.Output;
            streamOut.Position = 0;
            var swap = new Swap32Stream(streamOut);

            return swap;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Assemble a single instruction and see its binary");

            while (true)
            {
                try
                {
                    Console.Write("ASM: ");
                    String asm = Console.ReadLine();
                    var bin = _Assemble(asm);

                    bin.Position = 0;
                    byte[] bytes = new byte[4];

                    bin.Read(bytes, 0, bytes.Length);

                    Console.WriteLine("BIN: {0}{1}{2}{3}", 
                        Convert.ToString(bytes[3], 2).PadLeft(8, '0'), 
                        Convert.ToString(bytes[2], 2).PadLeft(8, '0'), 
                        Convert.ToString(bytes[1], 2).PadLeft(8, '0'), 
                        Convert.ToString(bytes[0], 2).PadLeft(8, '0'));
                }
                catch (Exception e)
                {
                    Console.WriteLine("FAIL: " + e.Message + "\n");
                }
            }
        }
    }
}
