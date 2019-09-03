using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using IL = System.Reflection.Emit.OpCodes;
using System.Runtime.CompilerServices;
using cor64.MSIL;
using cor64.IO;

namespace cor64.Mips.R4300I
{
    public partial class ILRecompiler : CoreR4300I
    {
        private class CoreBridge
        {
            private ILRecompiler m_Core;
            private Stack<ulong> m_JumpStack = new Stack<ulong>();

            public CoreBridge(ILRecompiler core)
            {
                m_Core = core;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Writeback32(int n, uint v)
            {
                m_Core.Writeback32(n, v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Writeback64(int n, ulong v)
            {
                m_Core.Writeback64(n, v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackDyn(int n, ulong v)
            {
                /* TODO: Should look into caching the selected writeback when the bit is set */

                if (m_Core.IsOperation64)
                    m_Core.Writeback64(n, v);
                else
                    m_Core.Writeback64(n, (uint)v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackHiLo32(uint r, uint v)
            {
                m_Core.WritebackHiLo32(r, v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackHiLo64(ulong r, ulong v)
            {
                m_Core.WritebackHiLo64(r, v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackHiLo64_U128(UInt128 v)
            {
                m_Core.WritebackHiLo64(v.hi, v.lo);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackLo(ulong value)
            {
                m_Core.WriteLo(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackHi(ulong value)
            {
                m_Core.WriteHi(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WritebackCop0(int reg, ulong value)
            {
                m_Core.State.Cp0.RegWrite(reg, value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetExceptionState(ExceptionType t)
            {
                m_Core.SetExceptionState(t);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IntPtr GetGprRef()
            {
                return m_Core.State.GetGprRef();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint ReadGPR32(int i)
            {
                return m_Core.ReadGPR32(i);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong ReadGPR64(int i)
            {
                return m_Core.ReadGPR64(i);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong ReadLo()
            {
                return m_Core.ReadLo();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong ReadHi()
            {
                return m_Core.ReadHi();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong ReadCop0Register(int select, bool isDwordInst)
            {
                return m_Core.ReadCp0Value(select, isDwordInst);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UInt128 Multiply64Signed(ulong a, ulong b)
            {
                return CoreUtils.Multiply64_Signed(a, b);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UInt128 Multiply64Unsigned(ulong a, ulong b)
            {
                return CoreUtils.Multiply64_Unsigned(a, b);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ReserveCheck(int mode)
            {
                bool op64 = m_Core.IsOperation64;

                if (((mode == 0) && !op64) || ((mode == 1) && op64))
                {
                    SetExceptionState(ExceptionType.Reserved);
                    return false;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Fallback(ILBasicBlock block, bool isBranch, int fallbackInstIndex)
            {
                var fallback = m_Core.m_FallbackInterpreter;

                /* Set the fallback PC to the address of the inst */
                fallback.SafeSetPC(block.NextPC);
                fallback.InjectInst(block.FallbackInstructions[fallbackInstIndex]);
                fallback.Step();

                /* Test units require this sync */
                m_Core.TargetAddress = fallback.TargetAddress;
                m_Core.BranchDelay = fallback.BranchDelay;

                if (fallback.WillJump)
                {
                    m_JumpStack.Push((uint)fallback.TargetAddress);
                    block.LinkPC(fallback.ReadPC());
                    fallback.ClearBranchUnit();
                    return;
                }

                if (m_JumpStack.Count > 0)
                {
                    var pc = m_JumpStack.Pop();
                    block.LinkPC(pc);
                }
                else
                {
                    block.LinkPC(fallback.ReadPC());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UpdatePC(ILBasicBlock block, bool isDelaySlot)
            {
                if (m_JumpStack.Count > 0)
                {
                    var pc = m_JumpStack.Pop();
                    block.LinkPC(pc);
                }
                else
                {
                    block.IncrementPC();
                }
            }

            public bool GetOperation64()
            {
                return m_Core.IsOperation64;
            }
        }
    }
}
