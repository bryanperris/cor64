using System;
using System.Collections.Generic;
using cor64.BassSharp;
using cor64.IO;

namespace cor64.BareMetal {
    public class N64BareMetalRdpAssembler : N64Assembler {
        private readonly HashSet<String> m_ValidSources = new HashSet<string>() {
            "LIB/N64.INC",
            "LIB/N64_GFX.INC"
        };

        public N64BareMetalRdpAssembler()
        {

        }

        private String ResolveSource(String name)
        {
            var test = name.ToUpper();

            if (m_ValidSources.Contains(name)) {
                return test.LeftMatchAndTrim("LIB/");
            }
            else {
                return null;
            }
        }

        protected override ISource RequestCodeSource(string name)
        {
            var resolvedName = ResolveSource(name);

            if (resolvedName != null) {
               return LoadSourceFromManifest(resolvedName, GetType());
            }

            return base.RequestCodeSource(name);
        }

        protected override ISource RequestBinarySource(string name)
        {
            var resolvedName = ResolveSource(name);
            
            if (resolvedName != null) {
                using (var s = OpenResourceStream(GetType(), resolvedName)) {
                    return new BinarySource(s);
                }
            }
            else {
                return base.RequestBinarySource(name);
            }
        }

        public override void AssembleCode(bool strict = false) {
            base.AssembleCode(strict);

            var asmStream = Output;
            asmStream.Position = 0;
            Byte[] asmBytes = new Byte[asmStream.Length];
            asmStream.Read(asmBytes, 0, asmBytes.Length);
        }

        public void AssembleCodeAndSwap(bool strict = false) {
            AssembleCode(strict);

            // Byteswap the rom stream from big to little
            var temp = new byte[Output.Length];
            var source = new Swap32Stream(Output);

            source.Position = 0;
            source.Read(temp, 0, temp.Length);
            source.Position = 0;
            Output.Write(temp, 0, temp.Length);
        }
    }
}