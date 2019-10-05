using cor64.Mips.Analysis;
using cor64.Mips.R4300I.JitCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using IL = System.Reflection.Emit.OpCodes;

namespace cor64.Mips.R4300I.JitIL
{
    public abstract class MipsILEmitter : ILEmitter, IMipsOpcodes
    {
        private Dictionary<String, MethodInfo> m_MipsBindings = new Dictionary<string, MethodInfo>();
        private Dictionary<String, MethodInfo> m_BlockBindings = new Dictionary<string, MethodInfo>();
        public bool IsDelaySlot { get; set; }
        protected EmitterRegisterMap m_RegAllocationMap;
        protected RuntimeBasicBlock m_TargetBlock;
        protected bool m_IsRuntime64;
        public bool IsEmit64 => m_IsRuntime64;
        private Label m_InterruptFunc;
        private Label m_NextInst;
        public bool IsExceptionPath { get; set; }
        private Label m_BlockEndLabel;

        private static String[] GetMemDataName =
        {
            "Invalid",
            nameof(IDynamicMips.GetMemData8),
            nameof(IDynamicMips.GetMemData16),
            "Invalid",
            nameof(IDynamicMips.GetMemData32),
            "Invalid",
            "Invalid",
            "Invalid",
            nameof(IDynamicMips.GetMemData64)
        };

        private static String[] SetMemDataName =
{
            "Invalid",
            nameof(IDynamicMips.SetMemData8),
            nameof(IDynamicMips.SetMemData16),
            "Invalid",
            nameof(IDynamicMips.SetMemData32),
            "Invalid",
            "Invalid",
            "Invalid",
            nameof(IDynamicMips.SetMemData64)
        };

        private static CLRValueType[] MemSignedDataType =
        {
            CLRValueType.Unknown,
            CLRValueType.INT_8,
            CLRValueType.INT_16,
            CLRValueType.Unknown,
            CLRValueType.INT_32,
            CLRValueType.Unknown,
            CLRValueType.Unknown,
            CLRValueType.Unknown,
            CLRValueType.INT_64
        };

        private static CLRValueType[] MemUnsignedDataType =
{
            CLRValueType.Unknown,
            CLRValueType.UINT_8,
            CLRValueType.UINT_16,
            CLRValueType.Unknown,
            CLRValueType.UINT_32,
            CLRValueType.Unknown,
            CLRValueType.Unknown,
            CLRValueType.Unknown,
            CLRValueType.UINT_64
        };

        public void SetBlockCompilationTarget(RuntimeBasicBlock block)
        {
            m_TargetBlock = block;
            m_RegAllocationMap = block.GenerateRegAllocationMap(this, 0);
        }

        public void BlockBegin()
        {
            m_InterruptFunc = DefineLabel();
            m_BlockEndLabel = DefineLabel();
            EmitLoadLocals();
        }

        public override void InstructionBegin(DecodedInstruction inst)
        {
            base.InstructionBegin(inst);
            m_NextInst = DefineLabel();

            if (!IsExceptionPath)
            {
                EmitMipsCall(nameof(IDynamicMips.CheckInterrupts), null);
                Emit(IL.Brfalse, m_NextInst);
                EmitStoreLocals();
                EmitMipsCall(nameof(IDynamicMips.ExceptionBegin), EmitBlockRef);
                EmitLoadLocals();
            }

            EmitNop();
            MarkLabel(m_NextInst);
        }

        public void BlockEnd()
        {
            EmitNop();
            MarkLabel(m_BlockEndLabel);

            EmitStoreLocals();
            Emit(IL.Ret);

            //if (!IsExceptionPath)
            //{
            //    EmitNop();
            //    MarkLabel(m_InterruptFunc);

            //    EmitStoreLocals();
            //    EmitMipsCall(nameof(IDynamicMips.ExceptionBegin), EmitBlockRef);
            //    EmitLoadLocals();
            //    Emit(IL.Br, m_NextInst);
            //}
        }

        public abstract void EmitLoadLocals();

        public abstract void EmitStoreLocals();

        public void EmitMipsRef()
        {
            Emit(IL.Ldarg_0);
        }

        public void EmitBlockRef()
        {
            Emit(IL.Ldarg_1);
        }

        private void AppendInterfaceMapping(IDictionary<string, MethodInfo> dict, Type iface, Object obj)
        {
            var ifaceMethods = obj.GetType().GetInterfaceMap(iface);

            foreach (var method in ifaceMethods.TargetMethods)
            {
                String name = method.Name;
                name = name.Substring(name.LastIndexOf('.') + 1);
                dict.Add(name, method);
            }
        }

        public void GenerateOwnerBindings(IDynamicMips dynamicMips)
        {
            m_MipsBindings.Clear();
            AppendInterfaceMapping(m_MipsBindings, typeof(IDynamicMips), dynamicMips);
        }

        public void GenerateBlockBindings(IDynamicBlock dynamicBlock) {
            m_BlockBindings.Clear();
            AppendInterfaceMapping(m_BlockBindings, typeof(IDynamicBlock), dynamicBlock);
        }

        public void EmitMipsCall(String methodName, Action emitterAction)
        {
            EmitMipsRef();
            emitterAction?.Invoke();
            Emit(IL.Call, m_MipsBindings[methodName]);
        }

        public void EmitBlockCall(String methodName, Action emitterAction)
        {
            EmitBlockRef();
            emitterAction?.Invoke();
            Emit(IL.Call, m_MipsBindings[methodName]);
        }

        /* -- Functions that affect MIPS core state  */

        public void CoreEmit_LoadGpr32(int reg) {
            if (reg > 0)
                EmitMipsCall(nameof(IDynamicMips.ReadGPR32), () => Constant32(reg));
        }

        public void CoreEmit_StoreGpr32(int reg)
        {
            if (reg > 0)
                EmitMipsCall(nameof(IDynamicMips.ReadGPR32), () => Constant32(reg));
        }

        public void CoreEmit_LoadGpr64(int reg) {
            if (reg > 0)
                EmitMipsCall(nameof(IDynamicMips.ReadGPR64), () => Constant32(reg));
        }

        public void CoreEmit_StoreGpr64(int reg)
        {
            if (reg > 0)
                EmitMipsCall(nameof(IDynamicMips.WriteGPR64), () => Constant32(reg));
        }

        public void CoreEmit_MipsException(ExceptionType type) => EmitMipsCall(nameof(IDynamicMips.SetExceptionState), () => Constant32((int)type));

        public void CoreEmit_FpuException(FpuExceptionFlags flags) => EmitMipsCall(nameof(IDynamicMips.SetFpuExceptionState), () => Constant32((int)flags));

        public void EmitDuplicate()
        {
            Emit(IL.Dup);
        }

        /* -- Functions used with register allocation  */

        public void EmitGpr_L(int reg)
        {
            if (reg == 0)
            {
                if (m_IsRuntime64)
                {
                    Constant64(0, true);
                }
                else
                {
                    Constant32(0, true);
                }

                return;
            }

            var mappedLocal = m_RegAllocationMap.GprMap[reg];

            if (mappedLocal < 0)
            {
                throw new ArgumentException("EmitGpr_L - selected reg local is not mapped");
            }

            EmitLocalLoad(mappedLocal);
        }

        public void EmitGpr_S(int reg) {
            var mappedLocal = m_RegAllocationMap.GprMap[reg];

            if (mappedLocal < 0)
            {
                throw new ArgumentException("EmitGpr_S - selected reg local is not mapped");
            }

            if (reg > 0)
                EmitLocalStore(mappedLocal);
            else
                EmitLocalPop();
        }

        public void EmitLo_L() => EmitLocalLoad(m_RegAllocationMap.MappedLo);

        public void EmitHi_L() => EmitLocalLoad(m_RegAllocationMap.MappedHi);

        public void EmitLo_S() => EmitLocalStore(m_RegAllocationMap.MappedLo);

        public void EmitHi_S() => EmitLocalStore(m_RegAllocationMap.MappedHi);

        public void EmitAddress_L() => EmitLocalLoad(m_RegAllocationMap.MappedAddress);

        public void EmitAddress_S() => EmitLocalStore(m_RegAllocationMap.MappedAddress);

        public void EmitFprD_L(int reg) => EmitLocalLoad(m_RegAllocationMap.FprDMap[reg]);

        public void EmitFprD_S(int reg) => EmitLocalStore(m_RegAllocationMap.FprDMap[reg]);

        public void EmitFprF_L(int reg) => EmitLocalLoad(m_RegAllocationMap.FprFMap[reg]);

        public void EmitFprF_S(int reg) => EmitLocalStore(m_RegAllocationMap.FprFMap[reg]);

        public void EmitFprW_Ptr(int reg)
        {
            int local = 0;

            if (m_RegAllocationMap.FprDMap[reg] >= 0)
            {
                local = m_RegAllocationMap.FprDMap[reg];
            }

            if (m_RegAllocationMap.FprFMap[reg] >= 0)
            {
                local = m_RegAllocationMap.FprFMap[reg];
            }

            Emit(IL.Ldarga, local);
            Emit(IL.Conv_U);
        }

        public void EmitFprW_L(int reg) {
            Emit(IL.Ldind_U4);
        }

        public void EmitFprW_S(int reg)
        {
            Emit(IL.Stind_I4);
        }

        public void EmitGpr32_L(int reg) {
            EmitGpr_L(reg);
            UnsignedConvert(CLRValueType.UINT_32);
        }

        public void EmitImm32(DecodedInstruction inst) => Constant32((uint)(short)inst.Immediate, true);

        public void EmitImm64(DecodedInstruction inst) => Constant64((ulong)(short)inst.Immediate, true);

        public void EmitSignExtend64()
        {
            SignedConvert(CLRValueType.INT_32);

            /* This call is required on Windows */
            SignedConvert(CLRValueType.INT_64);

            UnsignedConvert(CLRValueType.UINT_64);
        }

        public void CoreEmit_Cop0_Read(int reg)
        {
            EmitMipsCall(nameof(IDynamicMips.Cop0_Read), () =>
            {
                Constant32(reg);
            });
        }

        public void CoreEmit_Cop0_Write_BEGIN(int reg)
        {
            EmitMipsRef();
            Constant32(reg);
        }

        public void CoreEmit_Cop0_Write_END()
        {
            Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.Cop0_Write)]);
        }

        public void CoreEmit_Cop1Ctrl_Read()
        {
            EmitMipsCall(nameof(IDynamicMips.Cop1_Ctrl_Read), null);
        }

        public void CoreEmit_Cop1Ctrl_Write_BEGIN()
        {
            EmitMipsRef();
        }

        public void CoreEmit_Cop1Ctrl_Write_END()
        {
            UnsignedConvert(CLRValueType.UINT_32);
            Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.Cop1_Ctrl_Write)]);
        }

        /* -- MIPS Core Instructions */

        public void BitwiseLogic(DecodedInstruction inst)
        {
            LogIL("Opcode Bitwise");

            int destReg;

            EmitGpr_L(inst.Source);

            if (inst.IsImmediate())
            {
                destReg = inst.Target;

                if (m_IsRuntime64)
                    Constant64(inst.Immediate, true);
                else
                    Constant32(inst.Immediate, true);
            }
            else
            {
                destReg = inst.Destination;
                EmitGpr_L(inst.Target);
            }

            switch (inst.Op.ArithmeticType)
            {
                case ArithmeticOp.AND:   Emit(IL.And); break;
                case ArithmeticOp.OR:    Emit(IL.Or); break;
                case ArithmeticOp.XOR:   Emit(IL.Xor); break;
                case ArithmeticOp.NOR: { Emit(IL.Or); Emit(IL.Not); break; }
                default: throw new InvalidOperationException("Invalid selected IL Bitwise logic");
            }

            EmitGpr_S(destReg);
        }

        public void TransferReg(DecodedInstruction inst)
        {
            LogIL("Opcode TransferReg");

            int gpr_Source = inst.Destination;
            int gpr_Target = inst.Target;

            InterpreterBaseR4300I.RegTransferGprHelper(inst, out gpr_Source, out gpr_Target);

            if (inst.Op.XferTarget == RegBoundType.Cp0)
            {
                CoreEmit_Cop0_Write_BEGIN(inst.Destination);
            }

            if (inst.Op.XferTarget == RegBoundType.Cp1Ctl)
            {
                CoreEmit_Cop1Ctrl_Write_BEGIN();
            }

            if (inst.Op.XferSource == RegBoundType.Cp1 || inst.Op.XferTarget == RegBoundType.Cp1)
            {
                EmitFprW_Ptr(inst.FloatSource);
            }

            switch (inst.Op.XferSource)
            {
                case RegBoundType.Hi: EmitHi_L(); break;
                case RegBoundType.Lo: EmitLo_L(); break;
                case RegBoundType.Gpr: EmitGpr_L(gpr_Source); break;
                case RegBoundType.Cp0:
                    {
                        CoreEmit_Cop0_Read(inst.Destination);

                        if (!m_IsRuntime64)
                            UnsignedConvert(CLRValueType.UINT_32);

                        /* If running 64-bit mode, with the 32-bit version, then sign extend */
                        if (m_IsRuntime64 && inst.IsData32())
                        {
                            UnsignedConvert(CLRValueType.UINT_32);
                            EmitSignExtend64();
                        }

                        break;
                    }
                case RegBoundType.Cp1:
                    {
                        EmitFprW_L(inst.FloatSource);

                        /* If running 64-bit mode, with the 32-bit version, then sign extend */
                        if (m_IsRuntime64 && inst.IsData32())
                        {
                            EmitSignExtend64();
                        }

                        break;
                    }

                case RegBoundType.Cp1Ctl:
                    {
                        CoreEmit_Cop1Ctrl_Read();
                        break;
                    }
            }

            switch (inst.Op.XferTarget)
            {
                case RegBoundType.Gpr: EmitGpr_S(gpr_Target); break;
                case RegBoundType.Hi: EmitHi_S(); break;
                case RegBoundType.Lo: EmitLo_S(); break;
                case RegBoundType.Cp0:
                    {
                        CoreEmit_Cop0_Write_END();
                        break;
                    }
                case RegBoundType.Cp1:
                    {
                        /* Assuming that FR is enabled */
                        UnsignedConvert(CLRValueType.UINT_32);
                        EmitFprW_S(inst.FloatSource);
                        break;
                    }
                case RegBoundType.Cp1Ctl:
                    {
                        CoreEmit_Cop1Ctrl_Write_END();
                        break;
                    }
            }
        }

        public void SetOnLessThan(DecodedInstruction inst)
        {
            LogIL("Opcode SetOnLessThan");

            CLRValueType targetUnsignedType = m_IsRuntime64 ? CLRValueType.UINT_64 : CLRValueType.UINT_32;
            CLRValueType targetSignedType = m_IsRuntime64 ? CLRValueType.INT_64 : CLRValueType.INT_32;
            int destReg;

            EmitGpr_L(inst.Source);

            if (!inst.IsUnsigned())
                SignedConvert(targetSignedType);

            if (inst.IsImmediate())
            {
                if (m_IsRuntime64)
                    EmitImm64(inst);
                else
                    EmitImm32(inst);

                if (inst.IsUnsigned())
                    UnsignedConvert(targetUnsignedType);

                destReg = inst.Target;
            }
            else
            {
                EmitGpr_L(inst.Target);

                if (!inst.IsUnsigned())
                    SignedConvert(targetSignedType);

                destReg = inst.Destination;
            }

            if (inst.IsUnsigned())
                Emit(IL.Clt_Un);
            else
                Emit(IL.Clt);

            UnsignedConvert(targetUnsignedType);
            EmitGpr_S(destReg);
        }

        public void EmiReadLeftRightMask(int size, bool isLeft)
        {   
            switch (size)
            {
                default: throw new EmuException("invalid size for left/right mask");

                case 4:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LWL_MASK)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LWR_MASK)));
                        }

                        EmitAddress_L();
                        SignedConvert(CLRValueType.INT_32);

                        Emit(IL.Ldelem_U4);

                        break;
                    }


                case 8:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LDL_MASK)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LDR_MASK)));
                        }

                        EmitAddress_L();
                        SignedConvert(CLRValueType.INT_32);

                        Emit(IL.Ldelem_I8);
                        UnsignedConvert(CLRValueType.UINT_64);

                        break;
                    }
            }
        }


        public void EmitReadLeftRightShift(int size, bool isLeft)
        {
            switch (size)
            {
                default: throw new EmuException("invalid size for left/right shift");

                case 4:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LWL_SHIFT)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LWR_SHIFT)));
                        }

                        break;
                    }


                case 8:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LDL_SHIFT)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.LDR_SHIFT)));
                        }

                        break;
                    }
            }

            EmitAddress_L();
            SignedConvert(CLRValueType.INT_32);

            Emit(IL.Ldelem_I4);
        }

        public void EmitWriteLeftRightMask(int size, bool isLeft)
        {
            switch (size)
            {
                default: throw new EmuException("invalid size for left/right mask");

                case 4:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SWL_MASK)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SWR_MASK)));
                        }

                        EmitAddress_L();
                        SignedConvert(CLRValueType.INT_32);

                        Emit(IL.Ldelem_U4);

                        break;
                    }


                case 8:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SDL_MASK)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SDR_MASK)));
                        }

                        EmitAddress_L();
                        SignedConvert(CLRValueType.INT_32);

                        Emit(IL.Ldelem_I8);
                        UnsignedConvert(CLRValueType.UINT_64);

                        break;
                    }
            }
        }

        public void EmitWriteLeftRightShift(int size, bool isLeft)
        {
            switch (size)
            {
                default: throw new EmuException("invalid size for left/right shift");

                case 4:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SWL_SHIFT)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SWR_SHIFT)));
                        }

                        break;
                    }


                case 8:
                    {
                        if (isLeft)
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SDL_SHIFT)));
                        }
                        else
                        {
                            Emit(IL.Ldsfld, typeof(CTS).GetField(nameof(CTS.SDR_SHIFT)));
                        }

                        break;
                    }
            }

            EmitAddress_L();
            SignedConvert(CLRValueType.INT_32);

            Emit(IL.Ldelem_I4);
        }

        public void EmitMemoryAddress(DecodedInstruction inst)
        {
            EmitGpr_L(inst.Source);

            if (m_IsRuntime64)
            {
                EmitImm64(inst);
            }
            else
            {
                EmitImm32(inst);
            }

            Emit(IL.Add);

            if (!m_IsRuntime64) UnsignedConvert(CLRValueType.UINT_64);

            SignedConvert(CLRValueType.INT_64);
            EmitAddress_S();
        }

        public void EmitReadLeftRight(DecodedInstruction inst , int size)
        {
            bool isLeft = inst.IsLeft();

            EmitMipsCall(nameof(IDynamicMips.ReadDataMemory), () =>
            {
                EmitAddress_L();
                Constant64(size - 1);
                Emit(IL.Not);
                Emit(IL.And);
                Constant32(size);
            });

            /* Compute the table index and store it into the address local */
            EmitAddress_L();
            Constant64(size - 1);
            Emit(IL.And);
            EmitAddress_S();

            EmitMipsRef();

            /* Value */
            EmitGpr_L(inst.Target);

            if (size == 4 && m_IsRuntime64)
                UnsignedConvert(CLRValueType.UINT_32);

            /* Mask value */
            EmiReadLeftRightMask(size, isLeft);

            Emit(IL.And);

            if (size == 8)
            {
                EmitMipsCall(nameof(IDynamicMips.GetMemData64), null);
            }
            else
            {
                EmitMipsCall(nameof(IDynamicMips.GetMemData32), null);
            }

            EmitReadLeftRightShift(size, isLeft);

            if (isLeft)
                Emit(IL.Shl);
            else
                Emit(IL.Shr_Un);

            Emit(IL.Or);

            if (size == 8)
            {
                Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.SetMemData64)]);
            }
            else
            {
                Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.SetMemData32)]);
            }
        }

        public void EmitWriteLeftRight(DecodedInstruction inst, int size)
        {
            bool isLeft = inst.IsLeft();

            EmitMipsCall(nameof(IDynamicMips.ReadDataMemory), () =>
            {
                EmitAddress_L();
                Constant64(size - 1);
                Emit(IL.Not);
                Emit(IL.And);
                Constant32(size);
            });


            EmitAddress_L();
            EmitDuplicate();

            /* Compute the table index and store it into the address local */
            Constant64(size - 1);
            Emit(IL.And);
            EmitAddress_S();

            /* Mask */
            EmitMipsRef();

            /* Value */
            if (size == 8)
            {
                EmitMipsCall(nameof(IDynamicMips.GetMemData64), null);
            }
            else
            {
                EmitMipsCall(nameof(IDynamicMips.GetMemData32), null);
            }

            EmitWriteLeftRightMask(size, isLeft);
            Emit(IL.And);

            EmitGpr_L(inst.Target);

            if (size == 4 && m_IsRuntime64)
                UnsignedConvert(CLRValueType.UINT_32);

            EmitWriteLeftRightShift(size, isLeft);

            if (!isLeft)
                Emit(IL.Shl);
            else
                Emit(IL.Shr_Un);

            Emit(IL.Or);

            if (size == 8)
            {
                Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.SetMemData64)]);
            }
            else
            {
                Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.SetMemData32)]);
            }

            EmitAddress_S();

            EmitMipsCall(nameof(IDynamicMips.WriteDataMemory), () =>
            {
                EmitAddress_L();
                Constant32(size);
            });
        }

        public void Load(DecodedInstruction inst)
        {
            LogIL("Opcode Load");

            bool upperImm = inst.IsImmediate();
            bool loadLinked = inst.IsLink();
            bool left = inst.IsLeft();
            bool right = inst.IsRight();
            bool unsigned = inst.IsUnsigned();
            int size = 0;


            if (upperImm)
            {
                Constant32(inst.Immediate << 16, true);
                if (m_IsRuntime64) EmitSignExtend64();
                EmitGpr_S(inst.Target);
            }
            else
            {
                size = inst.DataSize();
                EmitMemoryAddress(inst);

                if (!m_IsRuntime64 && size == 8)
                {
                    CoreEmit_MipsException(ExceptionType.Reserved);
                    return;
                }

                if (left || right)
                {
                    EmitReadLeftRight(inst, size);
                }
                else
                {
                    // TODO: LLbit set to true only for loadlinked instructions
                    EmitMipsCall(nameof(IDynamicMips.ReadDataMemory), () =>
                    {
                        EmitAddress_L();
                        Constant32(size);
                    });
                }

                EmitMipsCall(GetMemDataName[size], null);

                if (!unsigned)
                {
                    SignedConvert(MemSignedDataType[size]);

                    if (size < 8 && m_IsRuntime64)
                    {
                        EmitSignExtend64();
                    }
                }

                if (m_IsRuntime64)
                    UnsignedConvert(CLRValueType.UINT_64);
                else
                    UnsignedConvert(CLRValueType.UINT_32);

                EmitGpr_S(inst.Target);
            }
        }

        public void Store(DecodedInstruction inst)
        {
            LogIL("Opcode Store");

            bool upperImm = inst.IsImmediate();
            bool loadLinked = inst.IsLink();
            bool left = inst.IsLeft();
            bool right = inst.IsRight();
            bool unsigned = inst.IsUnsigned();
            int size = inst.DataSize();

            EmitMemoryAddress(inst);

            if (!m_IsRuntime64 && size == 8)
            {
                CoreEmit_MipsException(ExceptionType.Reserved);
                return;
            }

            EmitMipsRef();
            EmitGpr_L(inst.Target);
            UnsignedConvert(MemUnsignedDataType[size]);
            Emit(IL.Call, m_MipsBindings[SetMemDataName[size]]);

            if (left || right)
            {
                EmitWriteLeftRight(inst, size);
            }
            else
            {
                if (loadLinked)
                {
                    throw new NotSupportedException("LL not yet handled in jit");
                }

                EmitMipsCall(nameof(IDynamicMips.WriteDataMemory), () =>
                {
                    EmitAddress_L();
                    Constant32(size);
                });
            }
        }

        public void Jump(DecodedInstruction inst)
        {
            LogIL("Opcode Jump");

            EmitMipsRef();
            EmitBlockRef();

            bool isLink = inst.IsLink();
            bool isRegister = inst.IsRegister();

            if (isLink)
            {
                if (m_IsRuntime64)
                    Constant64(inst.Address + 8, true);
                else
                    Constant32((uint)inst.Address + 8, true);

                EmitGpr_S(31);
            }

            if (!isRegister)
            {
                Constant64((inst.Inst.target << 2) | (inst.Address & 0xF0000000), true);
            }
            else
            {
                EmitGpr_L(inst.Source);
                UnsignedConvert(CLRValueType.UINT_32);
                UnsignedConvert(CLRValueType.UINT_64);
            }

            Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.SetBlockJump)]);
        }

        public void Branch(DecodedInstruction inst)
        {
            LogIL("Opcode Branch");

            bool isLikely = inst.IsLikely();
            bool isLink = inst.IsLink();
            var valtype = m_IsRuntime64 ? CLRValueType.INT_64 : CLRValueType.INT_32;
            bool isFpu = inst.Op.ArithmeticType == ArithmeticOp.TRUE || inst.Op.ArithmeticType == ArithmeticOp.FALSE;

            /* Compute and hardcode the target address */
            Constant64((long)CoreUtils.ComputeBranchPC(false, inst.Address, CoreUtils.ComputeBranchTargetOffset(inst.Immediate)));
            EmitAddress_S();

            if (isLink)
            {
                if (m_IsRuntime64)
                    Constant64(inst.Address + 8, true);
                else
                    Constant32((uint)inst.Address + 8, true);

                EmitGpr_S(31);
            }

            if (!isFpu)
            {
                EmitGpr_L(inst.Source);
                SignedConvert(valtype);

                if (inst.Op.ArithmeticType == ArithmeticOp.EQUAL || inst.Op.ArithmeticType == ArithmeticOp.NOT_EQUAL)
                    EmitGpr_L(inst.Target);
                else
                    EmitGpr_L(0);

                SignedConvert(valtype);
            }

            EmitBranchCondition(() =>
            {
                /* Branch taken */
                EmitMipsCall(nameof(IDynamicMips.SetBlockJump), () =>
                {
                    EmitBlockRef();
                    EmitAddress_L();
                    UnsignedConvert(CLRValueType.UINT_32);
                    UnsignedConvert(CLRValueType.UINT_64);
                });
            },
            () =>
            {
                /* Branch not taken */
                EmitMipsCall(nameof(IDynamicMips.ClearBlockJump), EmitBlockRef);

                if (isLikely)
                {
                    //EmitMipsCall(nameof(IDynamicMips.SetNullified), null);
                    Emit(IL.Br, m_BlockEndLabel);
                }
            },
            inst.Op.ArithmeticType);
        }

        public void Cache(DecodedInstruction inst)
        {
            EmitNop();
        }

        public void Sync(DecodedInstruction inst)
        {
            EmitNop();
        }

        public void EmitBranchCondition(Action trueAction, Action falseAction, ArithmeticOp op)
        {
            var l_true = DefineLabel();
            var l_false = DefineLabel();

            switch (op)
            {
                default: throw new ArgumentException("Unknown compare type");
                case ArithmeticOp.EQUAL:
                case ArithmeticOp.NOT_EQUAL: Emit(IL.Ceq); break;
                case ArithmeticOp.GREATER_THAN: Emit(IL.Cgt); break;
                case ArithmeticOp.LESS_THAN: Emit(IL.Clt); break;

                case ArithmeticOp.TRUE:
                    {
                        EmitMipsCall(nameof(IDynamicMips.CoprocessorFlag), () =>
                        {
                            Constant32(1);
                        });
                        break;
                    }
                case ArithmeticOp.FALSE:
                    {
                        EmitMipsCall(nameof(IDynamicMips.CoprocessorFlag), () =>
                        {
                            Constant32(0);
                        });
                        break;
                    }

                case ArithmeticOp.GREATER_THAN_OR_EQUAL:
                    {
                        Emit(IL.Clt);
                        Constant32(0);
                        Emit(IL.Ceq);
                        break;
                    }

                case ArithmeticOp.LESS_THAN_OR_EQUAL:
                    {
                        Emit(IL.Cgt);
                        Constant32(0);
                        Emit(IL.Ceq);
                        break;
                    }
            }

            if (op == ArithmeticOp.NOT_EQUAL)
            {
                Emit(IL.Brtrue, l_false);
            }
            else
            {
                Emit(IL.Brfalse, l_false);
            }

            trueAction?.Invoke();
            Emit(IL.Br, l_true);

            EmitNop();
            MarkLabel(l_false);

            falseAction?.Invoke();

            EmitNop();
            MarkLabel(l_true);
        }

        public void Add(DecodedInstruction inst)
        {
            LogIL("Opcode Add");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                EmitFprF_L(inst.FloatTarget);
                Emit(IL.Add);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                EmitFprD_L(inst.FloatTarget);
                Emit(IL.Add);
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Subtract(DecodedInstruction inst)
        {
            LogIL("Opcode Subtract");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                EmitFprF_L(inst.FloatTarget);
                Emit(IL.Sub);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                EmitFprD_L(inst.FloatTarget);
                Emit(IL.Sub);
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Multiply(DecodedInstruction inst)
        {
            LogIL("Opcode Multiply");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                EmitFprF_L(inst.FloatTarget);
                Emit(IL.Mul);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                EmitFprD_L(inst.FloatTarget);
                Emit(IL.Mul);
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Divide(DecodedInstruction inst)
        {
            LogIL("Opcode Divide");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                EmitFprF_L(inst.FloatTarget);
                Emit(IL.Div);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                EmitFprD_L(inst.FloatTarget);
                Emit(IL.Div);
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void SqrRoot(DecodedInstruction inst)
        {
            LogIL("Opcode Sqroot");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                Emit(IL.Call, typeof(Math).GetMethod(nameof(Math.Sqrt)));
                Emit(IL.Conv_R4);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                Emit(IL.Call, typeof(Math).GetMethod(nameof(Math.Sqrt)));
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Abs(DecodedInstruction inst)
        {
            LogIL("Opcode Abs");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                Emit(IL.Call, typeof(Math).GetMethod(nameof(Math.Abs), new Type[] { typeof(float) }));
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                Emit(IL.Call, typeof(Math).GetMethod(nameof(Math.Abs), new Type[] { typeof(double) }));
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Mov(DecodedInstruction inst)
        {
            LogIL("Opcode Mov");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Neg(DecodedInstruction inst)
        {
            LogIL("Opcode Neg");

            if (inst.Format == FpuValueType.FSingle)
            {
                EmitFprF_L(inst.FloatSource);
                Emit(IL.Neg);
                EmitFprF_S(inst.FloatDest);
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                EmitFprD_L(inst.FloatSource);
                Emit(IL.Neg);
                EmitFprD_S(inst.FloatDest);
            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void Condition(DecodedInstruction inst)
        {
            LogIL("Opcode Condition");

            bool flag_signaling = inst.Op.ArithmeticType == ArithmeticOp.SIGNALING;
            bool flag_equal = inst.Op.Flags.TestFlag(ExecutionFlags.CondEq);
            bool flag_less_than = inst.Op.Flags.TestFlag(ExecutionFlags.CondLT);
            bool flag_greater_than = inst.Op.Flags.TestFlag(ExecutionFlags.CondGT);
            bool flag_not = inst.Op.Flags.TestFlag(ExecutionFlags.CondNot);
            bool flag_unordered = inst.Op.Flags.TestFlag(ExecutionFlags.CondUn);
            bool flag_forced_ordered = inst.Op.Flags.TestFlag(ExecutionFlags.CondOrd);

            void EmitOperands()
            {
                if (inst.Format == FpuValueType.FSingle)
                {
                    EmitFprF_L(inst.FloatSource);
                    EmitFprF_L(inst.FloatTarget);
                }
                else
                {
                    EmitFprD_L(inst.FloatSource);
                    EmitFprD_L(inst.FloatTarget);
                }

            }

            if (inst.Format == FpuValueType.FSingle || inst.Format == FpuValueType.FDouble)
            {
                EmitMipsRef();

                Constant32(0);

                if (flag_equal)
                {
                    EmitOperands();
                    Emit(IL.Ceq);
                    Emit(IL.Or);
                }

                if (flag_less_than)
                {
                    EmitOperands();
                    Emit(IL.Clt);
                    Emit(IL.Or);
                }

                if (flag_greater_than)
                {
                    EmitOperands();
                    Emit(IL.Cgt);
                    Emit(IL.Or);
                }

                if (flag_not)
                {
                    Constant32(0);
                    Emit(IL.Ceq);
                }

                Emit(IL.Call, m_MipsBindings[nameof(IDynamicMips.SetFpuCondition)]);

            }
            else
            {
                CoreEmit_FpuException(FpuExceptionFlags.Unimplemented);
            }
        }

        public void ExceptionReturn(DecodedInstruction inst)
        {
            EmitMipsCall(nameof(IDynamicMips.ExceptionEnd), null);
        }

        public abstract void Add32(DecodedInstruction inst);
        public abstract void Add64(DecodedInstruction inst);
        public abstract void Subtract32(DecodedInstruction inst);
        public abstract void Subtract64(DecodedInstruction inst);
        public abstract void Shift32(DecodedInstruction inst);
        public abstract void Shift64(DecodedInstruction inst);
        public abstract void Divide32(DecodedInstruction inst);
        public abstract void Divide64(DecodedInstruction inst);
        public abstract void Multiply32(DecodedInstruction inst);
        public abstract void Multiply64(DecodedInstruction inst);
        public abstract void FloatLoad(DecodedInstruction inst);
        public abstract void FloatStore(DecodedInstruction inst);
        public abstract void Round(DecodedInstruction inst);
        public abstract void Truncate(DecodedInstruction inst);
        public abstract void Ceil(DecodedInstruction inst);
        public abstract void Floor(DecodedInstruction inst);
        public abstract void Convert(DecodedInstruction inst);
    }
}
