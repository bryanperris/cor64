using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {
    public class SetColorImage : RdpCommand {
        public SetColorImage(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        /// <summary>
        /// Base address (top left corner) of image in DRAM, in bytes
        /// </summary>
        /// <returns></returns>
        public int DramAddress => ReadField(0, 0x3FFFFFF);

        /// <summary>
        /// Width of image in pixels, image width = width + 1
        /// </summary>
        /// <returns></returns>
        public int Width => ReadField(32, 0xFFF);

        /// <summary>
        ///  Size of pixel/texel color element
        /// </summary>
        /// <returns></returns>
        public ImageSizeType Size => (ImageSizeType)ReadField(51, 0b11);
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
                case ImageSizeType.SizeInBytes_4: AsmParams_AppendParam("SIZE_OF_PIXEL_4B"); break;
                case ImageSizeType.SizeInBytes_8: AsmParams_AppendParam("SIZE_OF_PIXEL_8B"); break;
                case ImageSizeType.SizeInBytes_16: AsmParams_AppendParam("SIZE_OF_PIXEL_16B"); break;
                case ImageSizeType.SizeInBytes_32: AsmParams_AppendParam("SIZE_OF_PIXEL_32B"); break;
            }

            AsmParams_AppendParam(", ");

            AsmParams_AppendParam((Width).ToString());

            AsmParams_AppendParam(", $");

            AsmParams_AppendParam(DramAddress.ToString("X8"));

            return AsmParams_End();
        }
    }
}