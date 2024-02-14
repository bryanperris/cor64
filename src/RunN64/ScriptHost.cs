using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IronPython.Hosting;
using IronPython.Runtime;
using IronPython;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

/*
* Each script to define certain functions
* onload
* onunload
* onframe
* expose scripts to emulator core, imgui workbench
*/

namespace RunN64
{
    public class ScriptHost
    {
        private readonly ScriptEngine m_Engine;
        private readonly List<Script> m_Scripts = new List<Script>();

        private class Script {
            private readonly ScriptHost m_Host;
            private readonly string m_ScriptFilePath;
            private ScriptScope m_Scope;
            private Action m_FuncOnLoad;
            private Action m_FuncOnFrame;
            private Action m_FuncOnUnload;

            public Script(ScriptHost host, String path) {
                m_Host = host;
                m_ScriptFilePath = path;
            }

            public void Load() {
                m_Scope = m_Host.m_Engine.ExecuteFile(m_ScriptFilePath);

                dynamic scope = m_Scope;
                m_FuncOnLoad = scope.onload;
                m_FuncOnFrame = scope.onframe;
               // m_FuncOnUnload = scope.onunload;

                m_FuncOnLoad?.Invoke();
            }

            public void OnFrame() {
                m_FuncOnFrame?.Invoke();
            }

            public void Unload() {
                m_FuncOnUnload?.Invoke();
                m_Scope = null;
                m_FuncOnFrame = null;
                m_FuncOnLoad = null;
            }
        }

        public ScriptHost() {
            m_Engine = Python.CreateEngine();
            m_Engine.Runtime.LoadAssembly(typeof(cor64.N64System).Assembly);
            m_Engine.Runtime.LoadAssembly(typeof(ImGuiNET.ImColor).Assembly);
            m_Engine.Runtime.LoadAssembly(typeof(RunN64.Emulator).Assembly);
        }

        public void LoadScript(string path) {
            var script = new Script(this, path);
            m_Scripts.Add(script);
            script.Load();
        }

        public void ExecuteOnFrame() {
            foreach (var script in m_Scripts) {
                script.OnFrame();
            }
        }

        public void Reload() {
            foreach (var script in m_Scripts) {
                script.Unload();
                script.Load();
            }
        }
    }
}