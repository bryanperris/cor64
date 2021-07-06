using System.Numerics;
using System;
using ImGuiNET;

namespace RunN64.Graphics {
    public abstract class ImGuiWindow : ImGuiWidget {
        private readonly string m_WindowTitle;
        private ImGuiWindowFlags m_Flags = ImGuiWindowFlags.None;
        private bool m_IsOpen;

        private readonly Vector4 BLACK_COLOR = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        private readonly Vector4 BG_COLOR = new Vector4(0.129f, 0.094f, 0.117f, 1.0f);
        private readonly Vector4 FG_COLOR = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
        private readonly Vector4 TEXT_COLOR = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Vector4 SEP_COLOR = new Vector4(0.380f, 0f, 0.266f, 1.0f);
        private readonly Vector4 BUTTON_COLOR = new Vector4(0.380f, 0f, 0.266f, 1.0f);
        private readonly Vector4 BUTTON_HOVER_COLOR = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        private readonly Vector4 CHECK_COLOR = new Vector4(0.780f, 0.101f, 0.596f, 1.0f);

        protected ImGuiWindow(string title) {
            m_WindowTitle = title;
        }

        protected ImGuiWindow(string title, ImGuiWindowFlags flags) {
            m_WindowTitle = title;
            m_Flags = flags;
        }

        protected void SetWindowFlags(ImGuiWindowFlags flags) {
            m_Flags = flags;
        }

        public override void Build() {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, BG_COLOR);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, FG_COLOR);

            ImGui.Begin(m_WindowTitle, ref m_IsOpen, m_Flags);

            if (!ImGui.IsWindowCollapsed()) {
                ImGui.PushStyleColor(ImGuiCol.Text, TEXT_COLOR);
                ImGui.PushStyleColor(ImGuiCol.Separator, SEP_COLOR);
                ImGui.PushStyleColor(ImGuiCol.Button, BUTTON_COLOR);
                ImGui.PushStyleColor(ImGuiCol.Border, BLACK_COLOR);
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, TEXT_COLOR);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BUTTON_HOVER_COLOR);
                ImGui.PushStyleColor(ImGuiCol.CheckMark, CHECK_COLOR);
                ImGui.PushStyleColor(ImGuiCol.ResizeGrip, BUTTON_COLOR);
                ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, BUTTON_HOVER_COLOR);
                ImGui.PushStyleColor(ImGuiCol.Tab, BUTTON_COLOR);
                ImGui.PushStyleColor(ImGuiCol.TabActive, BUTTON_HOVER_COLOR);

                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);

                BuildComponents();
            }

            ImGui.PopStyleColor();

            ImGui.End();
        }

        protected abstract void BuildComponents();
    }
}