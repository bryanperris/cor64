using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using cor64;
using cor64.Mips;
using cor64.Mips.R4300I;
using NLog;
using NLog.Config;
using NLog.Targets;
using RunN64.Forms;
using Newtonsoft.Json;
using NLog.Conditions;

namespace RunN64
{
    class Program
    {
        private static ILRecompiler m_Interpreter = new ILRecompiler(true);
        private static N64System m_System;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void InitLogging()
        {
            LoggingConfiguration configuration = new LoggingConfiguration();

            String layout = @"${date:format=HH\:mm\:ss} ${message}";

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget()
            {
                Layout = layout,
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

        private static Stream GetCartRom(String path)
        {
            if (!File.Exists(path))
            {
                path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "test.bin";
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException("no rom file to use!");
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Cart ROM: {0}", path);
            Console.ResetColor();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                MemoryStream rom = new MemoryStream();
               // GZipStream gZipStream = new GZipStream(rom, CompressionMode.Compress);
                fs.Position = 0;
                fs.CopyTo(rom);
                // return new GZipStream(gZipStream, CompressionMode.Decompress);
                return rom;
                // Turns out gzip streams are not seekable.
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
            Console.WriteLine("--- Phase: {0} ---", msg);
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            //m_Interpreter.SetDebuggingMode(true);
            //m_Interpreter.AddVBP_ReadGPR(29);

            InitLogging();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("N");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("64");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Emulator");
            Console.WriteLine("```````````````");
            Console.ResetColor();

            try
            {
                PhaseMsg("Load Cartridge");

                Stream romStream = GetCartRom(config.RomFilepath);

                var cart = new Cartridge(romStream);

                PhaseMsg("Core Setup");

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


                PhaseMsg("Execution");

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
