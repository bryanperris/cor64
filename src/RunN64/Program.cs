using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using cor64;
using cor64.Mips;
using cor64.Mips.Analysis;
using cor64.Mips.R4300I;
using cor64.Mips.R4300I.JitIL;
using cor64.WebService;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using RunN64.Forms;

namespace RunN64
{
    class Program
    {
        //private static Interpreter m_Interpreter = new Interpreter(true, false);
        //private static CFloatInterpreter m_Interpreter = new CFloatInterpreter(true);
        private static ILRecompiler m_Interpreter = new ILRecompiler(true);

        private static N64System m_System;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void InitLogging()
        {
            var logLevel = LogLevel.Info;

//#if DEBUG
//            logLevel = LogLevel.Trace;
//#endif


            LoggingConfiguration configuration = new LoggingConfiguration();

            String layout = @"${message}";

            ConsoleTarget consoleTarget = new ConsoleTarget()
            {
                Layout = layout
            };

            FileTarget fileTarget = new FileTarget()
            {
                Layout = layout,
                FileName = "run.log",
                AutoFlush = true,
                DeleteOldFileOnStartup = true,
            };

            configuration.AddTarget("console", consoleTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

            configuration.AddTarget("file", fileTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, fileTarget));

            LogManager.Configuration = configuration;
            LogManager.Flush();
        }

        private static Cartridge GetCartRom(String path)
        {
            if (!File.Exists(path))
            {
                path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "test.bin";
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException("no rom file to use!");
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                MemoryStream rom = new MemoryStream();
                fs.Position = 0;
                fs.CopyTo(rom);

                var cart = new Cartridge(rom);

                Console.ForegroundColor = ConsoleColor.Magenta;

                Console.WriteLine("Title:         {0}", cart.Name);
                Console.WriteLine("Filename:      {0}", Path.GetFileName(path));
                Console.WriteLine("Entry Point:   0x{0:X8}", cart.EntryPoint);
                Console.WriteLine("Checksum:      0x{0:X8}{1:X8}", cart.Crc1, cart.Crc2);

                Console.Write("Cart IPL3 MD5: 0x");
                foreach (Byte b in cart.BootChecksumMD5)
                {
                    Console.Write("{0:X2}", b);
                }
                Console.WriteLine();

                return cart;
            }
        }

        private static void DumpExecutionLog()
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

        private static void Finish()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n\n****** The program has ended");
        }

        private static void PhaseMsg(String msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n--- {0} ---", msg);
            Console.ForegroundColor = ConsoleColor.Green;
        }

        private static void CicHash(Cartridge cart)
        {

        }

        private static void PrintEmuState()
        {
            Dictionary<string, string> snap = new Dictionary<string, string>();

            m_System.Dbg.DumpState(snap);

            Log.Debug("\n********* Emulator State *********\n");

            StringBuilder sb = new StringBuilder();

            m_System.Dbg.PrintCpuState(sb);

            Console.ForegroundColor = ConsoleColor.White;
            Log.Debug(sb);
            Console.ForegroundColor = ConsoleColor.Green;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Log.Debug("\nStack");
            Log.Debug(m_System.DeviceCPU.State.Stack.DumpStack());
            Console.ForegroundColor = ConsoleColor.Green;

            Log.Debug("**********************************\n");
        }

        static void Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            RawVideoRendererForm rawVidForm = null;

            InitLogging();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("****************************************");
            Console.WriteLine("* COR64 - A N64 emulator written in C# *");
            Console.WriteLine("****************************************");

            try
            {
                PhaseMsg("Insert Cartridge");

                var cart = GetCartRom(config.RomFilepath);
                m_Interpreter.SetProgramEntryPoint(cart.EntryPoint);

                Console.ForegroundColor = ConsoleColor.Green;

                PhaseMsg("System Initialization");

                m_System = new N64System();

                /* Debugging setup */
                m_Interpreter.SetDebuggingMode(true);
                m_Interpreter.SetInstructionDebugMode(DebugInstMode.None);
                m_Interpreter.SetTraceMode(ProgramTrace.TraceMode.None);
                m_Interpreter.TraceLog.Details = ProgramTrace.TraceDetails.None;

                //m_Interpreter.CoreDbg.AppendInstBreakpointByAddr(0x80002CCC);

                if (!String.IsNullOrEmpty(config.ElfFilepath))
                {
                    var elfFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + config.ElfFilepath;

                    if (File.Exists(elfFilePath))
                        m_Interpreter.Disassembler.AttachSymbolProvider(new DebugSymbolSource(elfFilePath));
                }


                EmuWebService webService = new EmuWebService();
                webService.Start();

                m_System.DebugMode();
                m_System.CPU(m_Interpreter);
                m_System.Boot(cart);

                /* Graphics/UI thread */
                Thread gfxThread = new Thread(() =>
                {
                    rawVidForm = new RawVideoRendererForm(m_System.DeviceMemory.Interface_VI, cart);
                    Application.Run(rawVidForm);
                });

                gfxThread.SetApartmentState(ApartmentState.STA);
                gfxThread.Start();

                PhaseMsg("System Execution Start");

                SingleThreadHost host = new SingleThreadHost(m_System);
                host.Break += PrintEmuState;
                host.Start();

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);

                        if (key.Key == ConsoleKey.Q)
                        {
                            host.Interrupt();

                            if (m_Interpreter.StartedWithProfiler)
                            {
                                Console.WriteLine("\n--- Profiler Results ---");
                                Console.Write(m_Interpreter.GetProfiledResults());
                                Console.WriteLine("------");
                            }
                        }

                        if (key.Key == ConsoleKey.B)
                        {
                            if (!m_System.Dbg.IsBreakActive)
                            {
                                m_System.Dbg.Break();
                            }
                        }

                        if (key.Key == ConsoleKey.C)
                        {
                            if (m_System.Dbg.IsBreakActive)
                            {
                                m_System.Dbg.Continue();
                            }
                        }

                        if (key.Key == ConsoleKey.S)
                        {
                            m_System.Dbg.Step();
                        }

                        if (key.Key == ConsoleKey.V)
                        {
                            Log.Info("Forcing video interrupt to fire");
                            rawVidForm?.TriggerVI();
                            m_System.DeviceCPU.State.Cp0.Status.SetInterruptsEnabled(true);
                        }
                    }

                    if (host.ThrownException != null)
                    {
                        if (host.ThrownException.GetType() == typeof(VirtualBreakpointException))
                        {
                            m_System.DeviceCPU.CoreDbg.SkipBreakpoint = true;
                            m_System.Dbg.Break();
                            host.Resume();
                            continue;
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Log.Error("\n--- An emulator error occured! ---");
                        Log.Error("Exception: {0}", host.ThrownException.Message);
                        Log.Error("Stack Trace: {0}", host.ThrownException.StackTrace.ToString());

                        try
                        {
                            DumpExecutionLog();
                        }
                        catch (Exception e)
                        {
                            Log.Error("\n--- A dump stack trace error occured! ---");
                            Log.Error("Exception: {0}", e.Message);
                            Log.Error("Stack Trace: {0}", e.StackTrace.ToString());
                        }


                        Log.Error("Last Instruction: " + m_System.DeviceCPU.Disassembler.GetFullDisassembly(m_System.DeviceCPU.CurrentInst));

                        PrintEmuState();

                        Finish();
                        break;
                    }

                    if (!host.IsRunning)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error("\n--- An application error occured! ---");
                Log.Error("Exception: {0}", e.Message);
                Log.Error("Stack Trace: {0}", e.ToString());
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.ReadLine();
            }

            Application.Exit();
        }
    }
}
