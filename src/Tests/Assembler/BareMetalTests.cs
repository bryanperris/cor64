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

namespace Tests.Assembler
{
    [TestFixture]
    public class BareMetalTests : BaseTest
    {
        [Test]
        public void InitializeProgramTest()
        {
            String checksum = "def6582b1b617d571be1ac37e0b755ff33d6d2d9c89e773c544f669f2f4d786d".ToUpper();
            var a = new AssemblyTextSource("main");
            a += Asm.ReadInternalAsm(this.GetType(), "Initialize.asm");
            var assembler = Asm.AssembleBareMetalAndPeek("N64 INITIALIZE", a);
            var result = assembler.ToBytes();

            Assert.AreEqual("B1DBA596", assembler.LastCrc1.ToString("X8"));
            Assert.AreEqual("949F511B", assembler.LastCrc2.ToString("X8"));

            /* Dump to file */
            // var d = File.Create("InitializeProgramTest.N64");
            // d.Write(result, 0, result.Length);
            // d.Flush();

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
