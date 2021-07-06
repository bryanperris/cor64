using System;
using cor64;
using cor64.Mips.Rsp;
using ImGuiNET;

namespace RunN64.Graphics {
    public sealed class DebugWindow : ImGuiWindow
    {
        private readonly N64System m_System;
        private readonly RspInterpreter m_Rsp;


        private int m_RspBreakpointAddress;
        private bool m_RspBreakpointAddressApply;

        public DebugWindow(N64System system) : base("Debugger")
        {
            m_System = system;
            m_Rsp = (RspInterpreter) system.DeviceRcp.DeviceRsp;
        }

        protected override void BuildComponents()
        {
            ImGui.SameLine();
            if (ImGui.Button("Break")) {
                m_System.Dbg.Break();
            }

            ImGui.SameLine();
            if (ImGui.Button("Step")) {
                m_System.Dbg.Step();
            }

            ImGui.SameLine();
            if (ImGui.Button("RSP Step")) {
                m_System.Dbg.StepRsp();
            }

            ImGui.SameLine();
            if (ImGui.Button("Continue")) {
                m_System.Dbg.Continue();
            }

            ImGui.Separator();

            int rspBreakAddr = 0;

            ImGui.InputInt("Rsp Breakpoint Address", ref m_RspBreakpointAddress, 4, 8, ImGuiInputTextFlags.CharsHexadecimal);

            if (ImGui.Checkbox("Active", ref m_RspBreakpointAddressApply)) {
                if (!m_RspBreakpointAddressApply)
                {
                    m_Rsp.Debugger.ClearBreakpoints();

                }
                else
                {
                    m_Rsp.Debugger.AppendInstBreakpointByAddr((uint)rspBreakAddr);
                }
            }
        }
    }
}