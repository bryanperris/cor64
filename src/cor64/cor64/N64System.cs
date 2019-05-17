using cor64.IO;
using cor64.Mips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.IO;
using cor64.Mips.R4300I;

namespace cor64
{
    public class N64System
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private N64MemoryController m_Memory;
        private BaseInterpreter m_CPU;
        private BootManager m_BootManager;
        private Cartridge m_Cartridge;

        public N64System()
        {
            m_Memory = new N64MemoryController();

            /* Boot Management */
            m_BootManager = new BootManager();
            m_BootManager.MMIOWrite += MMIORegWrite;
        }

        private void MMIORegWrite(BootManager.MMIORegWriteKind kind, uint v)
        {
            switch (kind)
            {
                case BootManager.MMIORegWriteKind.MiVersion: m_Memory.Interface_MI.SetVersion(v); break;
                case BootManager.MMIORegWriteKind.SpStatus: m_Memory.Interface_SP.SetStatus(v); break;
                default: break;
            }
        }

        public N64System CPU(BaseInterpreter interpreter)
        {
            Log.Info("MIPS R4300I CPU Engine: {0}", interpreter.Description);

            m_CPU = interpreter;

            interpreter.AttachIStream(m_Memory.CreateMemoryStream());
            interpreter.AttachDStream(m_Memory.CreateMemoryStream());
            interpreter.HookInterface(m_Memory);
            interpreter.AttachBootManager(m_BootManager);
            return this;
        }

        public N64System Boot(Cartridge cartridge)
        {
            m_Cartridge = cartridge;
            m_Memory.MountCartridge(cartridge);
            m_Memory.Init();
            m_BootManager.BootCartridge(cartridge, true);
            m_CPU.SetProgramEntryPoint(cartridge.EntryPoint);
            return this;
        }

        public void StepOnce()
        {
            m_CPU.Step();
        }

        public void DebugMode()
        {
            m_Memory.DebugMode();
        }

        public void DumpExecutionLog(String path)
        {
            StringBuilder sb = new StringBuilder();

            if (m_CPU != null)
            {
                if (m_CPU.TraceLog.Size > 0)
                {
                    sb.AppendLine("Trace Log Dump:");
                    sb.AppendLine(m_CPU.TraceLog.GenerateTraceLog());
                }

                m_CPU.DumpLogInfo(sb);
            }
            else
            {
                sb.AppendLine("No CPU was attached!");
            }

            File.WriteAllText(path, sb.ToString());
        }

        public BaseInterpreter DeviceCPU => m_CPU;

        public N64MemoryController DeviceMemory => m_Memory;
    }
}
