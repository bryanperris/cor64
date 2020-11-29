using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using cor64;
using cor64.Mips;
using cor64.Mips.Analysis;
using cor64.Mips.R4300I;
using cor64.Mips.R4300I.JitIL;
using cor64.Rdp.LLE;
using NLog;
using NLog.Config;
using NLog.Targets;
using RunN64.Graphics;
using cor64.BareMetal;

namespace RunN64
{
    public class Emulator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private N64System m_System = new N64System();
        private InterpreterBaseR4300I m_CpuEngine;
        public Config Configuration { get; set; } = new Config();
        private Cartridge m_Cartridge;
        private SingleThreadHost m_EmuHost;
        private GLFramebufferWindow m_FramebufferWindow;

        public void SetupLogging()
        {
            //var logLevel = LogLevel.Info;

            LoggingConfiguration configuration = new LoggingConfiguration();

            String layout = @"${message}";

            #if TIMESTAMPS_IN_LOG
            layout = "${date:format=HH\:MM\:ss} " + layout;
            #endif

            ConsoleTarget consoleTarget = new ConsoleTarget()
            {
                Layout = layout
            };

            configuration.AddTarget("console", consoleTarget);


            FileTarget fileTarget = new FileTarget()
            {
                Layout = layout,
                FileName = "run.log",
                AutoFlush = true,
                DeleteOldFileOnStartup = true,
            };

            configuration.AddTarget("file", fileTarget);


            /* Must configure rules here so that all targets get activated */
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));

            LogManager.Configuration = configuration;
        }

        private Cartridge MountCartridge(String path)
        {

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                MemoryStream rom = new MemoryStream();
                fs.Position = 0;
                fs.CopyTo(rom);

                // N64BareMetalAssembler assembler = new N64BareMetalAssembler("foorom");
                // assembler.AddAssemblySource(new AssemblyStreamSource("test", fs));
                // assembler.AssembleCode(true);
                // Stream rom = assembler.Output;
                //rom.Position = 0;

                var cart = new Cartridge(rom);

                Log.Info("Title:         {0}", cart.Name);
                Log.Info("Filename:      {0}", Path.GetFileName(path));
                Log.Info("Entry Point:   0x{0:X8}", cart.EntryPoint);
                Log.Info("Checksum:      0x{0:X8}{1:X8}", cart.Crc1, cart.Crc2);

                var md5 = "Cart IPL3 MD5: 0x";

                foreach (Byte b in cart.BootChecksumMD5)
                {
                    md5 += b.ToString("X2");
                }

                Log.Info(md5);

                return cart;
            }
        }

        public void Start()
        {
            if (!File.Exists(Configuration.RomFilepath))
            {
                throw new EmuException("Invalid cartridge rom path was provided");
            }

            PhaseMsg("Insert Cartridge");

            m_Cartridge = MountCartridge(Configuration.RomFilepath);

            PhaseMsg("System Initialization");

            if (Configuration.UseInterpreter) {
                m_CpuEngine = new Interpreter();
            }
            else {
                m_CpuEngine = new ILRecompiler();
            }

            if (!String.IsNullOrEmpty(Configuration.ElfFilepath))
            {
                var elfFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + Configuration.ElfFilepath;

                if (File.Exists(elfFilePath)) {
                    m_CpuEngine.Disassembler.AttachSymbolProvider(new DebugSymbolSource(elfFilePath));

                    Log.Info("Found provided ELF debug symbols");
                }
            }

            m_CpuEngine.SetDebuggingMode(true);
            m_CpuEngine.SetInstructionDebugMode(InstructionDebugMode.None);
            // m_CpuEngine.SetTraceMode(ProgramTrace.TraceMode.ProgramOnly);
            // m_CpuEngine.TraceLog.Details = ProgramTrace.TraceDetails.None;
            // m_CpuEngine.TraceLog.EnableLogVerfication();

            //m_CpuEngine.CoreDbg.AppendInstBreakpointByAddr(0x800F71BC);

            m_System.CPU(m_CpuEngine);

            m_System.DeviceRcp.SetRdpDevice(new MadCatRdp());
            m_System.DeviceRcp.DeviceRdp.SetDLDebug(true);

            Log.Info("Signal Processor Engine: {0}", m_System.DeviceRcp.DeviceRsp.Description);
            Log.Info("Rasterizer Engine: {0}", m_System.DeviceRcp.DeviceRdp.Description);

            m_System.Boot(m_Cartridge);

            CreateFramebufferWindow();

            PhaseMsg("System Execution Start");

            m_EmuHost = new SingleThreadHost(m_System);
            m_EmuHost.Break += OnBreak;
            m_EmuHost.Start();
        }

        private void CreateFramebufferWindow()
        {
            Thread fbThread = new Thread(() =>
            {
                m_FramebufferWindow = new GLFramebufferWindow(m_System);
                m_FramebufferWindow.Start();
            })
            {
                Name = "Framebuffer Thread"
            };

            fbThread.Start();

            while (true) {
                if (m_FramebufferWindow != null) {
                    if (m_FramebufferWindow.IsCreated) {
                        break;
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void OnBreak() {
            DumpStateToLog();
        }

        public void DumpExecutionLog()
        {
            Log.Info("Dumping trace log... Please wait...");

            if (File.Exists("emulation.log"))
                File.Delete("emulation.log");

            using (var fs = new FileStream("emulation.log", FileMode.CreateNew, FileAccess.ReadWrite))
            {
                m_System.DumpExecutionLog(fs);
            }

            Log.Info("Done! trace log = emulation.log");
        }

        private void PhaseMsg(String msg)
        {
            Log.Info("\n--- {0} ---", msg);
        }

        public void DumpStateToLog()
        {
            Log.Debug("\n********* Emulator State *********\n");

            StringBuilder sb = new StringBuilder();

            m_System.Dbg.PrintCpuState(sb);

            Log.Debug(sb);

            // Log.Debug("\nStack");
            // Log.Debug(m_System.DeviceCPU.State.Stack.DumpStack());

            Log.Debug("**********************************\n");
        }

        public void ForceVideoInterrupt() {
            if (m_FramebufferWindow != null) {
                m_FramebufferWindow.TriggerVI();
                m_System.DeviceCPU.State.Cp0.Status.SetInterruptsEnabled(true);
            }
        }

        public void ForceInterruptsEnable() {
            m_System.DeviceCPU.State.Cp0.Status.SetInterruptsEnabled(true);
        }

        public SingleThreadHost Host => m_EmuHost;

        public InterpreterBaseR4300I CPU => m_CpuEngine;

        public N64System System => m_System;
    }
}