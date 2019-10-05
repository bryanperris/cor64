using System;
using NUnit.Framework;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Tests
{
    public abstract class BaseTest
    {
        [SetUp]
        public void Setup()
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

            LogManager.GetLogger("Test").Info("---- Test {0} Start ----", TestContext.CurrentContext.Test.MethodName);
        }

        [TearDown]
        public void Teardown()
        {
            LogManager.GetLogger("Test").Info("---- Test {0} End   ----", TestContext.CurrentContext.Test.MethodName);
        }
    }
}
