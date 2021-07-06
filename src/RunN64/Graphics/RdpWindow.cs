using System;
using cor64;
using ImGuiNET;
using cor64.Rdp.LLE;

namespace RunN64.Graphics {
    public class RdpWindow : ImGuiWindow
    {
        private readonly MadCatRdp m_Rdp;

        public RdpWindow(N64System system) : base("RDP Rasterizer Engine")
        {
            m_Rdp = (MadCatRdp) system.DeviceRcp.DeviceRdp;
        }

        protected override void BuildComponents()
        {
            ImGui.Text(String.Format("Triangles: {0}", m_Rdp.Stats_Triangles));
        }
    }
}