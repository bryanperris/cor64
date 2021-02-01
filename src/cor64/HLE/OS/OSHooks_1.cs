using System;
using System.Collections.Generic;
using cor64.Mips;
using NLog;
using cor64.HLE.OS.V1;
using cor64.Mips.R4300I;

namespace cor64.HLE.OS {
    public static class OSHooks_1 {
        public static void AddHooks(InterpreterBaseR4300I core, IDictionary<ulong, CpuCodeHook> hooks) {
            AddHook(hooks, 0x803226B0, new CreateThreadHook(core));
            AddHook(hooks, 0x8032AE70, new DestroyThreadHook(core));
            AddHook(hooks, 0x80327D58, new PopThreadHook(core));
        }

        private static void AddHook(IDictionary<ulong, CpuCodeHook> hooks, ulong address, CpuCodeHook hook) {
            hooks.Add(address, hook);

            #if DEBUG && DEBUG_OS
            if (hook.DebugPrintAddressOffset.HasValue) {
                var postAddr = address + hook.DebugPrintAddressOffset.Value;
                hooks.Add(postAddr, hook.MakeDebugPrintHooK());
            }
            #endif
        }
    }
}