using System.Text;
using System;
using cor64.Mips.Rsp;
using ImGuiNET;
using System.Numerics;
using cor64.Mips;
using cor64;

namespace RunN64.Graphics {
    public sealed class RspWindow : ImGuiWindow {
        private readonly RspInterpreter m_Rsp;
        private readonly StringBuilder m_StrBuilder = new StringBuilder();

        private readonly static int[][] s_GprGrid = {
            new int[] { 0, 4, 8,  12, 16, 20, 23, 27, 31 },
            new int[] { 1, 5, 9,  13, 17, 21, 24, 28, /*pc*/ },
            new int[] { 2, 6, 10, 14, 18, 22, 25, 29, /*hi*/ },
            new int[] { 3, 7, 11, 15, 19, 22, 26, 30, /*lo*/ }
        };

        private bool m_Signal0 = false;
        private bool m_Signal1 = false;
        private bool m_Signal2 = false;
        private bool m_Signal3 = false;
        private bool m_Signal4 = false;
        private bool m_Signal5 = false;
        private bool m_Signal6 = false;
        private bool m_Signal7 = false;

        private readonly Vector4 COLOR_ACC = new(1.0f, 1.0f, 1.0f, 1f);
        private readonly Vector4 COLOR_VEC_EVEN = new(1.0f, 1.0f, 1.0f, 1f);
        private readonly Vector4 COLOR_VEC_ODD = new(0.835f, 0.521f, 0.741f, 1.0f);

        private readonly Vector4 ColorVecEven = new Vector4(0.101f, 0.878f, 0.454f, 1.0f);
        private readonly Vector4 ColorVecOdd = new Vector4(0.0f, 0.7f, 0.0f, 1.0f);
        private readonly Vector4 ColorVecBg = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        public RspWindow(N64System system) : base("Signal Processor") {
            m_Rsp = (RspInterpreter)system.DeviceRcp.DeviceRsp;
        }

        private void BuildMipsUI() {
            ImGui.Columns(2, "MIPS");
            ImGui.SetColumnWidth(0, 100.0f);
            GprText(s_GprGrid[0]);
            GprText(s_GprGrid[1]);
            ImGui.NextColumn();
            GprText(s_GprGrid[2]);
            GprText(s_GprGrid[3]);
            ImGui.Columns();

            ImGui.Separator();

            ImGui.Text(string.Format("pc: {0:X8}", (uint)m_Rsp.ReadPC()));
            ImGui.Text(string.Format("hi: {0:X8}", (uint)m_Rsp.State.GetHi()));
            ImGui.Text(string.Format("lo: {0:X8}", (uint)m_Rsp.State.GetLo()));

            // ImGui.Separator();

            // m_Disasm.PC = m_Rsp.ReadPC();
            // m_Disasm.Build();
        }

        private void BuildVecUI() {
            var drawList = ImGui.GetWindowDrawList();

            // ImGui.Columns(2, "RSP");

            m_StrBuilder.Clear();
            m_StrBuilder.Append("ACC H ");
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(0));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(1));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(2));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(3));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(4));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(5));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Hi(6));
            m_StrBuilder.AppendFormat("{0:X4}",  m_Rsp.Acc.Hi(7));
            ImGui.TextColored(COLOR_ACC,  m_StrBuilder.ToString());

            ImGui.SameLine();
            ImGui.Text(String.Format("VCO: {0:X4}", m_Rsp.ReadVCO()));

            m_StrBuilder.Clear();
            m_StrBuilder.Append("ACC M ");
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(0));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(1));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(2));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(3));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(4));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(5));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Mi(6));
            m_StrBuilder.AppendFormat("{0:X4}",  m_Rsp.Acc.Mi(7));
            ImGui.TextColored(COLOR_ACC,  m_StrBuilder.ToString());

            ImGui.SameLine();
            ImGui.Text(String.Format("VCC: {0:X4}", m_Rsp.ReadVCC()));

            m_StrBuilder.Clear();
            m_StrBuilder.Append("ACC L ");
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(0));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(1));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(2));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(3));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(4));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(5));
            m_StrBuilder.AppendFormat("{0:X4} ", m_Rsp.Acc.Lo(6));
            m_StrBuilder.AppendFormat("{0:X4}",  m_Rsp.Acc.Lo(7));
            ImGui.TextColored(COLOR_ACC,  m_StrBuilder.ToString());

            ImGui.SameLine();
            ImGui.Text(String.Format("VCE: {0:X4}", m_Rsp.ReadVCE()));

            ImGui.Separator();

            for (int i = 0; i < 32; i++) {
                m_StrBuilder.Clear();

                // var textColor = (i & 1) == 0 ? ColorVecEven : ColorVecOdd;
                var textColor = ColorVecEven;

                // drawList.DrawTextWithBackground(textColor, ColorVecBg, String.Format("V{0:D2}", i));
                ImGui.Text(String.Format("V{0:D2}", i));

                for (int j = 0; j < 8; j++) {
                    ImGui.SameLine();

                    if (j == 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 13);
                    // else
                    //      ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 1);

                    drawList.DrawTextWithBackground(textColor, ColorVecBg, m_Rsp.m_VecRegs[i].PackedU16(j).ToString("X4"));
                }
            }
        }

        private void BuildIFaceUI() {
            var iface = m_Rsp.Interface;
            var col = new Vector4(1, 1, 0, 1);

            m_Signal0 = iface.ReadSignal(0);
            m_Signal1 = iface.ReadSignal(1);
            m_Signal2 = iface.ReadSignal(2);
            m_Signal3 = iface.ReadSignal(3);
            m_Signal4 = iface.ReadSignal(4);
            m_Signal5 = iface.ReadSignal(5);
            m_Signal6 = iface.ReadSignal(6);
            m_Signal7 = iface.ReadSignal(7);

            ImGui.Text(String.Format("PC:               {0:X8}", iface.PC));
            ImGui.SameLine(); ImGui.Checkbox("SIG 0", ref m_Signal0);
            ImGui.SameLine(); ImGui.Checkbox("SIG 1", ref m_Signal1);

            ImGui.Text(String.Format("DMA SP Address:   {0:X8}", iface.SPMemAddr));
            ImGui.SameLine(); ImGui.Checkbox("SIG 2", ref m_Signal2);
            ImGui.SameLine(); ImGui.Checkbox("SIG 3", ref m_Signal3);

            ImGui.Text(String.Format("DMA DRAM Address: {0:X8}", iface.DramMemAddr));
            ImGui.SameLine(); ImGui.Checkbox("SIG 4", ref m_Signal4);
            ImGui.SameLine(); ImGui.Checkbox("SIG 5", ref m_Signal5);

            ImGui.Text(String.Format("DMA Read Length:  {0:X8}", iface.ReadLen));
            ImGui.SameLine(); ImGui.Checkbox("SIG 6", ref m_Signal6);
            ImGui.SameLine(); ImGui.Checkbox("SIG 7", ref m_Signal7);

            ImGui.Text(String.Format("DMA Write Length: {0:X8}", iface.WriteLen));
        }

        protected override void BuildComponents() {
            if (ImGui.BeginTabBar("RspTabs")) {

                if (ImGui.BeginTabItem("MIPS")) {
                    BuildMipsUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Vector Unit")) {
                    BuildVecUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Interface")) {
                     BuildIFaceUI();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Separator();
        }

        private void GprText(int i) {
            if (i == 0)
                ImGui.Text(string.Format("r0: 00000000"));
            else
                ImGui.Text(string.Format("{0}: {1:X8}", ABI.GetLabel("o32", ABI.RegType.GPR, i), m_Rsp.State.GetGpr32(i)));
        }

        private void GprText(int[] arr) {
            for (int i = 0; i < arr.Length; i++) {
                GprText(arr[i]);
            }
        }
    }
}