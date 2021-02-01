using System;
using cor64.Mips.R4300I;

namespace cor64.HLE {
    public abstract class CpuStruct {

        public abstract void Populate(InterpreterBaseR4300I core, uint address);

        public static T NewStruct<T>(InterpreterBaseR4300I core, uint address)
            where T : CpuStruct {
                var obj = (T)Activator.CreateInstance(typeof(T), Array.Empty<object>());
                obj.Populate(core, address);
                return obj;
            }
    }
}