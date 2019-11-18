using System;
using System.IO;
using System.Text;
using cor64;
using cor64.BareMetal;
using cor64.IO;

namespace Tests
{

    public static class Asm
    {

        public static String ReadInternalAsm(Type self, String name)
        {
            using (var s = self.Assembly.GetManifestResourceStream(self, name))
            {
                StreamReader reader = new StreamReader(s);
                return reader.ReadToEnd();
            }
        }

        public static Byte[] ToBytes(this N64Assembler assembler)
        {
            var o = assembler.Output;
            o.Position = 0;
            byte[] b = new byte[o.Length];
            o.Position = 0;
            o.Read(b, 0, b.Length);
            return b;
        }

        public static Byte[] AssembleBareMetal(String title, AssemblyTextSource src, bool byteswap = false)
        {
            var assembler = new N64BareMetalAssembler(title);
            assembler.AddAssemblySource(src);

            if (!byteswap)
                assembler.AssembleCode(true);
            else
                assembler.AssembleCodeAndSwap(true);

            return assembler.ToBytes();
        }

        public static N64BareMetalAssembler AssembleBareMetalAndPeek(String title, AssemblyTextSource src, bool byteswap = false)
        {
            var assembler = new N64BareMetalAssembler(title);

            assembler.AddAssemblySource(src);
            if (!byteswap)
                assembler.AssembleCode(true);
            else
                assembler.AssembleCodeAndSwap(true);

            return assembler;
        }

        public static Byte[] Assemble(AssemblyTextSource src)
        {
            var assembler = new N64Assembler();
            assembler.AddAssemblySource(src);
            assembler.AssembleCode(true);
            return assembler.ToBytes();
        }

        public static Stream Assemble(params String[] asm)
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

            return streamOut;
        }

        public static N64Assembler AssembleAndPeek(AssemblyTextSource src)
        {
            var assembler = new N64Assembler();
            assembler.AddAssemblySource(src);
            assembler.AssembleCode(true);
            return assembler;
        }

        private static Byte[] AssembleCartTypeA(bool swap)
        {
            var a = new AssemblyTextSource("main");

            a += ReadInternalAsm(typeof(Asm), "ProgSampleA.asm");

            return AssembleBareMetal("N64 INITIALIZE", a, swap);
        }

        public static Cartridge AssembleSimpleCart(bool swap = false)
        {
            var result = AssembleCartTypeA(swap);

            /* Dump to file */
            Stream d = new MemoryStream();
            d.Write(result, 0, result.Length);
            d.Position = 0;

            return new Cartridge(d);
        }

        public static Cartridge AssembleSimpleCartNoMagic(bool swap = false)
        {
            var result = AssembleCartTypeA(swap);

            /* Dump to file */
            Stream d = new MemoryStream();
            d.Write(result, 0, result.Length);
            d.Position = 0;

            // Clear away the bus configuration
            for (int i = 0; i < 4; i++)
            {
                d.WriteByte(0);
            }

            d.Position = 0;

            return new Cartridge(d);
        }
    }
}