using cor64;
using NUnit.Framework;

namespace Tests {

    [TestFixture]
    public class CartridgeTests : BaseTest {
        
        [Test]
        public void SimpleCartTest() {
            var cart = Asm.AssembleSimpleCart();
            Assert.AreEqual(0xF, cart.ClockRate);
            Assert.AreEqual(SecurityChipsetType.X102, cart.IPL.Cic);
            Assert.AreEqual(Cartridge.MAGIC_BIG.ToString("X8"), cart.PiBusConfig.ToString("X8"));

            // CoreConfig.Current.ByteSwap = false;
            // cart = Asm.AssembleSimpleCart(true);
            // Assert.AreEqual(0xF, cart.ClockRate);
            // Assert.AreEqual(SecurityChipsetType.X102, cart.IPL.Cic);
            // Assert.AreEqual(Cartridge.MAGIC_LITTLE.ToString("X8"), cart.PiBusConfig.ToString("X8"));

            cart = Asm.AssembleSimpleCartNoMagic();
            Assert.AreEqual(0xF, cart.ClockRate);
            Assert.AreEqual(SecurityChipsetType.X102, cart.IPL.Cic);
            Assert.AreEqual("00000000", cart.PiBusConfig.ToString("X8"));

            // CoreConfig.Current.ByteSwap = false;
            // cart = Asm.AssembleSimpleCartNoMagic(true);
            // Assert.AreEqual(0xF, cart.ClockRate);
            // Assert.AreEqual(SecurityChipsetType.X102, cart.IPL.Cic);
            // Assert.AreEqual("00000000", cart.PiBusConfig.ToString("X8"));
        }
    }
}