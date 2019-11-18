using System;
using NUnit.Framework;
using NLog;
using NLog.Config;
using NLog.Targets;
using cor64;
using cor64.BareMetal;
using NLog.Filters;

namespace Tests
{
    public abstract class BaseTest
    {
        [SetUp]
        public void Setup()
        {
            Console.SetOut(TestContext.Progress);

            LoggingConfiguration configuration = new LoggingConfiguration();

            String layout = @"${logger} ${message}";

            // NLogViewerTarget nLogViewerTarget = new NLogViewerTarget()
            // {
            //     Layout = layout,
            //     Address = "tcp4://localhost:9999"
            // };

            // configuration.AddTarget("log4view", nLogViewerTarget);
            // configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, nLogViewerTarget));

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget()
            {
                Layout = layout
            };

            configuration.AddTarget("console", consoleTarget);

            var rule = new LoggingRule("*", LogLevel.Debug, consoleTarget)
            {
                DefaultFilterResult = FilterResult.Log
            };

            rule.Filters.Add(new ConditionBasedFilter()
            {
                Condition = "starts-with('${logger}','cor64.BassSharp')",
                Action = FilterResult.Ignore
            });

            configuration.LoggingRules.Add(rule);

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
