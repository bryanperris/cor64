using System.IO;
using NLog;

namespace cor64.Rdp.Renderers {
    public sealed class DummyRdp : DrawProcessor
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public override string Description => "Dummy RDP Renderer";

        protected override void LoadBlock(RdpCommand command)
        {

        }

        protected override void LoadTile(RdpCommand command)
        {

        }

        protected override void LoadTlut(RdpCommand command)
        {

        }

        protected override void Rectangle(RdpCommand command)
        {

        }

        protected override void SetColor(RdpCommand command)
        {
        
        }

        protected override void SetCombineMode(RdpCommand command)
        {
        
        }

        protected override void SetConvert(RdpCommand command)
        {
        
        }

        protected override void SetImage(RdpCommand command)
        {
        
        }

        protected override void SetKey(RdpCommand command)
        {
        
        }

        protected override void SetOtherModes(RdpCommand command)
        {
        
        }

        protected override void SetPrimDepth(RdpCommand command)
        {
        
        }

        protected override void SetScissor(RdpCommand command)
        {
        
        }

        protected override void SetTile(RdpCommand command)
        {
        
        }

        protected override void SetTileSize(RdpCommand command)
        {
        
        }

        protected override void Triangle(RdpCommand command)
        {
        
        }
    }
}