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
using cor64.Debugging;

namespace cor64
{
    public class N64System
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private BootManager m_BootManager;
        private Cartridge m_Cartridge;

        public N64System()
        {
            DeviceMemory = new N64MemoryController();

            /* Boot Management */
            m_BootManager = new BootManager();
            m_BootManager.MMIOWrite += MMIORegWrite;

            Dbg = new Debugger(this);
        }

        private void MMIORegWrite(BootManager.MMIORegWriteKind kind, uint v)
        {
            switch (kind)
            {
                case BootManager.MMIORegWriteKind.MiVersion: DeviceMemory.Interface_MI.SetVersion(v); break;
                case BootManager.MMIORegWriteKind.SpStatus: DeviceMemory.Interface_SP.SetStatus(v); break;
                default: break;
            }
        }

        public N64System CPU(CoreR4300I interpreter)
        {
            Log.Info("MIPS R4300I CPU Engine: {0}", interpreter.Description);

            DeviceCPU = interpreter;

            interpreter.AttachIStream(DeviceMemory.CreateMemoryStream());
            interpreter.AttachDStream(DeviceMemory.CreateMemoryStream());
            interpreter.HookInterface(DeviceMemory);
            interpreter.AttachBootManager(m_BootManager);
            return this;
        }

        public N64System Boot(Cartridge cartridge)
        {
            m_Cartridge = cartridge;
            DeviceMemory.MountCartridge(cartridge);
            DeviceMemory.Init();
            m_BootManager.BootCartridge(cartridge, true);
            DeviceCPU.SetProgramEntryPoint(cartridge.EntryPoint);
            return this;
        }

        /// <summary>
        /// Step all system events on a single system tick (Single core mode)
        /// </summary>
        public void Tick()
        {
            Dbg.EnterExecution();
            DeviceCPU.Step();
            Dbg.LeaveExecution();
            DeviceCPU.CoreDbg.SkipBreakpoint = false;
        }

        /// <summary>
        /// If an exception is thrown during a tick, call this to cleanup anything aftwards
        /// </summary>
        public void TickFinally()
        {
            Dbg.LeaveExecution();
        }

        // For supporting multiple threads, we need an event system to schedule ticks in parallel

        public void DebugMode()
        {
            DeviceMemory.DebugMode();
        }

        public void DumpExecutionLog(Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);

            if (DeviceCPU != null)
            {
                if (DeviceCPU.TraceLog.Size > 0)
                {
                    writer.WriteLine("Trace Log Dump:");
                    writer.WriteLine(DeviceCPU.TraceLog.GenerateTraceLog());
                    writer.WriteLine("\n\n");
                    writer.Flush();
                }
            }
            else
            {
                writer.WriteLine("No CPU was attached!");
            }
        }

        public Debugger Dbg { get; }

        public CoreR4300I DeviceCPU { get; private set; }

        public N64MemoryController DeviceMemory { get; }
    }
}
