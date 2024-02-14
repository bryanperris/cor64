using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RunN64.Graphics;
using ImGuiNET;

namespace RunN64.Workbench
{
    public class ScriptsWindow : ImGuiWindow
    {
        private readonly ScriptHost m_ScriptHost;

        public ScriptsWindow(ScriptHost scriptHost) : base("Scripts")
        {
            m_ScriptHost = scriptHost;
        }

        protected override void BuildComponents()
        {
            if (ImGui.Button("Reload")) {
                m_ScriptHost.Reload();
            }
        }
    }
}