using System;
using System.Reflection;
using cor64;
using cor64.Mips.R4300I;
using NUnit.Framework;

namespace Tests
{
    public static class MachineSpawner
    {
        public static N64System CreateAndRun()
        {
            var system = new N64System();
            var cpu = new Interpreter();
            var cart = Asm.AssembleSimpleCart();

            system.CPU(cpu);
            system.Boot(cart);

            TestContext.Progress.WriteLine("Created test machine");

            Assert.DoesNotThrow(() =>
            {
                while (true)
                {
                    system.Tick();
                    system.TickFinally();
                    
                    if (cpu.InfiniteLoopWarn)
                        break;
                }
            }, "machine threw an exception PC=" + cpu.ReadPC().ToString("X8"));

            return system;
        }
    }
}