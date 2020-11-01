using NUnit.Framework;
using static Tests.RdpTestCaseHelper;

namespace Tests {

    [TestFixture]
    public class RdpDecodeTests : BaseTest {
        [Test]
        public void SetScissorDecodeTest() {
            RdpTest("Set_Scissor 0<<2,0<<2, 0,0, 320<<2,240<<2").ExpectDecode("Set_Scissor 0,0, 0,0, 1280,960").Run();
        }

        [Test]
        public void SetOtherModeDecodeTest() {
            RdpTest("Set_Other_Modes CYCLE_TYPE_FILL").ExpectDecode("Set_Other_Modes CYCLE_TYPE_FILL").Run();
        }

        [Test]
        public void SetColorImageDecodeTest() {
            RdpTest("Set_Color_Image IMAGE_DATA_FORMAT_RGBA,SIZE_OF_PIXEL_32B,320-1, $00100000").ExpectDecode("Set_Color_Image IMAGE_DATA_FORMAT_RGBA, SIZE_OF_PIXEL_32B, 319, $00100000").Run();
        }

        [Test]
        public void SetFillColorDecodeTest() {
            RdpTest("Set_Fill_Color $AABBCCFF").ExpectDecode("Set_Fill_Color $AABBCCFF").Run();
        }

        [Test]
        public void FillRectangleDecodeTest() {
            RdpTest("Fill_Rectangle 319<<2,239<<2, 0<<2,0<<2").ExpectDecode("Fill_Rectangle 1276,956, 0,0").Run();
        }

        [Test]
        public void SyncFullDecodeTest() {
            RdpTest("Sync_Full").ExpectDecode("Sync_Full").Run();
        }

        [Test]
        public void SetTextureImageDecodeFormat() {
            RdpTest("Set_Texture_Image IMAGE_DATA_FORMAT_RGBA,SIZE_OF_PIXEL_32B,320-1, $00100000").ExpectDecode("Set_Texture_Image IMAGE_DATA_FORMAT_RGBA, SIZE_OF_PIXEL_32B, 319, $00100000").Run();
        }

        [Test]
        public void SetZImageDecodeTest() {
            RdpTest("Set_Z_Image $00100000").ExpectDecode("Set_Z_Image $00100000").Run();
        }

        [Test]
        public void SetTileDecodeTest() {
            RdpTest(
                "Set_Tile IMAGE_DATA_FORMAT_RGBA,SIZE_OF_PIXEL_32B,2, $130,1,1,CLAMP_T,MIRROR_T,MASK_T_5,SHIFT_T_6,CLAMP_S,MIRROR_S,MASK_S_5,SHIFT_S_6")
            .ExpectDecode(
                "Set_Tile IMAGE_DATA_FORMAT_RGBA, SIZE_OF_PIXEL_32B, 2, $130, 1, PALETTE_1, CLAMP_T, MIRROR_T, MASK_T_5, SHIFT_T_6, CLAMP_S, MIRROR_S, MASK_S_5, SHIFT_S_6"
            )
            .Run();
        }

        [Test]
        public void LoadTileDecodeTest() {
            RdpTest("Load_Tile 0<<2,0<<2, 0, 255<<2,7<<2").ExpectDecode("Load_Tile 0,0, 0, 1020,28").Run();
        }

        [Test]
        public void LoadBlockDecodeTest() {
            RdpTest("Load_Block 0<<2,0<<2, 0, 255<<2,7<<1").ExpectDecode("Load_Block 0,0, 0, 1020,14").Run();
        }

        [Test]
        public void SetTileSizeDecodeTest() {
            RdpTest("Set_Tile_Size 20<<2,40<<2, 0, 100<<2,25<<2").ExpectDecode("Set_Tile_Size 80,160, 0, 400,100").Run();
        }

        [Test]
        public void LoadTlutDecodeTest() {
            RdpTest("Load_Tlut 20<<2,0<<2, 2, 100<<2,0<<2").ExpectDecode("Load_Tlut 80,0, 2, 400,0").Run();
        }

        [Test]
        public void FillTriangleDecodeTest() {
            RdpTest("Fill_Triangle 0,0,0, 400,200,200, 25,0,0,0, 75,0,-1,0, 75,0,0,0")
                .ExpectDecode("Fill_Triangle 0,0,0, 400,200,200, 25,0,0,0, 75,0,-1,0, 75,0,0,0")
                .Run();
        }

        [Test]
        public void ShadedTriangleDecodeTest() {
            RdpTest(
                    "Shade_Triangle 0,0,0, 400,200,200, 25,0,0,0, 75,0,-1,0, 75,0,0,0",
                    "Shade_Coefficients 0,256,0,0, -5,5,0,0, 0,256,0,0, -5,5,0,0, 0,-5,5,0, 0,0,0,0, 0,-5,5,0, 0,0,0,0")
            .ExpectDecode(
                    "Shade_Triangle 0,0,0, 400,200,200, 25,0,0,0, 75,0,-1,0, 75,0,0,0",
                    "Shade_Coefficients 0,256,0,0, -5,5,0,0, 0,256,0,0, -5,5,0,0, 0,-5,5,0, 0,0,0,0, 0,-5,5,0, 0,0,0,0")
            .Run();
        }

        [Test]
        public void TexturedTriangleDecodeTest() {
            RdpTest(
                    "Texture_Triangle 0,0,0, 400,200,200, 25,0,0,0, 75,0,-1,0, 75,0,0,0",
                    "Texture_Coefficients 992,32,0, 128,0,0, 0,0,0, 0,0,0, -32,32,0, 0,0,0, 0,0,0, 0,0,0")
            .ExpectDecode(
                    "Texture_Triangle 0,0,0, 400,200,200, 25,0,0,0, 75,0,-1,0, 75,0,0,0",
                    "Texture_Coefficients 992,32,0, 128,0,0, 0,0,0, 0,0,0, -32,32,0, 0,0,0, 0,0,0, 0,0,0")
            .Run();
        }

        [Test]
        public void ZTriangleDecodeTest() {
            RdpTest(
                    "Fill_ZBuffer_Triangle 1,0,0, 300,200,200, 150,0,-1,0, 100,0,1,0, 100,0,0,0",
                    "ZBuffer_Coefficients 0,0,0,0, 8,0,0,0")
            .ExpectDecode(
                    "Fill_ZBuffer_Triangle 1,0,0, 300,200,200, 150,0,-1,0, 100,0,1,0, 100,0,0,0",
                    "ZBuffer_Coefficients 0,0,0,0, 8,0,0,0")
            .Run();
        }

        [Test]
        public void ShadedTexZTriangleDecodeTest() {
            RdpTest(
                    "Shade_Texture_Z_Buffer_Triangle 1,0,0, 300,200,200, 150,0,-1,0, 100,0,1,0, 100,0,0,0",
                    "Shade_Coefficients 0,256,0,0, -5,5,0,0, 0,256,0,0, -5,5,0,0, 0,-5,5,0, 0,0,0,0, 0,-5,5,0, 0,0,0,0",
                    "Texture_Coefficients 992,32,0, 128,0,0, 0,0,0, 0,0,0, -32,32,0, 0,0,0, 0,0,0, 0,0,0",
                    "ZBuffer_Coefficients 0,0,0,0, 8,0,0,0")
            .ExpectDecode(
                    "Shade_Texture_Z_Buffer_Triangle 1,0,0, 300,200,200, 150,0,-1,0, 100,0,1,0, 100,0,0,0",
                    "Shade_Coefficients 0,256,0,0, -5,5,0,0, 0,256,0,0, -5,5,0,0, 0,-5,5,0, 0,0,0,0, 0,-5,5,0, 0,0,0,0",
                    "Texture_Coefficients 992,32,0, 128,0,0, 0,0,0, 0,0,0, -32,32,0, 0,0,0, 0,0,0, 0,0,0",
                    "ZBuffer_Coefficients 0,0,0,0, 8,0,0,0")
            .Run();
        }

        [Test]
        public void TextureRectangleDecodeTest() {
            RdpTest("Texture_Rectangle 320<<2,6<<2, 0, 0<<2,0<<2, 0<<5,0<<5, 1<<10,1<<10")
                .ExpectDecode("Texture_Rectangle 1280,24, 0, 0,0, 0,0, 1024,1024")
                .Run();
        }

        [Test]
        public void TextureRectangleFlipDecodeTest() {
            RdpTest("Texture_Rectangle_Flip 320<<2,6<<2, 0, 0<<2,0<<2, 0<<5,0<<5, 1<<10,1<<10")
                .ExpectDecode("Texture_Rectangle_Flip 1280,24, 0, 0,0, 0,0, 1024,1024")
                .Run();
        }

        [Test]
        public void SetEnvColorDecodeTest() {
            RdpTest("Set_Env_Color $DDEEAAFF")
                .ExpectDecode("Set_Env_Color $DDEEAAFF")
                .Run();
        }

        [Test]
        public void SetPrimColorDecodeTest() {
            RdpTest("Set_Prim_Color 1, 2, $DDEEAAFF")
                .ExpectDecode("Set_Prim_Color 1, 2, $DDEEAAFF")
                .Run();
        }

        [Test]
        public void SetBlendColorDecodeTest() {
            RdpTest("Set_Blend_Color $DDEEAAFF")
                .ExpectDecode("Set_Blend_Color $DDEEAAFF")
                .Run();
        }

        [Test]
        public void SetFogColorDecodeTest() {
            RdpTest("Set_Fog_Color $DDEEAAFF")
                .ExpectDecode("Set_Fog_Color $DDEEAAFF")
                .Run();
        }

        [Test]
        public void SetPrimDepthDecodeTest() {
            RdpTest("Set_Prim_Depth 1, 2")
                .ExpectDecode("Set_Prim_Depth 1, 2")
                .Run();
        }

        [Test]
        public void SetConvertDecodeTest() {
            RdpTest("Set_Convert 175,-43,-89,222,114,42")
                .ExpectDecode("Set_Convert 175, -43, -89, 222, 114, 42")
                .Run();
        }

        [Test]
        public void SetKeyRDecodeTest() {
            RdpTest("Set_Key_R 100, 10, 23")
                .ExpectDecode("Set_Key_R 100, 10, 23")
                .Run();
        }

        [Test]
        public void SetKeyGBDecodeTest() {
            RdpTest("Set_Key_GB 100, 10, 23, 55, 33, 22")
                .ExpectDecode("Set_Key_GB 100, 10, 23, 55, 33, 22")
                .Run();
        }

        [Test]
        public void SetCombineModeDecodeTest() {
            RdpTest("Set_Combine_Mode $0,$00, 0,0, $6,$01, $0,$F, 1,0, 0,0,0, 7,7,7")
                .ExpectDecode("Set_Combine_Mode 0,0, 0,0, 6,1, 0,15, 1,0, 0,0,0, 7,7,7")
                .Run();
        }
    }
}