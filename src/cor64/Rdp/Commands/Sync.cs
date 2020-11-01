using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {

    /// <summary>
    /// This command stalls the RDP until the last dram buffer is read or written from any preceeding
    /// primitive. It is typically only needed if the memory data is to be reused, like switching display buffers, or writing a
    /// color_image to be used as a texture_image, or for consistent r/w access to an RDP w/r image from the cpu.
    /// </summary>
    public class Sync : RdpCommand {
        
        public Sync(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        protected override String Params() {
            return "";
        }
    }
}