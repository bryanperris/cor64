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
using cor64.Rdp.LLE;
using cor64.HLE;
using cor64.HLE.OS;
using cor64.PIF;

namespace cor64
{
    public class N64System
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly BootManager m_BootManager;
        private Cartridge m_Cartridge;

        private readonly JoyController[] m_JoyControllers = new JoyController[4];

        public N64System()
        {
            SystemMemory = new N64MemoryController();
            //DeviceMemory.UseSafeAccess();

            /* Boot Management */
            m_BootManager = new BootManager();
            m_BootManager.MMIOWrite += MMIORegWrite;

            Dbg = new EmuDebugger(this);

            m_JoyControllers[0] = new JoyController(0);
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
            SystemMemory.MountCartridge(cartridge);
            DeviceCPU.SetProgramEntryPoint(cartridge.EntryPoint);

            /* Attach RCP to memory */
            DeviceRcp.AttachToMemory(SystemMemory);

            /* Initlaize core memory */
            SystemMemory.Init();

            /* Cartridge verification */
            // ((MemMappedCart) SystemMemory.Cart).SelfTest(cartridge.RomStream);

            /* Init the RDP */
            if (DeviceRcp.GraphicsHLEDevice == null)
                 DeviceRcp.DeviceRdp.Init();

            /* Init the RSP */
            DeviceRcp.DeviceRsp.Init();

            /* Init the serial controller */
            DeviceRcp.SerialDevice.Init();

            /* Attach memory to CPU */
            DeviceCPU.AttachMemory(SystemMemory);

            /* Attach RCP to CPU */
            DeviceCPU.AttachRcp(DeviceRcp);

            /* Hook up joypads */
            DeviceRcp.SerialDevice.ReadJoycons += UpdateJoyconPif;

            /* Attach DMA engines */
            SystemMemory.PIRegs.AttachDma();
            SystemMemory.SPRegs.AttachDma();
            SystemMemory.SIRegs.AttachDma();

            /* Init the CPU */
            DeviceCPU.Init();

            /* Perform system boot intialization (Motherboard IPl) */
            m_BootManager.BootCartridge(cartridge, true);

            return this;
        }

        public N64System BootForTesting() {
            /* Attach RCP to memory */
            DeviceRcp.AttachToMemory(SystemMemory);

            if (DeviceCPU != null) {
                /* Attach memory to CPU */
                DeviceCPU.AttachMemory(SystemMemory);

                /* Attach RCP to CPU */
                DeviceCPU.AttachRcp(DeviceRcp);
            }
            /* Initialize system memory */
            SystemMemory.Init();

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

            VI.Tick();
        }

        public Video VI => DeviceRcp.VideoInterface;

        public PIFController PIF => SystemMemory.PIF;

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
                if (DeviceCPU.TraceLog.HasContent)
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

        private void UpdateJoyconPif() {
             if (m_JoyControllers[0].CheckPending()) {
                PIF.JoyWriteButtons(0, m_JoyControllers[0].ReadPending());
            }
        }

        public EmuDebugger Dbg { get; }

        public InterpreterBaseR4300I DeviceCPU { get; private set; }

        public N64MemoryController SystemMemory { get; }

        public RcpCore DeviceRcp { get; } = new RcpCore();

        public Cartridge AttachedCartridge => m_Cartridge;

        public JoyController[] Joys => m_JoyControllers;
    }
}
