using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace cor64.Tests
{
    public sealed class LoggingAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            LoggingConfiguration configuration = new LoggingConfiguration();

            String layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";

            NLogViewerTarget nLogViewerTarget = new NLogViewerTarget()
            {
                Layout = layout,
                Address = "tcp4://localhost:9999"
            };

            configuration.AddTarget("log4view", nLogViewerTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, nLogViewerTarget));

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget()
            {
                Layout = layout
            };

            configuration.AddTarget("console", consoleTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));

            LogManager.Configuration = configuration;
            LogManager.Flush();

            LogManager.GetLogger("Test").Info("---- Test {0} Start ----", methodUnderTest.Name);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            LogManager.GetLogger("Test").Info("---- Test {0} End   ----", methodUnderTest.Name);
        }
    }
}
