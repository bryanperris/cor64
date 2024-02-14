using System;
using cor64;
using cor64.Mips.Rsp;
using cor64.Debugging;
using ImGuiNET;

namespace RunN64.Graphics {
    public sealed class DmaWindow : ImGuiWindow
    {
        private readonly N64System m_System;
        private int m_CurrentLine;
        private readonly string[] m_Log = new string[100];
        private int m_LogCount;

        public DmaWindow(N64System system) : base("DMA Log")
        {
            m_System = system;
            InitLog();
            EmuDebugger.Current.DmaOperation += OnDmaOp;
        }

        private void InitLog() {
            for (int i = 0; i < m_Log.Length; i++) {
                m_Log[i] = "";
            }

            m_LogCount = 0;
        }

        private void OnDmaOp(EmuDebugger.DmaEvent e) {
            if (m_LogCount >= m_Log.Length) {
                ScrollBack();
            }

            m_Log[m_LogCount++] = e.HwType.ToString();

            // m_CurrentLine = m_LogCount - 1;
        }

        protected override void BuildComponents()
        {
            // TODO: This is crashing
            // ImGui.ListBox("dmalog", ref m_CurrentLine, m_Log, m_Log.Length);
        }

        private void ScrollBack() {
            for (int i = 0; i < m_Log.Length; i++) {
                if ((i + 1) < m_Log.Length)
                    m_Log[i] = m_Log[i + 1];
                else
                    m_Log[i] = "";
            }

            m_LogCount = m_Log.Length - 1;
        }
    }
}