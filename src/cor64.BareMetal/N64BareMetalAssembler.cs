using cor64;
using cor64.BassSharp;
using cor64.IO;
using cor64.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BareMetal
{
    public class N64BareMetalAssembler : N64Assembler
    {
        private String m_Title = "N64 Program";
        private const int SIZE = 27;

        public uint LastCrc1 { get; private set; }

        public uint LastCrc2 { get; private set; }

        private HashSet<String> m_ValidSources = new HashSet<string>() {
            "LIB/N64.INC",
            "LIB/N64_3DCP1.INC",
            "LIB/N64_BOOTCODE.BIN",
            "LIB/N64_GFX.INC",
            "LIB/N64_HEADER.ASM",
            "LIB/N64_INPUT.INC",
            "LIB/N64_INTERRUPT.INC",
            "LIB/N64_RSP.INC"
        };

        public N64BareMetalAssembler(String title)
        {
            m_Title = title;

            if (m_Title.Length < SIZE) {
                StringBuilder sb = new StringBuilder(m_Title);
                int padsize = SIZE - m_Title.Length;
                for (int i = 0; i < padsize; i++) sb.Append(' ');
                m_Title = sb.ToString();
            }
            else {
                if (m_Title.Length > SIZE)
                    throw new ArgumentException("title too large");
            }
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
                var src = LoadSourceFromManifest(resolvedName, GetType());

                /* Hack the title in */
                if (src.Name == "N64_HEADER.ASM") {
                    src.AssemblyLines[0] = src.AssemblyLines[0].Replace("N64 PROGRAM TITLE          ", m_Title);
                }

                return src;
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

            UpdateRomChecksum(SecurityChipsetType.X102);
        }

        public void UpdateRomChecksum(SecurityChipsetType cicType) {
            Byte[] buffer = new Byte[CartridgeRomChecksum.InputSize];
            Output.Position = 0;
            Output.Read(buffer, 0, buffer.Length);

            using var crc = new CartridgeRomChecksum(cicType);
            crc.ComputeHash(buffer, 0, buffer.Length);
            crc.UpdateRomChecksum(Output);
            LastCrc1 = crc.CRC1;
            LastCrc2 = crc.CRC2;
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
