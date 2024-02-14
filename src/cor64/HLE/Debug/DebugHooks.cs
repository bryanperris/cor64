using System;
using System.Collections.Generic;
using cor64.Mips;
using NLog;
using cor64.HLE.OS.V1;
using cor64.Mips.R4300I;

namespace cor64.HLE.Debug {
    public static class DebugHooks {
        public static void AddHooks(InterpreterBaseR4300I core, IDictionary<long, CpuCodeHook> hooks) {
            AddHook(hooks, 0x84900000, new PrintHook(core));
        }

        private static void AddHook(IDictionary<long, CpuCodeHook> hooks, long address, CpuCodeHook hook) {
            hooks.Add(address, hook);
        }
    }
}