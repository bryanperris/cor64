using System;
using System.IO;
using cor64;
using cor64.BareMetal;

namespace RdpTests
{
    public static class Asm
    {

        public static String ReadInternalAsm(Type self, String name)
        {
            using var s = self.Assembly.GetManifestResourceStream(self, name);
            StreamReader reader = new StreamReader(s);
            return reader.ReadToEnd();
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

        public static Byte[] AssembleDisplayList(String resourceFileName)
        {
            var assembly = new AssemblyTextSource("displaylist");
            assembly += ReadInternalAsm(typeof(Asm), resourceFileName);

            var assembler = new N64BareMetalRspAssembler();

            assembler.AddAssemblySource(assembly);
            assembler.AssembleCode(true);

            return assembler.ToBytes();
        }
    }
}