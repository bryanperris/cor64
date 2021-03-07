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
using cor64.IO;
using System.IO;
using cor64.Mips.JitCommon;
using System.Diagnostics;
using System.Threading;
using System.Security.Permissions;

/******************
 * Let the interpreter base read bytes and increment PC
 * For each inst: Record static PC address, call emitter function
 * At end of block: Push code block, then call
 * 
 * IMPORTANT NOTES:
 * :: The recompiler handles delay slots by deffering the end of block by 1 instruction
 */

namespace cor64.Mips.R4300I.JitIL
{
    public class ILRecompiler : InterpreterBaseR4300I, IDynamicMips
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly string JitLogDir = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "jitlog";
        private Interpreter m_FallbackInterpreter;
        private MipsILEmitter m_CurrentEmitter = null;
        private IMipsOpcodes m_CurrentOpEmitter = null;
        private MipsILEmitter32 m_Emitter32 = new MipsILEmitter32();
        private MipsILEmitter64 m_Emitter64 = new MipsILEmitter64();
        private RuntimeBasicBlock m_CurrentIntBlock = null; // Current interpreter block
        private Dictionary<ulong, MipsILExecutableBlock> m_CompiledProgramCache = new Dictionary<ulong, MipsILExecutableBlock>();
        private readonly Dictionary<ulong, MipsILExecutableBlock> m_CompiledInterruptCache = new Dictionary<ulong, MipsILExecutableBlock>();
        private bool m_IntBlockEnd;
        private bool m_BranchHit;
        private ulong m_JitPc;
        private bool m_JitExecute;
        private int m_CurrentInstOffset;
        private Thread m_MainThread;
        private Thread m_InterruptThread;
        private bool m_ExceptionPath = false;
        protected bool m_SkipThreading;
        private volatile bool m_ExceptionFlag = false;

        // TODO: Cache both versions 32 and 64 of blocks

        public ILRecompiler() : base(new Disassembler("o32"))
        {
            m_Emitter32.GenerateOwnerBindings(this);
            m_Emitter64.GenerateOwnerBindings(this);

            if (Directory.Exists(JitLogDir))
                DeleteDirectory(JitLogDir);

            Directory.CreateDirectory(JitLogDir);
        }

        private void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        public override string Description => "MIPS Recompiler (JitIL)";

        public void SetFallbackInterpreter(Interpreter interpreter)
        {
            m_FallbackInterpreter = interpreter;
            //interpreter.SetInstructionDebugMode(DebugInstMode.Full);
            interpreter.OverrideIStream(IMemoryStream);
            interpreter.OverrideDStream(DMemoryStream);

            // interpreter.OverrideCoreState(State);
            // TODO: Instead of replaceing the state object by referecen, the state object
            //       itself should handle it.
            throw new NotImplementedException("TODO: Implement a new way to override state in a better way");

            interpreter.OverrideCop0(Cop0);
            interpreter.BypassMMU = BypassMMU;
        }

        protected override void EntryPointHit()
        {
            base.EntryPointHit();
            m_CompiledProgramCache.Clear();
        }

        private void JitCompileBlock()
        {
            m_BranchHit = false;
            m_IntBlockEnd = false;

            m_CurrentEmitter = (IsOperation64 ? (MipsILEmitter)m_Emitter64 : m_Emitter32);
            m_CurrentOpEmitter = m_CurrentEmitter;

            MipsILExecutableBlock executableBlock = new MipsILExecutableBlock(m_CurrentIntBlock.Address, m_CurrentIntBlock.InstructionList.Count, m_CurrentIntBlock);

            var gen = executableBlock.Begin();

            m_CurrentEmitter.SetCodeGenerator(gen);

            m_CurrentEmitter.SetBlockCompilationTarget(m_CurrentIntBlock);
            m_CurrentEmitter.GenerateBlockBindings(executableBlock);

            m_CurrentEmitter.BlockBegin();

            m_CurrentInstOffset = 0;

            var blockCount = m_CurrentIntBlock.InstructionList.Count;

            /* Step through the block and compile it */
            for (int i = 0; i < blockCount; i++)
            {
                var inst = m_CurrentIntBlock.InstructionList[i];
                var call = CallTable[inst];
                m_CurrentInstOffset = i;

                if (call == null)
                {
                    throw new NotSupportedException(String.Format("Opcode {0} not supported", inst.Op.Op));
                }

                m_CurrentEmitter.InstructionBegin(inst);

                if (inst.Inst.inst == 0)
                {
                    m_CurrentEmitter.EmitNop();
                }
                else
                {
                    call(inst);
                    //EmitFallback(inst);
                }

                m_CurrentEmitter.InstructionEnd();
            }

            /* Finish and put in cache */
            m_CurrentEmitter.BlockEnd();

            /* Compile log */
            executableBlock.CompileLog = m_CurrentEmitter.GetCompileLog();

            try
            {
                executableBlock.End();
            }
            catch (Exception e)
            {
                //DumpBlockIL(executableBlock);

                if (!String.IsNullOrEmpty(executableBlock.CompileLog))
                {
                    Log.Error("\n\n~~~~~~~~ Emitted IL Log \n Block {0:X8}\n {1} ~~~~~~~~\n", executableBlock.Address, executableBlock.CompileLog);
                }

                throw e;
            }
            finally
            {
                //DumpBlockIL(executableBlock);
            }

            if (m_CompiledProgramCache.ContainsKey(executableBlock.Address))
            {
                throw new InvalidProgramException("Jit block cache already has block " + executableBlock.Address.ToString("X8"));
            }

            m_CompiledProgramCache.Add(executableBlock.Address, executableBlock);

            m_CurrentIntBlock = null;
            m_JitExecute = true;
        }

        [Conditional("DEBUG")]
        private void DumpBlockIL(MipsExecutableBlock block)
        {
            try
            {
                var log = block.CompileLog;

                if (!String.IsNullOrEmpty(log))
                {
                    String path = JitLogDir + Path.DirectorySeparatorChar + block.Address.ToString("X8") + ".txt";

                    if (File.Exists(path))
                        File.Delete(path);

                    File.WriteAllText(path, block.CompileLog);
                }
            }
            catch
            {

            }
        }

        private void JitStep()
        {
            if (m_FallbackInterpreter == null)
            {
                SetFallbackInterpreter(new Interpreter());
            }

            /* Start of a new block */
            if (m_CurrentIntBlock == null)
            {
                m_CurrentIntBlock = new RuntimeBasicBlock(this.Disassembler, PC);
                m_JitPc = PC;
            }

            /* If already hit the branch, we must be in delay slot */
            m_BranchHit = false;

            /* Fetch instruction from memory */
            var inst = Decode();

            /* Increment PC */
            PC += 4;

            /* Check for an invalid instruction */
            if (inst.IsInvalid || inst.IsNull)
                throw new EmuException("ILRecompiler encountered an invalid instruction");

            /* Check first if we hit the end of code space */
            if (inst.LastOne)
                m_IntBlockEnd = true;

            /* End block at exception returns */
            if (inst.Op == Opcodes.ERET)
                m_IntBlockEnd = true;

            /* Append instruction to interpreted block */
            m_CurrentIntBlock.Append(inst);

            /* If not at end of block, check for branch ending */
            if (!m_IntBlockEnd)
            {
                /* Check for a branch, if found, then begin ending the block */
                m_BranchHit = inst.IsBranch;
                m_IntBlockEnd = m_BranchHit;
            }

            /* Check if we just need to add the delay slot */
            if (m_IntBlockEnd)
            {
                if (m_BranchHit)
                {
                    /* Allow delay slot to be appended if possible */
                    if (!inst.LastOne)
                    {
                        return;
                    }
                }

                JitCompileBlock();
            }
        }

        private void JitBlockExecute()
        {
            var executionBlock = m_CompiledProgramCache[m_JitPc];

            try
            {
                executionBlock.Execute(this);
            }
            catch (InvalidProgramException e)
            {
                Log.Error("! JIT RUNTIME ERROR: Execute block {0:X8}", executionBlock.Address);

                if (!String.IsNullOrEmpty(executionBlock.CompileLog))
                {
                    Log.Trace(executionBlock.CompileLog);
                }

                throw e;
            }

            if (executionBlock.WillJump)
            {
                m_JitPc = (uint)executionBlock.JumpAddress;
                TargetAddress = m_JitPc;
            }
            else
            {
                m_JitPc = executionBlock.Address + ((ulong)executionBlock.Size * 4);
                TargetAddress = 0;
            }
        }

        public sealed override void Step()
        {
            if (!m_SkipThreading)
            {
                if (m_InterruptThread == null)
                {
                    m_InterruptThread = new Thread(JitInterrupt);
                    m_InterruptThread.IsBackground = true;
                    m_InterruptThread.Start();
                }


                if (m_MainThread == null)
                {
                    m_MainThread = new Thread(JitMainLoop);
                    m_MainThread.IsBackground = true;
                    m_MainThread.Start();
                }
            }
            else
            {
                JitNext();
            }
        }

        private void JitNext()
        {
            if (m_JitExecute && !m_CompiledProgramCache.ContainsKey(m_JitPc))
            {
                //Console.WriteLine("Jit Block Not Found: JitPC={0:X8} Pc={1:X8}", m_JitPc, PC);
                PC = m_JitPc;
                m_JitExecute = false;
            }

            while (!m_JitExecute)
            {
                //Console.WriteLine("Jit Step: JitPC={0:X8} Pc={1:X8}", m_JitPc, PC);
                JitStep();
            }

            //Console.WriteLine("Jit Execute: JitPC={0:X8} Pc={1:X8}", m_JitPc, PC);
            JitBlockExecute();
        }

        private void JitMainLoop()
        {
            while (true)
            {
                JitNext();
            }
        }

        public void JitInterrupt()
        {
            while (true)
            {
                Cop0.MipsTimerTick(1);

                // TODO: FIgure out improved interrupt/exception checking for the recompiler

                m_ExceptionFlag = Cop0State.Status.ErrorLevel || Cop0State.Status.ExceptionLevel;
            }
        }

        private void EmitFallback(DecodedInstruction inst)
        {
            /* Save locals back into emu state */
            m_CurrentEmitter.EmitStoreLocals();

            m_CurrentEmitter.EmitMipsCall(nameof(IDynamicMips.Fallback), () =>
            {
                m_CurrentEmitter.BlockRef();
                m_CurrentEmitter.Constant32(m_CurrentInstOffset);
            });

            m_CurrentEmitter.EmitLoadLocals();
        }

        private void CheckPc()
        {
            m_JitExecute = m_CompiledProgramCache.ContainsKey(PC);

            if (m_JitExecute)
            {
                m_JitExecute = m_CompiledProgramCache.ContainsKey(m_JitPc);
            }
        }

        public sealed override void BitwiseLogic(DecodedInstruction inst) => m_CurrentOpEmitter.BitwiseLogic(inst);

        public sealed override void Add32(DecodedInstruction inst) => m_CurrentOpEmitter.Add32(inst);

        public sealed override void Add64(DecodedInstruction inst) => m_CurrentOpEmitter.Add64(inst);

        public sealed override void Subtract32(DecodedInstruction inst) => m_CurrentOpEmitter.Subtract32(inst);

        public sealed override void Subtract64(DecodedInstruction inst) => m_CurrentOpEmitter.Subtract64(inst);

        public sealed override void Shift32(DecodedInstruction inst) => m_CurrentOpEmitter.Shift32(inst);

        public sealed override void Shift64(DecodedInstruction inst) => m_CurrentOpEmitter.Shift64(inst);

        public sealed override void Divide32(DecodedInstruction inst) => m_CurrentOpEmitter.Divide32(inst);

        public sealed override void Divide64(DecodedInstruction inst) => m_CurrentOpEmitter.Divide64(inst);

        public sealed override void Multiply32(DecodedInstruction inst) => m_CurrentOpEmitter.Multiply32(inst);

        public sealed override void Multiply64(DecodedInstruction inst) => m_CurrentOpEmitter.Multiply64(inst);

        public sealed override void SetOnLessThan(DecodedInstruction inst) => m_CurrentOpEmitter.SetOnLessThan(inst);

        // TODO: Some FPU issues with the compiled transfered reg opcode

        //public sealed override void TransferReg(DecodedInstruction inst) => m_CurrentOpEmitter.TransferReg(inst);
        public sealed override void TransferReg(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Branch(DecodedInstruction inst) => m_CurrentOpEmitter.Branch(inst);

        public sealed override void Jump(DecodedInstruction inst) => m_CurrentOpEmitter.Jump(inst);

        public sealed override void Store(DecodedInstruction inst) => m_CurrentOpEmitter.Store(inst);

        public sealed override void Load(DecodedInstruction inst) => m_CurrentOpEmitter.Load(inst);

        public sealed override void Cache(DecodedInstruction inst) => m_CurrentOpEmitter.Cache(inst);

        public sealed override void Sync(DecodedInstruction inst) => m_CurrentOpEmitter.Sync(inst);

        public sealed override void FloatLoad(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void FloatStore(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Add(DecodedInstruction inst) => m_CurrentOpEmitter.Add(inst);

        public sealed override void Subtract(DecodedInstruction inst) => m_CurrentOpEmitter.Subtract(inst);

        public sealed override void Multiply(DecodedInstruction inst) => m_CurrentOpEmitter.Multiply(inst);

        public sealed override void Divide(DecodedInstruction inst) => m_CurrentOpEmitter.Divide(inst);

        public sealed override void SqrRoot(DecodedInstruction inst) => m_CurrentOpEmitter.SqrRoot(inst);

        public sealed override void Abs(DecodedInstruction inst) => m_CurrentOpEmitter.Abs(inst);

        public sealed override void Mov(DecodedInstruction inst) => m_CurrentOpEmitter.Mov(inst);

        public sealed override void Neg(DecodedInstruction inst) => m_CurrentOpEmitter.Neg(inst);

        public sealed override void Round(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Truncate(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Ceil(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Floor(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Convert(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Condition(DecodedInstruction inst) => m_CurrentOpEmitter.Condition(inst);

        public sealed override void ExceptionReturn(DecodedInstruction inst) => m_CurrentOpEmitter.ExceptionReturn(inst);

        public sealed override void Trap(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void Break(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void TLBProbe(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void TLBRead(DecodedInstruction inst) => EmitFallback(inst);

        public sealed override void TLBWrite(DecodedInstruction inst) => EmitFallback(inst);

        public override void Syscall(DecodedInstruction inst) => EmitFallback(inst);

        uint IDynamicMips.ReadGPR32(int select)
        {
            return ReadGPR32(select);
        }

        void IDynamicMips.SetExceptionState(ExceptionType type)
        {
            SetExceptionState(type);
        }

        void IDynamicMips.WriteGPR32(int select, uint value)
        {
            WriteGPR32(select, value);
        }

        uint IDynamicMips.ReadLo32()
        {
            return (uint)ReadLo();
        }

        uint IDynamicMips.ReadHi32()
        {
            return (uint)ReadHi();
        }

        void IDynamicMips.WriteLo32(uint value)
        {
            WriteLo(value);
        }

        void IDynamicMips.WriteHi32(uint value)
        {
            WriteHi(value);
        }

        void IDynamicMips.SetBlockJump(MipsExecutableBlock block, ulong pc)
        {
            block.SetJump(pc);
            TargetAddress = pc;
            TakeBranch = true;
            BranchDelay = true;
        }

        ulong IDynamicMips.ReadGPR64(int select)
        {
            return ReadGPR64(select);
        }

        void IDynamicMips.WriteGPR64(int select, ulong value)
        {
            WriteGPR64(select, value);
        }

        ulong IDynamicMips.ReadLo64()
        {
            return ReadLo();
        }

        ulong IDynamicMips.ReadHi64()
        {
            return ReadHi();
        }

        void IDynamicMips.WriteLo64(ulong value)
        {
            WriteLo(value);
        }

        void IDynamicMips.WriteHi64(ulong value)
        {
            WriteHi(value);
        }

        ulong IDynamicMips.Cop0_Read(int reg)
        {
            return Cop0.CpuRegisterRead(reg);
        }

        void IDynamicMips.Cop0_Write(int reg, ulong val)
        {
            Cop0.CpuRegisterWrite(reg, val);
        }

        void IDynamicMips.Fallback(MipsExecutableBlock block, int instOffset)
        {
            /* Set the fallback PC to the address of the inst */
            var inst = block.Shadow.InstructionList[instOffset];
            //Console.WriteLine("JitIL Fallback Inst: " + Disassembler.GetFullDisassembly(inst));
            m_FallbackInterpreter.SafeSetPC(inst.Address);
            m_FallbackInterpreter.InjectInst(inst);
            m_FallbackInterpreter.Step();

            if (inst.IsBranch)
            {
                TargetAddress = m_FallbackInterpreter.TargetAddress;
                BranchDelay = m_FallbackInterpreter.BranchDelay;
                NullifyNext = m_FallbackInterpreter.NullifyNext;

                if (m_FallbackInterpreter.WillJump)
                {
                    block.SetJump((uint)m_FallbackInterpreter.TargetAddress);
                    m_FallbackInterpreter.ClearBranchUnit();
                }
                else
                {
                    block.ClearJump();
                    m_FallbackInterpreter.ClearBranchUnit();
                }
            }
        }

        bool IDynamicMips.GetNullified()
        {
            return NullifyNext;
        }

        void IDynamicMips.ClearNullified()
        {
            NullifyNext = false;
        }

        void IDynamicMips.ReadDataMemory(long address, int size)
        {
            m_DataMemory.ReadData((uint)address, size);
        }

        void IDynamicMips.WriteDataMemory(long address, int size)
        {
            m_DataMemory.WriteData((uint)address, size);
        }

        byte IDynamicMips.GetMemData8()
        {
            return m_DataMemory.Data8;
        }

        void IDynamicMips.SetMemData8(byte value)
        {
            m_DataMemory.Data8 = value;
        }

        ushort IDynamicMips.GetMemData16()
        {
            return m_DataMemory.Data16;
        }

        void IDynamicMips.SetMemData16(ushort value)
        {
            m_DataMemory.Data16 = value;
        }

        uint IDynamicMips.GetMemData32()
        {
            return m_DataMemory.Data32;
        }

        void IDynamicMips.SetMemData32(uint value)
        {
            m_DataMemory.Data32 = value;
        }

        ulong IDynamicMips.GetMemData64()
        {
            return m_DataMemory.Data64;
        }

        void IDynamicMips.SetMemData64(ulong value)
        {
            m_DataMemory.Data64 = value;
        }

        void IDynamicMips.ClearBlockJump(MipsExecutableBlock block)
        {
            block.ClearJump();
            TargetAddress = 0;
            TakeBranch = false;
            BranchDelay = true;
        }

        void IDynamicMips.SetNullified()
        {
            NullifyNext = true;
            BranchDelay = false;
        }

        bool IDynamicMips.CoprocessorFlag(bool compare)
        {
            var cond = State.FCR.Condition;

            if (!compare)
            {
                /* Expecting FALSE condition */
                return !cond && true;
            }
            else
            {
                /* Expecting TRUE condition */
                return cond && true;
            }
        }

        double IDynamicMips.ReadFPRDouble(int select)
        {
            return StateR4000I.FPR.ReadFloatDouble(select);
        }

        void IDynamicMips.WriteFPRDouble(int select, double value)
        {
            StateR4000I.FPR.WriteFloatDouble(select, value);
        }

        void IDynamicMips.SetFpuExceptionState(FpuExceptionFlags flags)
        {
            State.FCR.Cause |= flags;
        }

        float IDynamicMips.ReadFPRFloat(int select)
        {
            return StateR4000I.FPR.ReadFloatSingle(select);
        }

        void IDynamicMips.WriteFPRFloat(int select, float value)
        {
            StateR4000I.FPR.WriteFloatSingle(select, value);
        }

        void IDynamicMips.SetFpuCondition(bool condition)
        {
            State.FCR.Condition = condition;
        }

        void IDynamicMips.ExceptionBegin(MipsExecutableBlock block)
        {
            PC = Cop0.ExceptionHandlerAddress;
            m_JitPc = PC;

            m_Emitter32.IsExceptionPath = true;
            m_Emitter64.IsExceptionPath = true;
            m_ExceptionPath = true;

            var cache = m_CompiledProgramCache;
            m_CompiledProgramCache = m_CompiledInterruptCache;

            CheckPc();

            while (m_ExceptionPath) {
                JitNext();
            }

            m_Emitter32.IsExceptionPath = false;
            m_Emitter64.IsExceptionPath = false;
            Cop0State.Status.ErrorLevel = false;
            Cop0State.Status.ExceptionLevel = false;
            m_JitExecute = true;

            m_CompiledProgramCache = cache;
        }

        void IDynamicMips.ExceptionEnd()
        {
            m_ExceptionPath = false;
        }

        bool IDynamicMips.CheckInterrupts()
        {
            return m_ExceptionFlag;
        }

        uint IDynamicMips.Cop1_Ctrl_Read()
        {
            return State.FCR.Value;
        }

        void IDynamicMips.Cop1_Ctrl_Write(uint value)
        {
            State.FCR.Value = value;
        }
    }
}
