using cor64;
using cor64.Mips.R4300I;
using NUnit.Framework;
using cor64.BareMetal;
using System;
using System.IO;

namespace Tests {

    [TestFixture]
    public class EndianessTests : BaseTest {

        // [Test]
        // public void BigEndianTest() {
        //     var machine = MachineSpawner.CreateAndRun();
        //     Assert.AreEqual("DEADBEEF", machine.Probe_ReadMem32(0x00000000).ToString("X8"));
        // }

        // [Test]
        // public void LittleEndianTest() {
        //     CoreConfig.Current.ByteSwap = false;
        //     var machine = MachineSpawner.CreateAndRun();
        //     Assert.AreEqual("DEADBEEF", machine.Probe_ReadMem32(0x00000000).ToString("X8"));
        // }
    }
}