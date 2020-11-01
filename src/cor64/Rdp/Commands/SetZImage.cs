using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {
    public class SetZImage : RdpCommand {
        public SetZImage(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        /// <summary>
        /// Base address (top left corner) of image in DRAM, in bytes
        /// </summary>
        /// <returns></returns>
        public int DramAddress => ReadField(0, 0x3FFFFFF);

        protected override String Params() {
            AsmParams_Start();

            AsmParams_AppendParam("$");

            AsmParams_AppendParam(DramAddress.ToString("X8"));

            return AsmParams_End();
        }
    }
}