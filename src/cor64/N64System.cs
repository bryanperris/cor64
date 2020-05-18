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
using cor64.RCP;

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
            //DeviceMemory.UseSafeAccess();

            /* Boot Management */
            m_BootManager = new BootManager();
            m_BootManager.MMIOWrite += MMIORegWrite;

            Dbg = new Debugger(this);
        }

        private void MMIORegWrite(BootManager.MMIORegWriteKind kind, uint v)
        {
            switch (kind)
            {
                case BootManager.MMIORegWriteKind.MiVersion: DeviceRcp.RcpInterface.SetVersion(v); break;
                case BootManager.MMIORegWriteKind.SpStatus: DeviceRcp.RspInterface.SetStatus(v); break;
                default: break;
            }
        }

        public N64System CPU(InterpreterBaseR4300I interpreter)
        {
            Log.Info("CPU Engine: {0}", interpreter.Description);

            DeviceCPU = interpreter;

            interpreter.AttachBootManager(m_BootManager);
            return this;
        }

        public N64System Boot(Cartridge cartridge)
        {
            /* Attach cartridge to system */
            m_Cartridge = cartridge;
            DeviceMemory.MountCartridge(cartridge);
            DeviceCPU.SetProgramEntryPoint(cartridge.EntryPoint);

            /* Attach RCP to memory */
            DeviceRcp.AttachToMemory(DeviceMemory);

            /* Initialize system memory */
            DeviceMemory.Init();

            /* Attach memory to CPU */
            DeviceCPU.AttachIStream(DeviceMemory.CreateMemoryStream());
            DeviceCPU.AttachDStream(DeviceMemory.CreateMemoryStream());

            /* Attach RCP to CPU */
            DeviceCPU.AttachRcp(DeviceRcp);

            /* Perform system boot intialization (Motherboard IPl) */
            m_BootManager.BootCartridge(cartridge, true);

            /* Start the RCP Core */
            DeviceRcp.Start();

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

        public void DumpExecutionLog(Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);

            if (DeviceCPU != null)
            {
                if (DeviceCPU.TraceLog.Size > 0)
                {
                    writer.WriteLine("Trace Log Dump:");

                    var tracelog = DeviceCPU.TraceLog.GenerateTraceLog();

                    for (int i = 0; i < tracelog.Count; i++)
                        writer.WriteLine(tracelog[i]);

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

        public InterpreterBaseR4300I DeviceCPU { get; private set; }

        public N64MemoryController DeviceMemory { get; }

        public RcpCore DeviceRcp { get; } = new RcpCore();
    }
}
