using System.IO;
using NLog;

namespace cor64.Rdp.Renderers {
    public sealed class DummyRdp : DrawProcessor
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public override string Description => "Dummy RDP Renderer";

        private void DumpCommandAsm(RdpCommand command) {
            Log.Info("RDP Command: " + command.ToString());
        }

        protected override void LoadBlock(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void LoadTile(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void LoadTlut(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void Rectangle(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetColor(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetCombineMode(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetConvert(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetImage(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetKey(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetOtherModes(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetPrimDepth(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetScissor(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetSync(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetTile(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void SetTileSize(RdpCommand command)
        {
            DumpCommandAsm(command);
        }

        protected override void Triangle(RdpCommand command)
        {
            DumpCommandAsm(command);
        }
    }
}