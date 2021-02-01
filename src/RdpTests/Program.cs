using System.Text;
using System.IO;
using System;
using NLog;
using NLog.Config;
using NLog.Targets;
using cor64.Rdp;
using cor64;
using cor64.Rdp.LLE;

namespace RdpTests
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupLogging();
            ViewRdpTest("DisplayListAquaBox.asm");
        }

        static void ViewRdpTest(String asmName) {
            N64System system = new N64System();
            var rdp = new MadCatRdp();
            rdp.SetDLDebug(true);
            system.DeviceRcp.SetRdpDevice(rdp);
            system.BootForTesting();

            AssembleAndRunRdp(system, asmName);
        }

        public static void AssembleAndRunRdp(N64System system, string asmName) {
            var displayList = Asm.AssembleDisplayList(asmName);

            // Write the display list bytes into beginning of RDRAM
            system.DeviceMemory.Write(0, displayList, 0, displayList.Length);

            // Setup the DP interface
            system.DeviceRcp.DisplayProcessorCommandInterface.DirectDLSetup(0, displayList.Length);
        }

        static void SetupLogging()
        {
            LoggingConfiguration configuration = new LoggingConfiguration();

            const String layout = "${message}";

            ConsoleTarget consoleTarget = new ConsoleTarget()
            {
                Layout = layout
            };

            configuration.AddTarget("console", consoleTarget);


            /* Must configure rules here so that all targets get activated */
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

            LogManager.Configuration = configuration;
        }
    }
}
