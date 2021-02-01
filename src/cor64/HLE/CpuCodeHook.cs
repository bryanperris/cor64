using System;
using cor64.Mips.R4300I;
using cor64.Mips;

namespace cor64.HLE {
    public abstract class CpuCodeHook {

        public enum ReturnControl {
            Normal,
            HLECall
        }

        public CpuCodeHook(InterpreterBaseR4300I core) {
            Core = core;
        }

        public abstract ReturnControl Execute();

        protected uint ReadArg32(int index) {
            return Core.CPUABI32_ReadArgData(index);
        }

        public virtual void DebugPrint() {

        }

        protected InterpreterBaseR4300I Core { get; }

        public ulong? DebugPrintAddressOffset { get; protected set; } = null;

        public CpuCodeHook MakeDebugPrintHooK() => new CodeHookDebugPrint(this);

        private sealed class CodeHookDebugPrint : CpuCodeHook {
            private readonly CpuCodeHook m_ParentHook;

            public CodeHookDebugPrint(CpuCodeHook parentHook) : base(null)
            {
                m_ParentHook = parentHook;
            }

            public override ReturnControl Execute()
            {
                m_ParentHook.DebugPrint();
                return ReturnControl.Normal;
            }
        }
    }
}