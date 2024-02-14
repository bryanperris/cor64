using System.IO;
using System.Text;
using cor64;
using cor64.IO;
using cor64.RCP;
using cor64.Rdp;
using cor64.Rdp.LLE;

namespace Tests.RdpLLE {
    public static class RdpHelper {
        private static readonly ushort[] s_TlutData = new ushort[] {
            0x0000,0x0100,0x0200,0x0300,0x0400,0x0500,0x0600,0x0700,0x0800,0x0900,0x0A00,0x0B00,0x0C00,0x0D00,0x0E00,0x0F00,
            0x1000,0x1100,0x1200,0x1300,0x1400,0x1500,0x1600,0x1700,0x1800,0x1900,0x1A00,0x1B00,0x1C00,0x1D00,0x1E00,0x1F00,
            0x2000,0x2100,0x2200,0x2300,0x2400,0x2500,0x2600,0x2700,0x2800,0x2900,0x2A00,0x2B00,0x2C00,0x2D00,0x2E00,0x2F00,
            0x3000,0x3100,0x3200,0x3300,0x3400,0x3500,0x3600,0x3700,0x3800,0x3900,0x3A00,0x3B00,0x3C00,0x3D00,0x3E00,0x3F00,
            0x4000,0x4100,0x4200,0x4300,0x4400,0x4500,0x4600,0x4700,0x4800,0x4900,0x4A00,0x4B00,0x4C00,0x4D00,0x4E00,0x4F00,
            0x5000,0x5100,0x5200,0x5300,0x5400,0x5500,0x5600,0x5700,0x5800,0x5900,0x5A00,0x5B00,0x5C00,0x5D00,0x5E00,0x5F00,
            0x6000,0x6100,0x6200,0x6300,0x6400,0x6500,0x6600,0x6700,0x6800,0x6900,0x6A00,0x6B00,0x6C00,0x6D00,0x6E00,0x6F00,
            0x7000,0x7100,0x7200,0x7300,0x7400,0x7500,0x7600,0x7700,0x7800,0x7900,0x7A00,0x7B00,0x7C00,0x7D00,0x7E00,0x7F00,
            0x8000,0x8100,0x8200,0x8300,0x8400,0x8500,0x8600,0x8700,0x8800,0x8900,0x8A00,0x8B00,0x8C00,0x8D00,0x8E00,0x8F00,
            0x9000,0x9100,0x9200,0x9300,0x9400,0x9500,0x9600,0x9700,0x9800,0x9900,0x9A00,0x9B00,0x9C00,0x9D00,0x9E00,0x9F00,
            0xA000,0xA100,0xA200,0xA300,0xA400,0xA500,0xA600,0xA700,0xA800,0xA900,0xAA00,0xAB00,0xAC00,0xAD00,0xAE00,0xAF00,
            0xB000,0xB100,0xB200,0xB300,0xB400,0xB500,0xB600,0xB700,0xB800,0xB900,0xBA00,0xBB00,0xBC00,0xBD00,0xBE00,0xBF00,
            0xC000,0xC100,0xC200,0xC300,0xC400,0xC500,0xC600,0xC700,0xC800,0xC900,0xCA00,0xCB00,0xCC00,0xCD00,0xCE00,0xCF00,
            0xD000,0xD100,0xD200,0xD300,0xD400,0xD500,0xD600,0xD700,0xD800,0xD900,0xDA00,0xDB00,0xDC00,0xDD00,0xDE00,0xDF00,
            0xE000,0xE100,0xE200,0xE300,0xE400,0xE500,0xE600,0xE700,0xE800,0xE900,0xEA00,0xEB00,0xEC00,0xED00,0xEE00,0xEF00,
            0xF000,0xF100,0xF200,0xF300,0xF400,0xF500,0xF600,0xF700,0xF800,0xF900,0xFA00,0xFB00,0xFC00,0xFD00,0xFE00,0xFF00
        };

        public static void AssembleAnInjectDL(this N64System system, params string[] lines) {
            var displayList = Asm.AssembleSingleCommandDisplayList(lines);

            // Write the display list bytes into beginning of RDRAM
            for (int i = 0; i < displayList.Length; i++) {
                system.SystemMemory.U8(0, displayList[i]);
            }

            // Setup the DP interface
            system.DeviceRcp.DisplayProcessorCommandInterface.DirectDLSetup(0, displayList.Length);
        }

        public static N64System CreateTestableSystem() {
            N64System system = new N64System();
            var rdp = new MadCatRdp();
            rdp.SetDLDebug(true);
            system.DeviceRcp.SetRdpDevice(rdp);
            system.BootForTesting();
            return system;
        }

        public static void RdpExecute(this N64System system) {
            system.DeviceRcp.DisplayProcessorCommandInterface.DirectDLExecute();
        }

        public static MadCatRdp GetRdp(this N64System system) {
            return (MadCatRdp)system.DeviceRcp.DeviceRdp;
        }

        public static void SetTestData(this MadCatRdp rdp) {
            var gs = rdp.GraphicsState;

            gs.CombinedColor.SetColor(Color.Zero);
            gs.PixelColor.SetColor(Color.Zero);

            gs.FogColor.SetColor(new Color("D4404020"));
            gs.PrimColor.SetColor(new Color("D4AF40FF"));
            gs.EnvironmentColor.SetColor(new Color("B4D440F0"));
            gs.MemoryColor.SetColor(new Color("9BF24A00"));

            gs.KeyCenter.SetColor(new Color("ADADAD00"));
            gs.KeyScale = new Color("F7F7F700");
            gs.KeyWidth = new Color("CFCFCF00");

            // Select Environment Color for S0
            rdp.RdpCombiner.S0.SelectAll(5);

            // Select Prim Color for S1
            rdp.RdpCombiner.S1.SelectAll(3);

            var blenderSelections0 = new Blender.BlendInputSelections(1, 1, 1, 1);

            var blenderSelections1 = new Blender.BlendInputSelections(1, 1, 1, 1);

            rdp.RdpBlender.UpdateInputs(blenderSelections0, blenderSelections1);

            gs.MinLevel = 1;
            gs.MaxLevel = 10;
            gs.OtherModes.EnableTextureLevelOfDetail = true;
            gs.Pipeline_UseLod = true;

            // Test tile for texcoord testing only
            rdp.MakeTile(0, ImageDataFormat.RGBA, ImageSizeType.SizeInBytes_4, 4, 0, 0, true, true, 5, 2, true, true, 5, 1, 1, 100, 2, 70);

            // RGBA TLUT Test Tile
            rdp.MakeTile(1, ImageDataFormat.RGBA, ImageSizeType.SizeInBytes_4, 0, 0x100, 0, false, false, 0, 0, false, false, 0, 0, 0, 0, 0, 0);

            rdp.DeduceDerivatives();
        }
        public static void TestSpanDataFlipped(this MadCatRdp rdp) {
            var gs = rdp.GraphicsState;

            gs.SpanBuffer[0].rx = 0;
            gs.SpanBuffer[0].lx = 10;
            gs.SpanBuffer[0].ValidLine = true;

            unsafe {
                gs.SpanBuffer[0].InvalidYScan[0] = false;
                gs.SpanBuffer[0].InvalidYScan[1] = false;
                gs.SpanBuffer[0].InvalidYScan[2] = false;
                gs.SpanBuffer[0].InvalidYScan[3] = false;

                gs.SpanBuffer[0].minorx[0] = 10 << 3;
                gs.SpanBuffer[0].minorx[1] = 9 << 3;
                gs.SpanBuffer[0].minorx[2] = 8 << 3;
                gs.SpanBuffer[0].minorx[3] = 7 << 3;

                gs.SpanBuffer[0].majorx[0] = 6 << 3;
                gs.SpanBuffer[0].majorx[1] = 5 << 3;
                gs.SpanBuffer[0].majorx[2] = 4 << 3;
                gs.SpanBuffer[0].majorx[3] = 3 << 3;
            }
        }

        public static void TestSpanData(this MadCatRdp rdp) {
            var gs = rdp.GraphicsState;

            gs.SpanBuffer[0].lx = 0;
            gs.SpanBuffer[0].rx = 10;
            gs.SpanBuffer[0].ValidLine = true;

            unsafe {
                gs.SpanBuffer[0].InvalidYScan[0] = false;
                gs.SpanBuffer[0].InvalidYScan[1] = false;
                gs.SpanBuffer[0].InvalidYScan[2] = false;
                gs.SpanBuffer[0].InvalidYScan[3] = false;

                 gs.SpanBuffer[0].minorx[0] = 3 << 3;
                 gs.SpanBuffer[0].minorx[1] = 4 << 3;
                 gs.SpanBuffer[0].minorx[2] = 5 << 3;
                 gs.SpanBuffer[0].minorx[3] = 6 << 3;

                 gs.SpanBuffer[0].majorx[0] = 7 << 3;
                 gs.SpanBuffer[0].majorx[1] = 8 << 3;
                 gs.SpanBuffer[0].majorx[2] = 9 << 3;
                 gs.SpanBuffer[0].majorx[3] = 10 << 3;
            }
        }

        public static void TestTlutData(this MadCatRdp rdp) {
            
        }

        private static void MakeTile(
            this MadCatRdp rdp,
            int tileIndex,
            ImageDataFormat format,
            ImageSizeType size,
            int lineSize,
            int tmemAddress,
            int palette,
            bool enableClampT,
            bool enableMirrorT,
            int maskT,
            int shiftT,
            bool enableClampS,
            bool enableMirrorS,
            int maskS,
            int shiftS,
            int lowS,
            int highS,
            int lowT,
            int highT)
             {
                var tiles = rdp.GraphicsState.Tiles;
                
                tiles[tileIndex] = new Tile();
                tiles[tileIndex].Format = (int)format;
                tiles[tileIndex].Line = lineSize;
                tiles[tileIndex].S.Mask = maskS;
                tiles[tileIndex].T.Mask = maskT;
                tiles[tileIndex].Palette = palette;
                tiles[tileIndex].S.Shift = shiftS;
                tiles[tileIndex].T.Shift = shiftT;
                tiles[tileIndex].Size = (int)size;
                tiles[tileIndex].TmemAddress = tmemAddress;
                tiles[tileIndex].S.UseClamping = enableClampS;
                tiles[tileIndex].T.UseClamping = enableClampT;
                tiles[tileIndex].S.UseMirroring = enableMirrorS;
                tiles[tileIndex].T.UseMirroring = enableMirrorT;
                tiles[tileIndex].S.CoordLow = lowS;
                tiles[tileIndex].S.CoordHigh = highS;
                tiles[tileIndex].T.CoordLow = lowT;
                tiles[tileIndex].T.CoordHigh = highT;

                tiles[tileIndex].ComputeClampDiffs();
                tiles[tileIndex].ComputeDerivs();
            }
    }
}