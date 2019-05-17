using cor64;
using cor64.Mips.R4300I;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using RunN64.Forms;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace RunN64
{
    class Program
    {
        private static Interpreter m_Interpreter = new Interpreter(true, false);
        //private static CFloatInterpreter m_Interpreter = new CFloatInterpreter(true);
        //private static ILRecompiler m_Interpreter = new ILRecompiler(true);

        private static N64System m_System;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void InitLogging()
        {
            LoggingConfiguration configuration = new LoggingConfiguration();

            String layout = @"${date:format=HH\:mm\:ss} ${message}";

            //ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget()
            //{
            //    Layout = layout,

            //};

            ConsoleTarget consoleTarget = new ConsoleTarget()
            {
                Layout = layout
            };

            //ConsoleRowHighlightingRule phaseColorRule = new ConsoleRowHighlightingRule(
            //    ConditionParser.ParseExpression("starts-with('${message}', '--- Phase:')"),
            //    ConsoleOutputColor.Cyan,
            //    ConsoleOutputColor.Black);

            //consoleTarget.RowHighlightingRules.Add(phaseColorRule);

            FileTarget fileTarget = new FileTarget()
            {
                Layout = layout,
                FileName = "run.log",
                AutoFlush = true,
                DeleteOldFileOnStartup = true,
            };

            configuration.AddTarget("console", consoleTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));

            configuration.AddTarget("file", fileTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));

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
            if (File.Exists("emulation.log"))
                File.Delete("emulation.log");

            m_System.DumpExecutionLog("emulation.log");

            Log.Info("Dumped trace log to emulation.log");
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

        static void Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            //m_Interpreter.SetDebuggingMode(true);
            //m_Interpreter.AddVBP_ReadGPR(29);
            //m_Interpreter.SetInstructionDebugMode(DebugInstMode.ProgramOnly);

            InitLogging();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("****************************************");
            Console.WriteLine("* COR64 - A N64 emulator written in C# *");
            Console.WriteLine("****************************************");

            try
            {
                PhaseMsg("Insert Cartridge");

                var cart = GetCartRom(config.RomFilepath);
                m_Interpreter.DebugEntryPoint = cart.EntryPoint;


                Console.ForegroundColor = ConsoleColor.Green;

                PhaseMsg("System Initialization");

                m_System = new N64System();
                m_System.DebugMode();
                m_System.CPU(m_Interpreter);
                m_System.Boot(cart);

                /* Graphics/UI thread */
                Thread gfxThread = new Thread(() =>
                {
                    RawVideoRendererForm rawVidForm = new RawVideoRendererForm(m_System.DeviceMemory.Interface_VI, cart);
                    Application.Run(rawVidForm);
                });

                gfxThread.SetApartmentState(ApartmentState.STA);
                gfxThread.Start();


                PhaseMsg("System Execution Start");

                SingleThreadHost host = new SingleThreadHost(m_System);

                host.Start();

                while (true)
                {
                    //Console.WriteLine("PC Peek: {0:X8}", m_Interpreter.ReadPC());

                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.Q)
                        {
                            host.Interrupt();

                            if (m_Interpreter.StartedWithProfiler)
                            {
                                Console.WriteLine("\n--- Profiler Results ---");
                                Console.Write(m_Interpreter.GetProfiledResults());
                                Console.WriteLine("------");
                            }
                        }
                    }

                    if (host.ThrownException != null)
                    {
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
