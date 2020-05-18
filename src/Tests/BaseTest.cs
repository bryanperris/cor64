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


            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget()
            {
                Layout = layout
            };

            configuration.AddTarget("console", consoleTarget);

            var rule = new LoggingRule("*", LogLevel.Trace, consoleTarget)
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
