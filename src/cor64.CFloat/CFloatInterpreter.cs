using System;
using System.Runtime.InteropServices;
using cor64;
using cor64.Mips;
using cor64.Mips.R4300I;

namespace cor64.CFloat
{
    public class CFloatInterpreter : Interpreter
    {
        public override string Description => "Intepreter (CFloat)";

        private static readonly int _flag_dz = (int)FpuExceptionFlags.DivideByZero;
        private static readonly int _flag_ie = (int)FpuExceptionFlags.Inexact;
        private static readonly int _flag_iv = (int)FpuExceptionFlags.Invalid;
        private static readonly int _flag_uf = (int)FpuExceptionFlags.Underflow;
        private static readonly int _flag_of = (int)FpuExceptionFlags.Overflow;

        public CFloatInterpreter(bool debug) : base(debug)
        {
        }

        private void CheckFPUExceptions()
        {
            if (CF.has_errors())
            {
                int flags = 0;
                flags |= (CF.has_flag_dz() * _flag_dz);
                flags |= (CF.has_flag_ie() * _flag_ie);
                flags |= (CF.has_flag_iv() * _flag_iv);
                flags |= (CF.has_flag_uf() * _flag_uf);
                flags |= (CF.has_flag_of() * _flag_of);
                SetExceptionState((FpuExceptionFlags)flags);
            }
        }

        public sealed override void Add(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, CF.add_s(ReadFPR_S(inst.FloatSource), ReadFPR_S(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, CF.add_d(ReadFPR_D(inst.FloatSource), ReadFPR_D(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public sealed override void Divide(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, CF.div_s(ReadFPR_S(inst.FloatSource), ReadFPR_S(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, CF.div_d(ReadFPR_D(inst.FloatSource), ReadFPR_D(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public sealed override void Multiply(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, CF.mul_s(ReadFPR_S(inst.FloatSource), ReadFPR_S(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, CF.mul_d(ReadFPR_D(inst.FloatSource), ReadFPR_D(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public sealed override void Subtract(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, CF.sub_s(ReadFPR_S(inst.FloatSource), ReadFPR_S(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, CF.sub_d(ReadFPR_D(inst.FloatSource), ReadFPR_D(inst.FloatTarget)));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public sealed override void Ceil(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = CF.ceil_s(ReadFPR_S(inst.FloatSource));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = CF.ceil_d(ReadFPR_D(inst.FloatSource));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public sealed override void Floor(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = CF.floor_s(ReadFPR_S(inst.FloatSource));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = CF.floor_d(ReadFPR_D(inst.FloatSource));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public sealed override void Truncate(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = CF.trunc_s(ReadFPR_S(inst.FloatSource));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = CF.trunc_d(ReadFPR_D(inst.FloatSource));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public sealed override void Round(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = CF.round_s(ReadFPR_S(inst.FloatSource));
                CheckFPUExceptions();
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = CF.round_d(ReadFPR_D(inst.FloatSource));
                CheckFPUExceptions();
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }
    }
}
