using System;
using System.IO;
using cor64.Mips;
using Newtonsoft.Json;
using NLog;
using CommandLine;
using cor64.Mips.R4300I;

namespace RunN64
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static Emulator s_Emulator;

        public class Options
        {
            [Option('w', "workbench", Required = false, HelpText = "Start the emulator in workbench mode.", Default = false)]
            public bool WorkbenchMode { get; set; }
        }


        private static void Finish()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n\n****** The program has ended");
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            var parsedArgs = Parser.Default.ParseArguments<Options>(args).WithParsed((options) =>
            {
                // Sync up the workbench mode, very important
                cor64.CoreConfig.Current.WorkbenchMode = options.WorkbenchMode;

                s_Emulator = new Emulator();
                s_Emulator.WorkbenchMode = options.WorkbenchMode;
                s_Emulator.Configuration = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
                s_Emulator.SetupLogging();

                Console.ForegroundColor = ConsoleColor.Green;
                Log.Info("****************************************");
                Log.Info("* COR64 - A N64 emulator written in C# *");
                Log.Info("****************************************");

                try
                {
                    s_Emulator.Start();

                    CommandLoop();

                    // if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    // {
                    //     Console.ReadLine();
                    // }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Log.Error("\n--- An application error occured! ---");
                    Log.Error("Exception: {0}", e.Message);
                    Log.Error("Stack Trace: {0}", e.ToString());
                }
            });
        }

        private static void CommandLoop()
        {
            do
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Q)
                    {
                        s_Emulator.Host.Interrupt();

                        if (s_Emulator.CPU.StartedWithProfiler)
                        {
                            Console.WriteLine("\n--- Profiler Results ---");
                            Console.Write(s_Emulator.CPU.GetProfiledResults());
                            Console.WriteLine("------");
                        }
                    }

                    if (key.Key == ConsoleKey.B)
                    {
                        if (!s_Emulator.System.Dbg.IsBreakActive)
                        {
                            s_Emulator.System.Dbg.Break();
                        }
                    }

                    if (key.Key == ConsoleKey.C)
                    {
                        if (s_Emulator.System.Dbg.IsBreakActive)
                        {
                            s_Emulator.System.Dbg.Continue();
                        }
                    }

                    if (key.Key == ConsoleKey.S)
                    {
                        s_Emulator.System.Dbg.Step();
                    }

                    if (key.Key == ConsoleKey.V)
                    {
                        Log.Info("Forcing video interrupt to fire");
                        s_Emulator.ForceVideoInterrupt();
                    }

                    if (key.Key == ConsoleKey.T)
                    {
                        Log.Info("Force Mips Timer");
                        var count = s_Emulator.CPU.Cop0.TimerCount;
                        count += 10000000;
                        s_Emulator.CPU.Cop0.CpuRegisterWrite(CTS.CP0_REG_COMPARE, count);
                    }

                    if (key.Key == ConsoleKey.D)
                    {
                        s_Emulator.CPU.SetInstructionDebugMode(InstructionDebugMode.Full);
                    }
                }

                if (s_Emulator.Host.ThrownException != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Log.Error("\n--- An emulator error occured! ---");
                    Log.Error("Exception: {0}", s_Emulator.Host.ThrownException.Message);
                    Log.Error("Stack Trace: {0}", s_Emulator.Host.ThrownException.StackTrace);

                    try
                    {
                        s_Emulator.DumpExecutionLog();
                    }
                    catch (Exception e)
                    {
                        Log.Error("\n--- A dump stack trace error occured! ---");
                        Log.Error("Exception: {0}", e.Message);
                        Log.Error("Stack Trace: {0}", e.StackTrace);
                    }


                    Log.Error("Last Instruction: " + s_Emulator.System.DeviceCPU.Disassembler.GetFullDisassembly(s_Emulator.System.DeviceCPU.CurrentInst));

                    s_Emulator.DumpStateToLog();

                    Finish();
                    break;
                }
            }

            while (s_Emulator.Host.IsRunning);
        }
    }
}
