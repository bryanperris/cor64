using System;
using cor64.Rdp.Commands;
using System.Diagnostics;
using NLog;
using System.IO;
using cor64.IO;
using System.Collections.Generic;
using cor64.Mips;

namespace cor64.Rdp.LLE {
    public class MadCatRdp : DrawProcessor
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public State GraphicsState { get; }
        public readonly Blender RdpBlender;
        public readonly Combiner RdpCombiner;
        public readonly Rasterizer RdpRasterizer;
        public readonly TextureUnit RdpTextureUnit;
        public readonly TextureMemory RdpTextureMemory;

        internal Rdram RdramMemory { get; private set; }
        internal Framebuffer N64Framebuffer { get; private set; }

        public override String Description => "MadCat RDP";

        public int Stats_Triangles { get; set; }

        public MadCatRdp() {
            ZBuffer.InitZBuffer();
            GraphicsState = new State();
            RdpBlender = new Blender(this);
            RdpCombiner = new Combiner(this);
            RdpRasterizer = new Rasterizer(this);
            RdpTextureUnit = new TextureUnit(this);
            RdpTextureMemory = new TextureMemory(this);
        }

        public override void Init() {
            RdramMemory.Init();

             for (int i = 0; i < GraphicsState.Tiles.Length; i++) {
               GraphicsState.Tiles[i] = new Tile();
               GraphicsState.Tiles[i].Reset();
            }

            GraphicsState.WorkerOffset = 0;
            GraphicsState.RandomSeed = 3 + (GraphicsState.WorkerOffset * 13);

            var blenderSelections0 = new Blender.BlendInputSelections(0,0,0,0);
            var blenderSelections1 = new Blender.BlendInputSelections(0,0,0,0);

            RdpBlender.UpdateInputs(blenderSelections0, blenderSelections1);
        }

        public override void AttachMemory(N64MemoryController.N64MemoryStream stream) {
            base.AttachMemory(stream);
            RdramMemory = new Rdram(stream);
            N64Framebuffer = new Framebuffer(this);
        }

        public void DeduceDerivatives() {
            var GS = GraphicsState;
            var CT = (CycleType)GS.OtherModes.CycleType;

            GS.OtherModes.Flags.UsePartialReject_1Cycle =
                RdpBlender.S0.Mode2.IsAlphaRefEqual(GS.InvPixelColor) && RdpBlender.S0.Mode1.IsAlphaRefEqual(GS.PixelColor);

            GS.OtherModes.Flags.UsePartialReject_2Cycle =
                RdpBlender.S1.Mode2.IsAlphaRefEqual(GS.InvPixelColor) && RdpBlender.S1.Mode1.IsAlphaRefEqual(GS.PixelColor);

            bool specialBSel0 = RdpBlender.S0.Mode2.IsAlphaRefEqual(GS.MemoryColor);
            bool specialBSel1 = RdpBlender.S1.Mode2.IsAlphaRefEqual(GS.MemoryColor);

            RdpBlender.RealShifterNeeded =
                (specialBSel0 && CT == CycleType.Type1) ||
                (specialBSel1 && CT == CycleType.Type2);

            RdpBlender.InterPixelShifterNeeded =
                specialBSel0 && CT == CycleType.Type2;

            GS.OtherModes.Flags.RgbaDither = (GS.OtherModes.RgbDitherSelection << 2) | GS.OtherModes.AlphaDitherSelection;

            bool lod_frac_used_in_cc1 = RdpCombiner.S1.Multiply.ColorTag == nameof(GS.LodFraction) || RdpCombiner.S1.Multiply.AlphaTag == nameof(GS.LodFraction);
            bool lod_frac_used_in_cc0 = RdpCombiner.S0.Multiply.ColorTag == nameof(GS.LodFraction) || RdpCombiner.S0.Multiply.AlphaTag == nameof(GS.LodFraction);

            static bool UsedInCC0(CombinerState CS, ColorRef texel, bool usedInAC0) {
                bool result = false;

                result |= CS.Multiply.IsColorRefEqual(texel);
                result |= CS.SubtractA.IsColorRefEqual(texel);
                result |= CS.SubtractB.IsColorRefEqual(texel);
                result |= CS.Add.IsColorRefEqual(texel);

                result |= usedInAC0;

                return result;
            }

            static bool UsedInCC1(CombinerState CS, ColorRef texel) {
                bool result = false;

                result |= CS.Multiply.IsColorRefEqual(texel);
                result |= CS.SubtractA.IsColorRefEqual(texel);
                result |= CS.SubtractB.IsColorRefEqual(texel);
                result |= CS.Add.IsColorRefEqual(texel);

                result |= CS.Multiply.IsAlphaRefEqual(texel);
                result |= CS.SubtractA.IsAlphaRefEqual(texel);
                result |= CS.SubtractB.IsAlphaRefEqual(texel);
                result |= CS.Add.IsAlphaRefEqual(texel);

                return result;
            }

            static bool UsedInAC0(CombinerState CS, ColorRef texel) {
                bool result = false;
                result |= CS.Multiply.IsAlphaRefEqual(texel);
                result |= CS.SubtractA.IsAlphaRefEqual(texel);
                result |= CS.SubtractB.IsAlphaRefEqual(texel);
                result |= CS.Add.IsAlphaRefEqual(texel);
                return result;
            }

            bool texel1_used_in_cc1 = UsedInCC1(RdpCombiner.S1, GS.Texel1Color);
            bool texel0_used_in_cc1 = UsedInCC1(RdpCombiner.S1, GS.Texel0Color);

            bool texel1_used_in_ac0 = UsedInAC0(RdpCombiner.S0, GS.Texel1Color);
            bool texel0_used_in_ac0 = UsedInAC0(RdpCombiner.S0, GS.Texel0Color);

            bool texel1_used_in_cc0 = UsedInCC0(RdpCombiner.S0, GS.Texel1Color, texel1_used_in_ac0);
            bool texel0_used_in_cc0 = UsedInCC0(RdpCombiner.S0, GS.Texel0Color, texel0_used_in_ac0);

            bool texels_or_lf_used_in_ac0 =
                texel0_used_in_ac0 ||
                texel1_used_in_ac0 ||
                RdpCombiner.S0.Multiply.AlphaTag == nameof(GS.LodFraction);

            if (texel1_used_in_cc1) {
                GS.Cycle1RenderMode = 0;
            }
            else if (texel0_used_in_cc1 || lod_frac_used_in_cc1) {
                GS.Cycle1RenderMode = 1;
            }
            else {
                GS.Cycle1RenderMode = 2;
            }


            if (texel1_used_in_cc1 || (GS.OtherModes.EnableAlphaCompare && texels_or_lf_used_in_ac0))
            {
                GS.Cycle2RenderMode = 0;
            }
            else if (texel1_used_in_cc0 || texel0_used_in_cc1)
            {
                GS.Cycle2RenderMode = 1;
            }
            else if (texel0_used_in_cc0 || lod_frac_used_in_cc0 || lod_frac_used_in_cc1)
            {
                GS.Cycle2RenderMode = 2;
            }
            else
            {
                GS.Cycle2RenderMode = 3;
            }

            bool useLodFrac = (CT == CycleType.Type2 && (lod_frac_used_in_cc0 || lod_frac_used_in_cc1)) || (CT == CycleType.Type1 && lod_frac_used_in_cc1);

            if ((CT == CycleType.Type1 && RdpCombiner.S1.SubtractA.ColorTag == nameof(GS.Noise)) ||
                (CT == CycleType.Type2 && (RdpCombiner.S0.SubtractA.ColorTag == nameof(GS.Noise) || RdpCombiner.S1.SubtractA.ColorTag == nameof(GS.Noise))) ||
                GS.OtherModes.AlphaDitherSelection == 2) {
                    GS.OtherModes.Flags.GetDitherLevel = 0;
                }
            else if (GS.OtherModes.Flags.RgbaDither != 0xF) {
                GS.OtherModes.Flags.GetDitherLevel = 1;
            }
            else {
                GS.OtherModes.Flags.GetDitherLevel = 2;
            }

            GS.Pipeline_UseLod = GS.OtherModes.EnableTextureLevelOfDetail || useLodFrac;

            // switch ((CycleType)GS.OtherModes.CycleType) {
            //     case CycleType.Type1: Log.Debug("RDP Render Mode: Cycle 1, Mode {0}", GS.Cycle1RenderMode); break;
            //     case CycleType.Type2: Log.Debug("RDP Render Mode: Cycle 2, Mode {0}", GS.Cycle2RenderMode); break;
            //     case CycleType.Fill: Log.Debug("RDP Render Mode: Fill"); break;
            //     case CycleType.Copy: Log.Debug("RDP Render Mode: Copy"); break;
            // }
        }

        public virtual void TextureCoordDivision(int ss, int st, int sw, ref int sss, ref int sst)
        {
            if (GraphicsState.OtherModes.EnablePerspectiveTextures) {
                TexCoord.PerspectiveDivide(ss, st, sw, ref sss, ref sst);
            }
            else {
                TexCoord.NonPerspectiveDivide(ss, st, sw, ref sss, ref sst);
            }
        }

        protected override void SetCombineMode(RdpCommand command) {
            var cmd = command.As<SetCombineMode>();

            RdpCombiner.S0.InputSubtractA.RGBSelection =     cmd.SubAColorCycle0;
            RdpCombiner.S0.InputSubtractA.AlphaSelection =   cmd.SubAAlphaCycle0;
            RdpCombiner.S0.InputSubtractB.RGBSelection =     cmd.SubBColorCycle0;
            RdpCombiner.S0.InputSubtractB.AlphaSelection =   cmd.SubBAlphaCycle0;
            RdpCombiner.S0.InputMultiply.RGBSelection =      cmd.MulColorCycle0;
            RdpCombiner.S0.InputMultiply.AlphaSelection =    cmd.MulAlphaCycle0;
            RdpCombiner.S0.InputAdd.RGBSelection =           cmd.AddColorCycle0;
            RdpCombiner.S0.InputAdd.AlphaSelection =         cmd.AddAlphaCycle0;

            RdpCombiner.S1.InputSubtractA.RGBSelection =     cmd.SubAColorCycle1;
            RdpCombiner.S1.InputSubtractA.AlphaSelection =   cmd.SubAAlphaCycle1;
            RdpCombiner.S1.InputSubtractB.RGBSelection =     cmd.SubBColorCycle1;
            RdpCombiner.S1.InputSubtractB.AlphaSelection =   cmd.SubBAlphaCycle1;
            RdpCombiner.S1.InputMultiply.RGBSelection =      cmd.MulColorCycle1;
            RdpCombiner.S1.InputMultiply.AlphaSelection =    cmd.MulAlphaCycle1;
            RdpCombiner.S1.InputAdd.RGBSelection =           cmd.AddColorCycle1;
            RdpCombiner.S1.InputAdd.AlphaSelection =         cmd.AddAlphaCycle1;

            RdpCombiner.UpdateCombineMode();

            GraphicsState.OtherModes.Flags.IsDerivativesStale = true;
        }
    
        protected override void SetColor(RdpCommand command) {
            switch (command.Type.Flags) {
                case RdpCommandFlags.Primitive: {
                    var cmd = command.As<SetPrimColor>();

                    GraphicsState.MinLevel = cmd.PrimMinLevel;
                    GraphicsState.PrimitiveLODFactor = cmd.PrimLevelFraction;
                    GraphicsState.PrimColor.SetColor(new Color (cmd.Red, cmd.Green, cmd.Blue, cmd.Alpha));

                    break;
                }

                case RdpCommandFlags.Environment: {
                    var cmd = command.As<SetEnvColor>();

                    GraphicsState.EnvironmentColor.SetColor(new Color(cmd.Red, cmd.Green, cmd.Blue, cmd.Alpha));

                    break;
                }

                case RdpCommandFlags.Fog: {
                    var cmd = command.As<SetFogColor>();

                    GraphicsState.FogColor.SetColor(new Color(cmd.Red, cmd.Green, cmd.Blue, cmd.Alpha));

                    break;
                }


                case RdpCommandFlags.Blend: {
                    var cmd = command.As<SetBlendColor>();

                    GraphicsState.BlendColor.SetColor(new Color(cmd.Red, cmd.Green, cmd.Blue, cmd.Alpha));

                    break;
                }

                case RdpCommandFlags.Color: {
                    var cmd = command.As<SetFillColor>();

                    GraphicsState.FillColor = cmd.PackedColor;

                    break;
                }

                default: throw new RdpException("Unknown color type for SetColor");
            }
        }
    
        protected override void SetKey(RdpCommand command) {
            switch (command.Type.Flags) {
                case RdpCommandFlags.Red: {
                    var cmd = command.As<SetKeyR>();

                    GraphicsState.KeyWidth.R = cmd.WidthR;
                    GraphicsState.KeyCenter.R = cmd.CenterR;
                    GraphicsState.KeyScale.R = cmd.ScaleR;

                    break;
                }

                case RdpCommandFlags.GreenBlue: {
                    var cmd = command.As<SetKeyGB>();

                    GraphicsState.KeyWidth.G = cmd.WidthG;
                    GraphicsState.KeyWidth.B = cmd.WidthB;

                    GraphicsState.KeyCenter.G = cmd.CenterG;
                    GraphicsState.KeyCenter.B = cmd.CenterB;

                    GraphicsState.KeyScale.G = cmd.ScaleG;
                    GraphicsState.KeyScale.B = cmd.ScaleB;

                    break;
                }

                default: throw new RdpException("Invalid key type for SetKey");
            }
        }

        protected override void SetConvert(RdpCommand command) {
            var cmd = command.As<SetConvert>();

            GraphicsState.TextureFilter_K0 = (cmd.K0 << 1) + 1;
            GraphicsState.TextureFilter_K1 = (cmd.K1 << 1) + 1;
            GraphicsState.TextureFilter_K2 = (cmd.K2 << 1) + 1;
            GraphicsState.TextureFilter_K3 = (cmd.K3 << 1) + 1;
            GraphicsState.YuvConversion_K4 = cmd.K4;
            GraphicsState.YuvConversion_K5 = cmd.K5;
        }

        protected override void SetImage(RdpCommand command) {
            switch (command.Type.Flags) {
                case RdpCommandFlags.Texture: {
                    var cmd = command.As<SetTextureImage>();

                    GraphicsState.TextureInputFormat = (int)cmd.Format;
                    GraphicsState.TextureInputTexelSize = (int)cmd.Size;
                    GraphicsState.TextureInputWidth = cmd.Width + 1;
                    GraphicsState.TextureInputAddress = (uint)(cmd.DramAddress & 0x0FFFFFF);

                    break;
                }

                case RdpCommandFlags.Color: {
                    var cmd = command.As<SetColorImage>();

                    var framebufferAddress = (uint)(cmd.DramAddress & 0x0FFFFFF);

                    GraphicsState.FramebufferFormat = (int)cmd.Format;
                    GraphicsState.FramebufferSize = (int)cmd.Size;
                    GraphicsState.FramebufferWidth = cmd.Width + 1;
                    GraphicsState.FramebufferAddress = framebufferAddress;

                    // VideoInterface.SetFBFromRDP(framebufferAddress);
                    VideoInterface.SetFBWidthFromRDP(GraphicsState.FramebufferWidth);

                    //Log.Debug("RDP framebuffer config has been set");

                    break;
                }

                case RdpCommandFlags.Mask: {
                    var cmd = command.As<SetZImage>();

                    GraphicsState.ZBufferAddress = (uint)(cmd.DramAddress & 0x0FFFFFFF);

                    // TODO: Must pass this address to the Video interface

                    break;
                }

                default: throw new RdpException("Invalid image type for SetImage");
            }
        }

        protected override void SetTile(RdpCommand command) {
            var cmd = command.As<SetTile>();

            int index = cmd.TileIndex;

            GraphicsState.Tiles[index].Format = (int)cmd.Format;
            GraphicsState.Tiles[index].Size = (int)cmd.Size;
            GraphicsState.Tiles[index].Line = cmd.Line;
            GraphicsState.Tiles[index].TmemAddress = cmd.TMemAddress;
            GraphicsState.Tiles[index].Palette = cmd.Pallete;

            GraphicsState.Tiles[index].T.UseClamping = cmd.EnableClamp_T;
            GraphicsState.Tiles[index].T.UseMirroring = cmd.EnableMirror_T;
            GraphicsState.Tiles[index].T.Mask = cmd.Mask_T;
            GraphicsState.Tiles[index].T.Shift = cmd.Shift_T;

            GraphicsState.Tiles[index].S.UseClamping = cmd.EnableClamp_S;
            GraphicsState.Tiles[index].S.UseMirroring = cmd.EnableMirror_S;
            GraphicsState.Tiles[index].S.Mask = cmd.Mask_S;
            GraphicsState.Tiles[index].S.Shift = cmd.Shift_S;

            GraphicsState.Tiles[index].ComputeDerivs();
        }

        protected override void SetTileSize(RdpCommand command) {
            var cmd = command.As<SetTileSize>();

            int index = cmd.Tile;

            GraphicsState.Tiles[index].S.CoordLow = cmd.LowS;
            GraphicsState.Tiles[index].S.CoordHigh = cmd.HighS;

            GraphicsState.Tiles[index].T.CoordLow = cmd.LowT;
            GraphicsState.Tiles[index].T.CoordHigh = cmd.HighT;

            GraphicsState.Tiles[index].ComputeClampDiffs();
        }

        protected override void LoadBlock(RdpCommand command){
            var cmd = command.As<LoadBlock>();

            int index = cmd.Tile;

            int sl, sh, tl, dxt;

            GraphicsState.Tiles[index].S.CoordLow = sl = cmd.LowS;
            GraphicsState.Tiles[index].S.CoordHigh = sh = cmd.HighS;

            GraphicsState.Tiles[index].T.CoordLow = tl = cmd.LowT;
            GraphicsState.Tiles[index].T.CoordHigh = dxt = cmd.DxT;

            GraphicsState.Tiles[index].ComputeClampDiffs();

            int tlclamped = tl & 0x3FF;

            uint firstWord = command.ReadFieldU32(0);
            firstWord |= 0x10 << 19;
            firstWord |= (uint)cmd.Tile << 16;
            firstWord |= (uint)(tlclamped << 2) | 3;

            int yh = (((tlclamped << 2) | 3) << 16) | (tlclamped << 2);
            int xl = sh << 16;
            uint tsize = ((0x80U >> GraphicsState.TextureInputTexelSize) << 16) | ((uint)dxt >> 8);
            dxt = (dxt & 0xFF) << 8;

            var data = new EdgeWalkerLoadData
            {
                commandType = command.Type,
                tileIndex = cmd.Tile,

                yl = Utils.Sign((int)firstWord, 14),
                ym = Utils.Sign(yh >> 16, 14),
                yh = Utils.Sign(yh, 14),

                xl = Utils.Sign(xl, 28),
                xm = Utils.Sign(xl, 28),
                xh = Utils.Sign(sl << 16, 28),

                s = sl << 3 << 16,
                t = tl << 3 << 16,

                dsdx = (int)((tsize & 0xFFFF0000U) | (((uint)dxt >> 16) & 0xFFFF)),
                dtdx = (int)((tsize << 16) & 0xFFFF0000) | (dxt & 0xFFFF),
                dsde = 0,
                dtde = 0x20 << 16,
                dsdy = 0,
                dtdy = 0x20 << 16
            };

            RdpTextureUnit.EdgeWalkerForLoads(data);
        }

        private void LoadTileTlutCommon(RdpCommandType commandType, int tileIndex, uint firstWord, int sl, int tl, int sh, int th) {
            GraphicsState.Tiles[tileIndex].S.CoordLow = sl;
            GraphicsState.Tiles[tileIndex].S.CoordHigh = sh;

            GraphicsState.Tiles[tileIndex].T.CoordLow = tl;
            GraphicsState.Tiles[tileIndex].T.CoordHigh = th;

            GraphicsState.Tiles[tileIndex].ComputeClampDiffs();

            firstWord &= 0xFF000000;
            firstWord |= 0x10 << 19;
            firstWord |= (uint)tileIndex << 16;
            firstWord |= (uint)th | 3;

            int yh = ((th | 3) << 16) | tl;
            int xl = ((sh >> 2) << 16) | ((sh & 3) << 14);

            uint tisize = (0x200U >> GraphicsState.TextureInputTexelSize) << 16;

            var data = new EdgeWalkerLoadData
            {
                commandType = commandType,
                tileIndex = tileIndex,

                yl = Utils.Sign((int)firstWord, 14),
                ym = Utils.Sign(yh >> 16, 14),
                yh = Utils.Sign(yh, 14),

                xl = Utils.Sign(xl, 28),
                xm = Utils.Sign(xl, 28),
                xh = Utils.Sign(((sl >> 2) << 16) | ((sl & 3) << 14), 28),

                s = sl << 3 << 16,
                t = tl << 3 << 16,

                dsdx = (int)(tisize & 0xFFFF0000),
                dtdx = (int)((tisize << 16) & 0xFFFF0000),
                dsde = 0,
                dtde = 0x20 << 16,
                dsdy = 0,
                dtdy = 0x20 << 16
            };

            RdpTextureUnit.EdgeWalkerForLoads(data);
        }

        protected override void LoadTlut(RdpCommand command) {
            var cmd = command.As<LoadTlut>();

            LoadTileTlutCommon(
                command.Type,
                cmd.Tile,
                cmd.ReadFieldU32(0),
                cmd.LowS,
                cmd.LowT,
                cmd.HighS,
                cmd.HighT
            );
        }

        protected override void LoadTile(RdpCommand command) {
            var cmd = command.As<LoadTile>();

            LoadTileTlutCommon(
                command.Type,
                cmd.Tile,
                cmd.ReadFieldU32(0),
                cmd.LowS,
                cmd.LowT,
                cmd.HighS,
                cmd.HighT
            );
        }

        protected override void Triangle(RdpCommand command) {
            Stats_Triangles++;
            
            var cmd = command.As<Triangle>();
            RdpRasterizer.EdgewalkerForPrims(cmd);
        }

        protected override void Rectangle(RdpCommand command) {
            //command.PrintCommandCArray("rect_data");

            MemoryStream dataStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(dataStream);

            var cycleType = (CycleType)GraphicsState.OtherModes.CycleType;

            void Write32(int value) {
                writer.Write(value);
            }

            bool isTex = (command.Type.Flags & RdpCommandFlags.Texture) == RdpCommandFlags.Texture;
            bool isTexFlipped = isTex && (command.Type.Flags & RdpCommandFlags.Flip) == RdpCommandFlags.Flip;

            int offset = 0;

            /* ********************** Edge Coeff ******************** */

            var pos = dataStream.Position;

            int xl;
            int yl;
            int xh;
            int yh;

            if (isTexFlipped) {
                var cmd = command.As<TextureRectangleFlip>();
                xl = cmd.XL;
                yl = cmd.YL;
                xh = cmd.XH;
                yh = cmd.YH;
            }
            else if (isTex) {
                var cmd = command.As<TextureRectangle>();
                xl = cmd.XL;
                yl = cmd.YL;
                xh = cmd.XH;
                yh = cmd.YH;
            }
            else {
                var cmd = command.As<FillRectangle>();
                xl = cmd.BottomRightX;
                yl = cmd.BottomRightY;
                xh = cmd.TopLeftX;
                yh = cmd.TopLeftY;
            }

            if (cycleType == CycleType.Fill || cycleType == CycleType.Copy) {
                    yl |= 3;
            }

            int xlint = (xl >> 2) & 0x3FF;
            int xhint = (xh >> 2) & 0x3FF;

            if (isTexFlipped) {
                var cmd = command.As<TextureRectangleFlip>();
                Write32((0x25 << 24) | ((0x80 | cmd.Tile) << 16) | yl);
            }
            else if (isTex) {
                var cmd = command.As<TextureRectangle>();
                Write32((0x24 << 24) | ((0x80 | cmd.Tile) << 16) | yl);
            }
            else {
                Write32((0x3680 << 16) | yl);
            }

            Write32((yl << 16) | yh);

            Write32((xlint << 16) | ((xl & 3) << 14));
            Write32(0);

            Write32((xhint << 16) | ((xh & 3) << 14));
            Write32(0);

            Write32((xlint << 16) | ((xl & 3) << 14));
            Write32(0);


            /* ********************** Texture Coeff ******************** */

            if (isTex) {
                if (!isTexFlipped) {
                    var cmd = command.As<TextureRectangle>();

                    int dsdx = (short)(ushort)cmd.DsDx;
                    int dtdy = (short)(ushort)cmd.DtDy;

                    Write32((cmd.SCoord << 16) | cmd.TCoord);
                    Write32(0);
                    Write32((dsdx >> 5) << 16);
                    Write32(0);
                    Write32(0);
                    Write32(0);
                    Write32((dsdx & 0x1F) << 11 << 16);
                    Write32(0);
                    Write32((dtdy >> 5) & 0xFFFF);
                    Write32(0);
                    Write32((dtdy >> 5) & 0xFFFF);
                    Write32(0);
                    Write32((dtdy & 0x1F) << 11);
                    Write32(0);
                    Write32((dtdy & 0x1F) << 11);
                    Write32(0);
                }
                else {
                    var cmd = command.As<TextureRectangleFlip>();

                    int dsdx = (short)(ushort)cmd.DsDx;
                    int dtdy = (short)(ushort)cmd.DtDy;

                    Write32((cmd.SCoord << 16) | cmd.TCoord);
                    Write32(0);
                    Write32((dtdy >> 5) & 0xFFFF);
                    Write32(0);
                    Write32(0);
                    Write32(0);
                    Write32((dtdy & 0x1F) << 11);
                    Write32(0);
                    Write32((dsdx >> 5) << 16);
                    Write32(0);
                    Write32((dsdx >> 5) << 16);
                    Write32(0);
                    Write32((dsdx & 0x1F) << 27);
                    Write32(0);
                    Write32((dsdx & 0x1F) << 27);
                    Write32(0);
                }
            }

            /* ******************************************************* */


            byte[] buffer = new byte[dataStream.Length];
            dataStream.Position = 0;

            unsafe {
                fixed (byte * ptr = &buffer[0]) {
                    int len = buffer.Length / 8;

                    for (int i = 0; i < len; i++) {
                        var reader = new BinaryReader(dataStream);
                        ulong * writr_ptr = ((ulong *)ptr) + i;
                        var a = reader.ReadUInt32();
                        var b = reader.ReadUInt32();
                        var v  = ((ulong)a << 32) | b;
                        *writr_ptr = v;
                    }
                }
            }

            var triangle = new Triangle(command.Type, buffer, isTex);

            // Log.Debug("Rect -> Triangle: {0}", triangle.ToString());

            //triangle.PrintCommandCArray("rect_tri_data");

            RdpRasterizer.EdgewalkerForPrims(triangle);
        }

        protected override void SetPrimDepth(RdpCommand command) {
            var cmd = command.As<SetPrimDepth>();
            GraphicsState.PrimitiveZ = (uint)(cmd.Z << 16) & 0x7FFF0000;
            GraphicsState.PrimitiveDeltaZ = (ushort)cmd.DeltaZ;
        }

        protected override void SetScissor(RdpCommand command) {
            var cmd = command.As<SetScissor>();
            GraphicsState.Clip.xh = (ushort)cmd.TopLeftX;
            GraphicsState.Clip.yh = (ushort)cmd.TopLeftY;
            GraphicsState.Clip.xl = (ushort)cmd.BottomRightX;
            GraphicsState.Clip.yl = (ushort)cmd.BottomRightY;
            GraphicsState.ScField = cmd.FieldEnable ? 1 : 0;
            GraphicsState.ScKeepOdd = cmd.KeepOddLines ? 1 : 0;
        }

        protected override void SetOtherModes(RdpCommand command) {
            var cmd = command.As<SetOtherModes>();
            var m = GraphicsState.OtherModes;

            m.CycleType = (int)cmd.PipelineCycleMode;
            m.EnablePerspectiveTextures = cmd.EnableTexturePerspectiveCorrection;
            m.EnableDetailTextures = cmd.EnableDetailTexture;
            m.EnableSharpenTextures = cmd.EnableSharpendTexture;
            m.EnableTextureLevelOfDetail = cmd.EnableTextureLOD;
            m.EnableTextureLookupTable = cmd.EnableTlut;
            m.TextureLookupTableType = cmd.TlutType != 0;
            m.SampleType = cmd.SampleType == Commands.SetOtherModes.TextureSampleType.Sample2x2;
            m.MidTexel = cmd.UseMidTexel;
            m.BiLerp0 = cmd.BiLerp0;
            m.BiLerp1 = cmd.BiLerp1;
            m.ConvertOne = cmd.UseTexelColorConversion;
            m.EnableKey = cmd.EnableChromaKeyring;
            m.RgbDitherSelection = (int)cmd.RgbDitherSelection;
            m.AlphaDitherSelection = (int)cmd.AlphaDitherSelect;
            m.EnableForceBlend = cmd.EnableForceBlend;
            m.AlphaCoverageSelect = cmd.UseConverageForAlpha;
            m.CoverageTimesAlpha = cmd.UseConverageTimesAlpha;
            m.ZMode = (int)cmd.ZMode;
            m.CoverageDestination = (int)cmd.CoverageDestination;
            m.ColorOnCoverage = cmd.UpdateColorOnlyOnCoverage;
            m.EnableImageRead = cmd.EnableImageRead;
            m.EnableZUpdate = cmd.EnableZUpdate;
            m.EnableZCompare = cmd.EnableZCompare;
            m.EnableAntiAliasing = cmd.EnableAntiAliasing;
            m.ZSourceSelection = cmd.UsePixelZ;
            m.EnableDitherAlpha = cmd.EnableDitherAlpha;
            m.EnableAlphaCompare = cmd.EnableAlphaCompare;

            var blenderSelections0 = new Blender.BlendInputSelections(
                cmd.BlendMode1A0, cmd.BlendMode2A0,
                cmd.BlendMode1B0, cmd.BlendMode2B0
            );

            var blenderSelections1 = new Blender.BlendInputSelections(
                cmd.BlendMode1A1, cmd.BlendMode2A1,
                cmd.BlendMode1B1, cmd.BlendMode2B1
            );

            RdpBlender.UpdateInputs(blenderSelections0, blenderSelections1);

            GraphicsState.OtherModes.Flags.IsDerivativesStale = true;
        }
    }
}