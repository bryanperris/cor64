using cor64.BareMetal;
using cor64.BassSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using static cor64.BassSharp.Bass;
using System.Security.Cryptography;
using NUnit.Framework;
using cor64;

namespace Tests.Asm
{
    [TestFixture]
    public class BareMetalTests : BaseTest
    {
        private Byte[] Assemble(String title, AssemblyTextSource src)
        {
            var assembler = new N64BareMetalAssembler(title);
            assembler.AddAssemblySource(src);
            assembler.AssembleCode(true);
            var o = assembler.Output;
            byte[] b = new byte[o.Length];
            o.Position = 0;
            o.Read(b, 0, b.Length);
            return b;
        }

        private N64BareMetalAssembler AssembleAndPeek(String title, AssemblyTextSource src)
        {
            var assembler = new N64BareMetalAssembler(title);
            assembler.AddAssemblySource(src);
            assembler.AssembleCode(true);
            return assembler;
        }

        private String ReadInternalAsm(String name)
        {
            using (var s = GetType().Assembly.GetManifestResourceStream(GetType(), name)) {
                StreamReader reader = new StreamReader(s);
                return reader.ReadToEnd();
            }
        }

        [Test]
        public void InitializeProgramTest()
        {
            /* Prehash of the rom but with CRCs stripped out */
            String checksum = "FF4010A9E53D43F7FD9A68E62DB59D027160E6F6D06A0886F5B3B4C449328EF5";
            var a = new AssemblyTextSource("main");
            a += ReadInternalAsm("Initialize.asm");
            var result = Assemble("N64 INITIALIZE", a);

            /* Dump to file */
            var d = File.Create("InitializeProgramTest.N64");
            d.Write(result, 0, result.Length);
            d.Flush();

            Assert.True(result.Length == 1052672);

            var hasher = SHA256.Create();
            hasher.Initialize();
            var hash = hasher.ComputeHash(result);
            String assembledHash = BitConverter.ToString(hash).Replace("-", String.Empty);

            Assert.AreEqual(checksum.Length, assembledHash.Length);
            Assert.AreEqual(checksum, assembledHash);
        }
    }
}
