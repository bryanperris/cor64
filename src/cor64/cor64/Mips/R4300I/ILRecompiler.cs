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

/******************
 * Let the interpreter base read bytes and increment PC
 * For each inst: Record static PC address, call emitter function
 * At end of block: Push code block, then call
 * 
 * IMPORTANT NOTES:
 * :: The recompiler handles delay slots by deffering the end of block by 1 instruction
 */

namespace cor64.Mips.R4300I
{
    public partial class ILRecompiler : Interpreter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private CoreBridge m_CoreBridge;
        private Interpreter m_FallbackInterpreter;
        private ILCodeEmitter m_Emitter;
        private bool m_EndOfBlock = false;
        private bool m_CompileMode = true;

        private ILBasicBlock m_CurrentBlock;
        private ILBasicBlock m_LastBlock;
        private Dictionary<ulong, ILBasicBlock> m_BlockCache = new Dictionary<ulong, ILBasicBlock>();

        private const int LOCAL_TYPE_U32 = 0;
        private const int LOCAL_TYPE_U64 = 1;
        private const int LOCAL_TYPE_I32 = 2;
        private const int LOCAL_TYPE_U32_2 = 3;
        private const int LOCAL_TYPE_U64_2 = 4;
        private const int LOCAL_TYPE_I64 = 5;

        private readonly static String READ_GPR_32 = "ReadGPR32";
        private readonly static String READ_GPR_64 = "ReadGPR64";
        private readonly static String WB_32 = "Writeback32";
        private readonly static String WB_64 = "Writeback64";
        private readonly static String EXCEPTION = "SetExceptionState";
        private readonly static String WB_128_32 = "WritebackHiLo32";
        private readonly static String WB_128_64 = "WritebackHiLo64";
        private readonly static String WB_128_128 = "WritebackHiLo64_U128";
        private readonly static String MULT_64_S = "Multiply64Signed";
        private readonly static String MULT_64_UN = "Multiply64Unsigned";
        private readonly static String IS_OP64 = "GetOperation64";
        private readonly static String WB_DYN = "WritebackDyn";

        private readonly MethodInfo METHOD_READ_GPR_32;
        private readonly MethodInfo METHOD_READ_GPR_64;
        private readonly MethodInfo METHOD_WB_32;
        private readonly MethodInfo METHOD_WB_64;
        private readonly MethodInfo METHOD_EXCEPTION;
        private readonly MethodInfo METHOD_WB_128_32;
        private readonly MethodInfo METHOD_WB_128_64;
        private readonly MethodInfo METHOD_WB_128_128;
        private readonly MethodInfo METHOD_MULT_64_S;
        private readonly MethodInfo METHOD_MULT_64_UN;
        private readonly MethodInfo METHOD_IS_OP64;
        private readonly MethodInfo METHOD_WB_DYN;

        public ILRecompiler(bool debug) :
            base(new Disassembler("o32", debug ? BaseDisassembler.Mode.Debug : BaseDisassembler.Mode.Fast))
        {
            m_CoreBridge = new CoreBridge(this);
            m_Emitter = new ILCodeEmitter();

            METHOD_READ_GPR_32 = m_CoreBridge.GetType().GetMethod(READ_GPR_32);
            METHOD_READ_GPR_64 = m_CoreBridge.GetType().GetMethod(READ_GPR_64);
            METHOD_WB_32 = m_CoreBridge.GetType().GetMethod(WB_32);
            METHOD_WB_64 = m_CoreBridge.GetType().GetMethod(WB_64);
            METHOD_EXCEPTION = m_CoreBridge.GetType().GetMethod(EXCEPTION);
            METHOD_WB_128_32 = m_CoreBridge.GetType().GetMethod(WB_128_32);
            METHOD_WB_128_64 = m_CoreBridge.GetType().GetMethod(WB_128_64);
            METHOD_WB_128_128 = m_CoreBridge.GetType().GetMethod(WB_128_128);
            METHOD_MULT_64_S = m_CoreBridge.GetType().GetMethod(MULT_64_S);
            METHOD_MULT_64_UN = m_CoreBridge.GetType().GetMethod(MULT_64_UN);
            METHOD_IS_OP64 = m_CoreBridge.GetType().GetMethod(IS_OP64);
            METHOD_WB_DYN = m_CoreBridge.GetType().GetMethod(WB_DYN);
        }

        public override string Description => "The MIPS IL Recompiler";

        protected override bool Execute()
        {
            if (!m_CompileMode)
            {
                return true;
            }

            /* Step clock */
            CoreClock.NextTick();

            /* Step coprocessor 0 */
            Cop0.ProcessorTick();

            return base.Execute();
        }

        public void SetFallbackInterpreter(Interpreter interpreter)
        {
            m_FallbackInterpreter = interpreter;
            interpreter.OverrideIStream(new StreamEx.Wrapper(IMemoryStream));
            interpreter.OverrideDStream(DMemoryStream);
            interpreter.OverrideCoreState(State);
            interpreter.OverrideCop0(Cop0);
            interpreter.BypassMMU = BypassMMU;
        }

        private int GetOpcodeReservedType(Opcode op)
        {
            /* 64-bit only */
            if (op.Flags.TestFlag(ExecutionFlags.Reserved32))
            {
                return 0;
            }

            /* 32-bit only */
            if (op.Flags.TestFlag(ExecutionFlags.Reserved64))
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        private void ExecuteBlock(ILBasicBlock block)
        {
            m_CompileMode = false;
            m_BlockStart = false;
            block.Execute();
            m_LastBlock = block;
            m_Pc = block.NextPC;
        }

        protected sealed override void BlockBegin()
        {
            if (m_FallbackInterpreter == null)
            {
                SetFallbackInterpreter(new SimpleInterpreter(DebugMode));
            }

            ILBasicBlock codeBlock = null;

            if (m_LastBlock != null && m_LastBlock.Address == m_Pc)
            {
                ExecuteBlock(m_LastBlock);
                return;
            }

            if (!m_BlockCache.TryGetValue(m_Pc, out codeBlock))
            {
                m_CompileMode = true;
                m_CurrentBlock = new ILBasicBlock(m_Pc, this, m_CoreBridge);
                m_Emitter.Begin(m_CurrentBlock);
                m_Emitter.DefineLocalVariable(typeof(uint));
                m_Emitter.DefineLocalVariable(typeof(ulong));
                m_Emitter.DefineLocalVariable(typeof(int));
                m_Emitter.DefineLocalVariable(typeof(uint));
                m_Emitter.DefineLocalVariable(typeof(ulong));
                m_Emitter.DefineLocalVariable(typeof(long));
                return;
            }
            else
            {
                ExecuteBlock(codeBlock);
            }
        }

        protected sealed override void BlockEnd()
        {
            if (m_CurrentBlock != null)
            {
                m_BlockCache.Add(m_CurrentBlock.Address, m_CurrentBlock);

                m_Emitter.End();

                /* Move the PC back to where the block is allocated to allow it be executed */
                m_Pc = m_CurrentBlock.Address;

                ExecuteBlock(m_CurrentBlock);

                m_CurrentBlock = null;
            }
        }

        protected sealed override void OpBegin()
        {
            m_Emitter.OpBegin();

            /* Allow the delay slot to be part of the block */
            if (m_EndOfBlock)
            {
                m_EndOfBlock = false;
                m_BlockEndFlag = true;
            }
        }

        protected sealed override void OpEnd()
        {
            m_Emitter.OpEnd();
        }


        /* -------------------------------------------------------
         * Mips Common Emitter Methods
         * -------------------------------------------------------*/

        private void EmitFallbackOp(DecodedInstruction inst)
        {
            m_Emitter.BridgeMethodCall("Fallback", () =>
            {
                m_Emitter.BlockRef();
                m_Emitter.Constant32(inst.IsBranch ? 1 : 0);
            });
        }

        private void EmitUpdatePC()
        {
            m_Emitter.BridgeMethodCall("UpdatePC", () =>
            {
                m_Emitter.BlockRef();
                m_Emitter.Constant32(m_EndOfBlock ? 1 : 0);
            });
        }

        private void EmitReserveCheck(DecodedInstruction instruction)
        {
            m_Emitter.BridgeMethodCall("ReserveCheck", () =>
            {
                m_Emitter.Constant32(GetOpcodeReservedType(instruction.Op));
            });

            m_Emitter.ReturnOnFalse();
        }

        private void EmitReadGPR(MethodInfo methodInfo, int reg)
        {
            m_Emitter.BridgeMethodCall(methodInfo, () => {
                m_Emitter.Constant32(reg);
            });
        }

        private void EmitReadGPRSignedExtended(MethodInfo methodInfo, int reg, CLRValueType signType)
        {
            EmitReadGPR(methodInfo, reg);

            if (signType == CLRValueType.UINT_64)
            {
                m_Emitter.SignedConvert(CLRValueType.INT_64);
            }
            else
            {
                m_Emitter.SignedConvert(CLRValueType.INT_32);
            }
        }

        private void EmitSignedImmediate(DecodedInstruction inst, CLRValueType valueType)
        {
            short imm = (short)inst.Immediate;

            if (valueType == CLRValueType.UINT_32)
            {
                m_Emitter.Constant32((uint)imm, true);
            }
            else
            {
                m_Emitter.Constant64((ulong)imm, true);
            }
        }

        private void EmitMipsException(ExceptionType type)
        {
            m_Emitter.BridgeMethodCall(METHOD_EXCEPTION, () =>
            {
                m_Emitter.Constant32((int)type);
            });
        }

        private void EmitGPRWriteback(MethodInfo methodInfo, int destReg, int resultLocal)
        {
            m_Emitter.BridgeMethodCall(methodInfo, () =>
            {
                m_Emitter.Constant32(destReg);
                m_Emitter.LoadFromLocalVariable(resultLocal);
            });
        }

        private void Emit128Writeback(MethodInfo info, int hiLocal, int loLocal)
        {
            m_Emitter.BridgeMethodCall(info, () =>
            {
                m_Emitter.LoadFromLocalVariable(hiLocal);
                m_Emitter.LoadFromLocalVariable(loLocal);
            });
        }

        private void EmitAddition(DecodedInstruction inst, CLRValueType valueType)
        {
            int         _LOCAL;
            MethodInfo  _GPR_READ;
            MethodInfo  _WRITEBACK;
            int         _DEST_GPR = 0;

            if (valueType == CLRValueType.UINT_32)
            {
                _LOCAL = LOCAL_TYPE_U32;
                _GPR_READ = METHOD_READ_GPR_32;
                _WRITEBACK = METHOD_WB_32;
            }
            else
            {
                _LOCAL = LOCAL_TYPE_U64;
                _GPR_READ = METHOD_READ_GPR_64;
                _WRITEBACK = METHOD_WB_64;
            }

            EmitReserveCheck(inst);

            void EmitOperands()
            {
                EmitReadGPR(_GPR_READ, inst.Source);

                if (inst.IsImmediate())
                {
                    EmitSignedImmediate(inst, valueType);
                    _DEST_GPR = inst.Target;
                }
                else
                {
                    EmitReadGPR(_GPR_READ, inst.Target);
                    _DEST_GPR = inst.Destination;
                }
            }

            if (inst.IsUnsigned())
            {
                EmitOperands();
                m_Emitter.MathAdd(false);
                m_Emitter.StoreToLocalVariable(_LOCAL);
            }
            else
            {
                m_Emitter.TryCatchBlock(() =>
                {
                    EmitOperands();
                    m_Emitter.MathAdd(true);
                    m_Emitter.UnsignedConvert(valueType);
                    m_Emitter.StoreToLocalVariable(_LOCAL);
                },

                () =>
                {
                    EmitMipsException(ExceptionType.Overflow);
                }, 

                typeof(OverflowException));
            }

            EmitGPRWriteback(_WRITEBACK, _DEST_GPR, _LOCAL);

            EmitUpdatePC();
        }

        private void EmitSubtraction(DecodedInstruction inst, CLRValueType valueType)
        {
            int        _LOCAL;
            MethodInfo _GPR_READ;
            MethodInfo _WRITEBACK;

            if (valueType == CLRValueType.UINT_32)
            {
                _LOCAL = LOCAL_TYPE_U32;
                _GPR_READ = METHOD_READ_GPR_32;
                _WRITEBACK = METHOD_WB_32;
            }
            else
            {
                _LOCAL = LOCAL_TYPE_U64;
                _GPR_READ = METHOD_READ_GPR_64;
                _WRITEBACK = METHOD_WB_64;
            }

            EmitReserveCheck(inst);

            if (inst.IsUnsigned())
            {
                EmitReadGPR(_GPR_READ, inst.Source);
                EmitReadGPR(_GPR_READ, inst.Target);
                m_Emitter.MathSub(false);
                m_Emitter.StoreToLocalVariable(_LOCAL);
            }
            else
            {
                m_Emitter.TryCatchBlock(() =>
                {
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Source, valueType);
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Target, valueType);
                    m_Emitter.MathSub(true);
                    m_Emitter.UnsignedConvert(valueType);
                    m_Emitter.StoreToLocalVariable(_LOCAL);
                },

                () =>
                {
                    EmitMipsException(ExceptionType.Overflow);
                }, 

                typeof(OverflowException));
            }

            EmitGPRWriteback(_WRITEBACK, inst.Destination, _LOCAL);

            EmitUpdatePC();
        }

        private void EmitShift(DecodedInstruction inst, CLRValueType valueType)
        {
            int         _LOCAL;
            MethodInfo  _GPR_READ;
            MethodInfo  _WRITEBACK;
            ILGenerator _CG = m_Emitter.RawEmitter;
            Action      _ZERO;
            int         _SIGN_SHIFT;

            if (valueType == CLRValueType.UINT_32)
            {
                _LOCAL = LOCAL_TYPE_U32;
                _GPR_READ = METHOD_READ_GPR_32;
                _WRITEBACK = METHOD_WB_32;
                _ZERO = () => m_Emitter.Constant32(0, true);
                _SIGN_SHIFT = 31;
            }
            else
            {
                _LOCAL = LOCAL_TYPE_U64;
                _GPR_READ = METHOD_READ_GPR_64;
                _WRITEBACK = METHOD_WB_64;
                _ZERO = () => m_Emitter.Constant64(0, true);
                _SIGN_SHIFT = 63;
            }

            EmitReserveCheck(inst);

            /* Push the value that will be shifted */
            EmitReadGPR(_GPR_READ, inst.Target);

            /* Duplicate the value */
            _CG.Emit(IL.Dup);

            /* Push the shift amount */
            if (inst.IsVariableShift())
            {
                EmitReadGPR(METHOD_READ_GPR_32, inst.Source);

                m_Emitter.Constant32(0x3F, true);
                _CG.Emit(IL.And);
            }
            else
            {
                if (valueType == CLRValueType.UINT_64 && inst.IsShift32())
                    m_Emitter.Constant32(inst.ShiftAmount + 32);
                else
                    m_Emitter.Constant32(inst.ShiftAmount);
            }

            /* Duplicate and store a copy of shift amount into local */
            _CG.Emit(IL.Dup);
            m_Emitter.StoreToLocalVariable(LOCAL_TYPE_I32);

            /* The shift operation */
            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                _CG.Emit(IL.Shl);
                m_Emitter.StoreToLocalVariable(_LOCAL);
                m_Emitter.Pop();
            }
            else
            {
                _CG.Emit(IL.Shr_Un);
                m_Emitter.StoreToLocalVariable(_LOCAL);

                /* Sign extend logic */
                if (!inst.IsUnsigned())
                {
                    /* Push sign bit of value */
                    m_Emitter.Constant32(_SIGN_SHIFT);
                    _CG.Emit(IL.Shr_Un);

                    /* Push ~0 */
                    _ZERO();

                    _CG.Emit(IL.Not);

                    /* Push shift amount */
                    m_Emitter.LoadFromLocalVariable(LOCAL_TYPE_I32);

                    /* Shift right and invert */
                    _CG.Emit(IL.Shr_Un);
                    _CG.Emit(IL.Not);

                    /* The result will be sign-extended if sign bit is 1 */
                    _CG.Emit(IL.Mul);

                    /* or value and sign-extension */
                    m_Emitter.LoadFromLocalVariable(_LOCAL);
                    _CG.Emit(IL.Or);

                    m_Emitter.StoreToLocalVariable(_LOCAL);
                }
                else
                {
                    m_Emitter.Pop();
                }
            }

            EmitGPRWriteback(_WRITEBACK, inst.Destination, _LOCAL);

            EmitUpdatePC();
        }

        private void EmitDivision(DecodedInstruction inst, CLRValueType valueType)
        {
            int         _LOCAL_HI;
            int         _LOCAL_LO;
            MethodInfo  _GPR_READ;
            MethodInfo  _WRITEBACK;
            ILGenerator _CG = m_Emitter.RawEmitter;

            if (valueType == CLRValueType.UINT_32)
            {
                _LOCAL_HI = LOCAL_TYPE_U32;
                _LOCAL_LO = LOCAL_TYPE_U32_2;
                _GPR_READ = METHOD_READ_GPR_32;
                _WRITEBACK = METHOD_WB_128_32;
            }
            else
            {
                _LOCAL_HI = LOCAL_TYPE_U64;
                _LOCAL_LO = LOCAL_TYPE_U64_2;
                _GPR_READ = METHOD_READ_GPR_64;
                _WRITEBACK = METHOD_WB_128_64;
            }

            EmitReserveCheck(inst);

            m_Emitter.TryCatchBlock(() =>
            {
                if (inst.IsUnsigned())
                {
                    EmitReadGPR(_GPR_READ, inst.Source);
                    EmitReadGPR(_GPR_READ, inst.Target);
                    _CG.Emit(IL.Div_Un);

                    EmitReadGPR(_GPR_READ, inst.Source);
                    EmitReadGPR(_GPR_READ, inst.Target);
                    _CG.Emit(IL.Rem_Un);
                }
                else
                {
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Source, valueType);
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Target, valueType);
                    _CG.Emit(IL.Div);
                    m_Emitter.UnsignedConvert(valueType);

                    EmitReadGPRSignedExtended(_GPR_READ, inst.Source, valueType);
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Target, valueType);
                    _CG.Emit(IL.Rem);
                    m_Emitter.UnsignedConvert(valueType);
                }

                m_Emitter.StoreToLocalVariable(_LOCAL_HI);
                m_Emitter.StoreToLocalVariable(_LOCAL_LO);
            }, () =>

            {
                return;
            }, 

            typeof(DivideByZeroException));

            Emit128Writeback(_WRITEBACK, _LOCAL_HI, _LOCAL_LO);

            EmitUpdatePC();
        }

        private void EmitMultiplication(DecodedInstruction inst, CLRValueType valueType)
        {
            int         _LOCAL_HI;
            int         _LOCAL_LO;
            MethodInfo  _GPR_READ;
            MethodInfo  _WRITEBACK;
            ILGenerator _CG = m_Emitter.RawEmitter;

            if (valueType == CLRValueType.UINT_32)
            {
                _LOCAL_HI = LOCAL_TYPE_U32;
                _LOCAL_LO = LOCAL_TYPE_U32_2;
                _GPR_READ = METHOD_READ_GPR_32;
                _WRITEBACK = METHOD_WB_128_32;
            }
            else
            {
                _LOCAL_HI = LOCAL_TYPE_U64;
                _LOCAL_LO = LOCAL_TYPE_U64_2;
                _GPR_READ = METHOD_READ_GPR_64;
                _WRITEBACK = METHOD_WB_128_128;
            }

            EmitReserveCheck(inst);

            /* 32-bit path */
            if (valueType == CLRValueType.UINT_32)
            {
                if (inst.IsUnsigned())
                {
                    EmitReadGPR(_GPR_READ, inst.Source);
                    m_Emitter.UnsignedConvert(CLRValueType.UINT_64);
                    EmitReadGPR(_GPR_READ, inst.Target);
                    m_Emitter.UnsignedConvert(CLRValueType.UINT_64);
                }
                else
                {
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Source, valueType);
                    m_Emitter.SignedConvert(CLRValueType.INT_64);
                    m_Emitter.UnsignedConvert(CLRValueType.UINT_64);
                    EmitReadGPRSignedExtended(_GPR_READ, inst.Target, valueType);
                    m_Emitter.SignedConvert(CLRValueType.INT_64);
                    m_Emitter.UnsignedConvert(CLRValueType.UINT_64);
                }


                _CG.Emit(IL.Mul);
                m_Emitter.UnsignedConvert(CLRValueType.UINT_64);
                _CG.Emit(IL.Dup);

                /* Lo */
                m_Emitter.UnsignedConvert(CLRValueType.UINT_32);
                m_Emitter.StoreToLocalVariable(_LOCAL_LO);

                /* Hi */
                m_Emitter.Constant32(32, false);
                _CG.Emit(IL.Shr_Un);
                m_Emitter.UnsignedConvert(CLRValueType.UINT_32);
                m_Emitter.StoreToLocalVariable(_LOCAL_HI);

                Emit128Writeback(_WRITEBACK, _LOCAL_HI, _LOCAL_LO);
            }
            else
            {
                m_Emitter.BridgeMethodCall(_WRITEBACK, () =>
                {
                    if (inst.IsUnsigned())
                    {
                        m_Emitter.BridgeMethodCall(METHOD_MULT_64_UN, () =>
                        {
                            EmitReadGPR(_GPR_READ, inst.Source);
                            EmitReadGPR(_GPR_READ, inst.Target);
                        });
                    }
                    else
                    {
                        m_Emitter.BridgeMethodCall(METHOD_MULT_64_S, () =>
                        {
                            EmitReadGPR(_GPR_READ, inst.Source);
                            EmitReadGPR(_GPR_READ, inst.Target);
                        });
                    }
                });
            }

            EmitUpdatePC();
        }

        private void EmitBitwise(DecodedInstruction inst)
        {
            int         _LOCAL     = LOCAL_TYPE_U64;
            MethodInfo  _GPR_READ  = METHOD_READ_GPR_64;
            MethodInfo  _WRITEBACK = METHOD_WB_DYN;
            int         _DEST_GPR  = 0;
            ILGenerator _CG        = m_Emitter.RawEmitter;

            EmitReadGPR(_GPR_READ, inst.Source);

            if (inst.IsImmediate())
            {
                _DEST_GPR = inst.Target;
                m_Emitter.Constant64(inst.Immediate, true);
            }
            else
            {
                _DEST_GPR = inst.Destination;
                EmitReadGPR(_GPR_READ, inst.Target);
            }

            switch (inst.Op.ArithmeticType)
            {
                case ArithmeticOp.AND:   _CG.Emit(IL.And); break;
                case ArithmeticOp.OR:    _CG.Emit(IL.Or);  break;
                case ArithmeticOp.XOR:   _CG.Emit(IL.Xor); break;
                case ArithmeticOp.NOR: { _CG.Emit(IL.Or); _CG.Emit(IL.Not); break; }
                default: throw new InvalidOperationException("Invalid selected IL Bitwise logic");
            }

            m_Emitter.StoreToLocalVariable(_LOCAL);

            EmitGPRWriteback(_WRITEBACK, _DEST_GPR, _LOCAL);

            EmitUpdatePC();
        }

        /* -------------------------------------------------------
         * Mips Op Callbacks
         * -------------------------------------------------------*/

        protected override void BitwiseLogic(DecodedInstruction inst)
        {
            EmitBitwise(inst);
        }

        protected sealed override void Add32(DecodedInstruction inst)
        {
            EmitAddition(inst, CLRValueType.UINT_32);
        }

        protected sealed override void Add64(DecodedInstruction inst)
        {
            EmitAddition(inst, CLRValueType.UINT_64);
        }

        protected sealed override void Subtract32(DecodedInstruction inst)
        {
            EmitSubtraction(inst, CLRValueType.UINT_32);
        }

        protected sealed override void Subtract64(DecodedInstruction inst)
        {
            EmitSubtraction(inst, CLRValueType.UINT_64);
        }

        protected sealed override void Shift32(DecodedInstruction inst)
        {
            EmitShift(inst, CLRValueType.UINT_32);
        }

        protected sealed override void Shift64(DecodedInstruction inst)
        {
            EmitShift(inst, CLRValueType.UINT_64);
        }

        protected sealed override void Divide32(DecodedInstruction inst)
        {
            EmitDivision(inst, CLRValueType.UINT_32);
        }

        protected sealed override void Divide64(DecodedInstruction inst)
        {
            EmitDivision(inst, CLRValueType.UINT_64);
        }

        protected sealed override void Multiply32(DecodedInstruction inst)
        {
            EmitMultiplication(inst, CLRValueType.UINT_32);
        }

        protected sealed override void Multiply64(DecodedInstruction inst)
        {
            EmitMultiplication(inst, CLRValueType.UINT_64);
        }

        protected sealed override void SetOnLessThan(DecodedInstruction inst)
        {
            EmitFallbackOp(inst);
        }

        protected sealed override void TransferReg(DecodedInstruction inst)
        {
            EmitFallbackOp(inst);
        }

        protected sealed override void Branch(DecodedInstruction inst)
        {
            m_EndOfBlock = true;
            EmitFallbackOp(inst);
        }

        protected sealed override void Jump(DecodedInstruction inst)
        {
            m_EndOfBlock = true;
            EmitFallbackOp(inst);
        }

        protected sealed override void Store(DecodedInstruction inst)
        {
            EmitFallbackOp(inst);
        }

        protected sealed override void Load(DecodedInstruction inst)
        {
            EmitFallbackOp(inst);
        }

        protected sealed override void Cache(DecodedInstruction inst)
        {
            EmitFallbackOp(inst);
        }

        protected sealed override void Sync(DecodedInstruction inst)
        {
            EmitFallbackOp(inst);
        }
    }
}
