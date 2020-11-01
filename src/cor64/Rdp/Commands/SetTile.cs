using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {
    public class SetTile : RdpCommand {
        public SetTile(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int Shift_S => ReadField(0, 0b1111);

        public int Mask_S => ReadField(4, 0b1111);

        public bool EnableMirror_S => ReadFlag(8);

        public bool EnableClamp_S => ReadFlag(9);

        public int Shift_T => ReadField(10, 0b1111);

        public int Mask_T => ReadField(14, 0b1111);

        public bool EnableMirror_T => ReadFlag(18);

        public bool EnableClamp_T => ReadFlag(19);

        public int Pallete => ReadField(20, 0b1111);

        public int TileIndex => ReadField(24, 0b1111);

        public int TMemAddress => ReadField(32, 0b111111111);

        public int Line => ReadField(41, 0b111111111);

        public TexelSize Size => (TexelSize)ReadField(51, 0b11);

        public ImageDataFormat Format => (ImageDataFormat)ReadField(53, 0b111);

        protected override String Params() {
            AsmParams_Start();

            switch (Format) {
                case ImageDataFormat.RGBA: AsmParams_AppendParam("IMAGE_DATA_FORMAT_RGBA"); break;
                case ImageDataFormat.Intensity: AsmParams_AppendParam("IMAGE_DATA_FORMAT_I"); break;
                case ImageDataFormat.IntensityWithAlpha: AsmParams_AppendParam("IMAGE_DATA_FORMAT_IA"); break;
                case ImageDataFormat.ColorIndex: AsmParams_AppendParam("IMAGE_DATA_FORMAT_COLOR_INDX"); break;
            }

            AsmParams_AppendParam(", ");

            switch (Size) {
                case TexelSize.SizeInBytes_4: AsmParams_AppendParam("SIZE_OF_PIXEL_4B"); break;
                case TexelSize.SizeInBytes_8: AsmParams_AppendParam("SIZE_OF_PIXEL_8B"); break;
                case TexelSize.SizeInBytes_16: AsmParams_AppendParam("SIZE_OF_PIXEL_16B"); break;
                case TexelSize.SizeInBytes_32: AsmParams_AppendParam("SIZE_OF_PIXEL_32B"); break;
                case TexelSize.Other: AsmParams_AppendParam("4"); break;
            }

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam(Line.ToString());

            AsmParams_AppendParam(", $");

            AsmParams_AppendParam(TMemAddress.ToString("X3").Substring(0, 3));

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam(TileIndex.ToString());

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam("PALETTE_" + Pallete.ToString("X"));

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam(EnableClamp_T ? "CLAMP_T" : "0");

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam(EnableMirror_T ? "MIRROR_T" : "0");

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam("MASK_T_" + Mask_T.ToString("X"));

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam("SHIFT_T_" + Shift_T.ToString("X"));

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam(EnableClamp_S ? "CLAMP_S" : "0");

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam(EnableMirror_S ? "MIRROR_S" : "0");

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam("MASK_S_" + Mask_S.ToString("X"));

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam("SHIFT_S_" + Shift_S.ToString("X"));

            return AsmParams_End();
        }
    }
}