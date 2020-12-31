using System.Runtime.Intrinsics;
using System.Threading;
using System;
using System.Diagnostics;
using System.IO;
using cor64.IO;
using cor64.RCP;
using NLog;

/* RSP Notes:
    * VCO = Vector Not Equal and Carry Out
    * VCC = Vector Clip and Compare
    * VCE = Vector Sign Extensions
*/

namespace cor64.Mips.Rsp
{
    public class RspInterpreter : InterpreterBaseRsp
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private DecodedInstruction? m_InjectedInst;
        private DecodedInstruction m_FailedInstruction;
        public override string Description => "Rsp Interpreter";
        public DecodedInstruction FailedInstruction { get => m_FailedInstruction; set => m_FailedInstruction = value; }
        public RspVector[] m_VecRegs;
        private readonly PinnedBuffer m_VecPinnedMemory;
        private int m_DivIn = 0;
        private int m_DivOut = 0;
        private bool m_lastDoublePrecision = false;

        private bool m_InterruptOnBroke = false;

        private readonly EventWaitHandle m_CpuRspWait = new EventWaitHandle(false, EventResetMode.ManualReset);

        
        /* RSP Flag Registers */
        private readonly RspCarryFlag m_VCarry = new RspCarryFlag();      // VCO
        private readonly RspCompareFlag m_VccClip = new RspCompareFlag(); // VCC - Clip
        private readonly RspCompareFlag M_VccCompare = new RspCompareFlag(); // VCC - Compare
        private readonly RspCompareFlag m_Vce = new RspCompareFlag();  // VCE


        private const int S16_MIN = -32768;
        private const int S16_MAX = 32767;
        private const int ADDRESS_MASK = 0xFFF; // RSP has a limited address range, so we need this mask for it.

        private const ulong ADDR_IMEM_START = 0x04001000UL;
        private const ulong ADDR_IMEM_END = 0x04001FFFUL;

        public RspInterpreter() : base(new Disassembler("o32"))
        {
            m_VecRegs = new RspVector[32];
            m_VecPinnedMemory = new PinnedBuffer(m_VecRegs.Length * 16);

            for (int i = 0; i < m_VecRegs.Length; i++)
            {
                m_VecRegs[i] = new RspVector(m_VecPinnedMemory, i);
            }
        }

        public override void AttachInterface(MipsInterface rcpInterface, SPInterface iface, DPCInterface rdpInterface)
        {
            base.AttachInterface(rcpInterface, iface, rdpInterface);

            Status.Change += () =>
            {

                if (Status.IsCmdEmpty)
                    return;

                Log.Debug("SP Status Update");

                if (Status.TestCmdFlags(StatusCmdFlags.SetHalt))
                {
                    SetHaltMode(true);
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearBroke))
                {
                    Status.StatusFlags &= ~StatusFlags.Broke;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearIntterupt)) {
                    RcpInterface.ClearInterrupt(MipsInterface.INT_SP);
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetInterrupt)) {
                    RcpInterface.SetInterrupt(MipsInterface.INT_SP, true);
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetInterruptOnBreak)) {
                    m_InterruptOnBroke = true;
                    Status.StatusFlags |= StatusFlags.InterruptOnBreak;
                    // Log.Debug("Set RSP interrupt on break instruction flag");
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearInterruptOnBreak)) {
                    m_InterruptOnBroke = false;
                    Status.StatusFlags &= ~StatusFlags.InterruptOnBreak;
                    // Log.Debug("Clear RSP interrupt on break instruction flag");
                }
                
                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal0)) {
                    Status.StatusFlags |= StatusFlags.Signal0Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal0)) {
                    Status.StatusFlags &= ~StatusFlags.Signal0Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal1)) {
                    Status.StatusFlags |= StatusFlags.Signal1Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal1)) {
                    Status.StatusFlags &= ~StatusFlags.Signal1Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal2)) {
                    Status.StatusFlags |= StatusFlags.Signal2Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal2)) {
                    Status.StatusFlags &= ~StatusFlags.Signal2Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal3)) {
                    Status.StatusFlags |= StatusFlags.Signal3Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal3)) {
                    Status.StatusFlags &= ~StatusFlags.Signal3Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal4)) {
                    Status.StatusFlags |= StatusFlags.Signal4Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal4)) {
                    Status.StatusFlags &= ~StatusFlags.Signal4Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal5)) {
                    Status.StatusFlags |= StatusFlags.Signal5Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal5)) {
                    Status.StatusFlags &= ~StatusFlags.Signal5Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal6)) {
                    Status.StatusFlags |= StatusFlags.Signal6Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal6)) {
                    Status.StatusFlags &= ~StatusFlags.Signal6Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.SetSignal7)) {
                    Status.StatusFlags |= StatusFlags.Signal7Set;
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearSignal7)) {
                    Status.StatusFlags &= ~StatusFlags.Signal7Set;
                }

                /* Process this last so all flags are counted for before starting the RSP */
                if (Status.TestCmdFlags(StatusCmdFlags.ClearHalt))
                {
                    UnhaltFromCpu();
                }

                /* Clear out the written command flags */
                Status.ClearCmdFlags();
            };
        }

        public override void Init()
        {
            Status.StatusFlags = StatusFlags.Halt;
        }

        public override void Step()
        {
            if (IsHalted) {
                return;
            }

            /* Execute next instruction */
            ExecuteNextInst();
        }

        public override void ManualStart(ulong pc)
        {
            PC = pc;
            IsHalted = false;
        }

        protected void ExecuteNextInst()
        {
            /* Delay Slot Logic */
            if (WillJump)
            {
                if (BranchDelay)
                {
                    BranchDelay = false;

                    if (!ExecuteInst())
                    {
                        throw new Exception("Failed to execute instruction in delay slot");
                    }
                }

                TakeBranch = false;
                UnconditionalJump = false;

                /* Should always be a word-aligned relative PC jump */
                /* Always force 32-bit addresses */
                PC = (uint)(TargetAddress & ADDRESS_MASK);
            }
            else
            {
                /* Nornal execution path */
                if (ExecuteInst())
                {
                    PC += 4;
                }
                else
                {
                    throw new Exception(
                        String.Format("Failed to execute instruction: 0x{0:X8} 0x{1:X8} {2}", 
                            m_FailedInstruction.Address, 
                            m_FailedInstruction.Inst.inst, 
                            DisassembleFailedInst()));
                }
            }
        }

        private string DisassembleFailedInst()
        {
            try
            {
                var dis = Disassembler.GetFullDisassembly(m_FailedInstruction);

                if (String.IsNullOrEmpty(dis))
                    return "?";

                return dis;
            }
            catch
            {
                return "?";
            }
        }

        private bool ExecuteInst()
        {
            DecodedInstruction decoded;

            if (m_InjectedInst.HasValue)
            {
                decoded = m_InjectedInst.Value;
                m_InjectedInst = null;
            }
            else
            {
                decoded = Decode();
            }

            LastReadInst = decoded;

            if (ValidateInstruction(decoded))
            {
                var call = CallTable[decoded];

                if (call == null)
                {
                    throw new NotSupportedException(String.Format("Opcode {0} not supported", decoded.Op.Op));
                }
                else
                {
                    CurrentInst = decoded;

                    if (!NullifyNext)
                    {
                        //CoreDbg.TestForInstBreakpoint(decoded);

                        TraceInstruction(decoded, false);

                        call(decoded);

                        DebugInstruction(decoded);

                    }
                    else
                    {
                        NullifyNext = false;
                        TraceInstruction(decoded, true);
                    }

                    return true;
                }
            }
            else
            {
                m_FailedInstruction = decoded;
                return false;
            }
        }

        [Conditional("DEBUG")]
        private void DebugInstruction(DecodedInstruction instruction)
        {
            if (core_InstDebugMode != InstructionDebugMode.None) {
                string memNote = null;

                if (core_MemAccessNote != null) {
                    memNote = "     # " + core_MemAccessNote.ReadMeta();
                    core_MemAccessNote = null;
                }

                // if (memNote != null)
                Console.WriteLine("RSP {0:X8} {1} {2}",
                    PC,
                    Disassembler.GetFullDisassembly(instruction),
                    memNote ?? ""
                    );
            }
        }

        private void SetHaltMode(bool halted)
        {
            IsHalted = halted;

            Log.Debug("New RSP halt status: " + (halted ? "Halted" : "Not Halted"));

            if (halted)
            {
                Status.StatusFlags |= StatusFlags.Halt;
                m_CpuRspWait.Set();
            }
            else
            {
                /* 
                   -------------------------
                   IMP: Start of RSP execution
                   ------------------------- 
                */

                PC = Interface.PC;

                Log.Debug("Start RSP TasK: {0:X8}", PC);

                // Detection of IMEM address range
                // TODO: This all should be done in a translate stream layer
                if (PC >= ADDR_IMEM_START && PC <= ADDR_IMEM_END) {
                    PC = Interface.PC - ADDR_IMEM_START;
                }

                Status.StatusFlags &= ~StatusFlags.Halt;
            }
        }

        private void UnhaltFromCpu() {
            SetHaltMode(false);

            RcpInterface.ClearInterrupt(MipsInterface.INT_SP);

            // Force CPU to wait for RSP to finish 
            m_CpuRspWait.Reset();
            m_CpuRspWait.WaitOne();
        }

        public override void AttachBootManager(BootManager bootManager)
        {
            throw new NotSupportedException();
        }

        public override ushort ReadVCC() {
            return (ushort)((m_VccClip.Value << 8) | M_VccCompare.Value);
        }

        public override ushort ReadVCO() {
            return m_VCarry.RegValue;
        }

        public override void WriteVCC(ushort vcc) {
            M_VccCompare.Value = (byte)vcc;
            m_VccClip.Value = (byte)(vcc >> 8);
        }

        public override byte ReadVCE() {
            return m_Vce.Value;
        }
        private static ushort ClampS16(int value) {
            return (ushort)Math.Clamp(value, S16_MIN, S16_MAX);
        }

        private static ushort Merge(bool compare, short pass, short fail) {
            int value = fail;

            if (compare) {
                value += pass - fail;
            }

            return (ushort)(short)value;
        }

        private static bool Merge(bool compare, bool pass, bool fail) {
            unsafe {
                int p = *(byte *)&pass;
                int f = *(byte *)&fail;

                if (compare) {
                    f += p - f;
                }

                return f == 1;
            }
        }

       /* ----------------------------------------------------------------
        * Mips Instruction Core
        * ----------------------------------------------------------------
        */

        public override void Add(DecodedInstruction inst)
        {
            bool isUnsigned = inst.IsUnsigned();
            bool isImmediate = inst.IsImmediate();
            uint operandA = ReadGPR(inst.Source);
            uint operandB = isImmediate ? (uint)(short)inst.Immediate : ReadGPR(inst.Target);
            uint result;
            int dest = isImmediate ? inst.Target : inst.Destination;

            if (isUnsigned)
            {
                unchecked
                {
                    result = operandA + operandB;
                }
            }
            else
            {
                int r = 0;
                int _a = (int)operandA;
                int _b = (int)operandB;

                unchecked
                {
                    r = _a + _b;
                }

                result = (uint)r;
            }

            Writeback(dest, result);
        }

        public override void BitwiseLogic(DecodedInstruction inst)
        {
            bool isImmediate = inst.IsImmediate();
            uint a = ReadGPR(inst.Source);
            uint b = isImmediate ? inst.Immediate : ReadGPR(inst.Target);

            uint result;

            result = inst.Op.ArithmeticType switch
            {
                ArithmeticOp.AND => a & b,
                ArithmeticOp.OR => a | b,
                ArithmeticOp.XOR => a ^ b,
                ArithmeticOp.NOR => ~(a | b),
                _ => throw new InvalidOperationException("Bitwise logic"),
            };

            Writeback((isImmediate ? inst.Target : inst.Destination), result);
        }

        public override void Branch(DecodedInstruction inst)
        {
            bool isLink = inst.IsLink();
            uint source = ReadGPR(inst.Source);
            uint target = ReadGPR(inst.Target);

            TargetAddress = CoreUtils.ComputeBranchPC(false, PC, CoreUtils.ComputeBranchTargetOffset(inst.Immediate));
            TakeBranch = ComputeBranchCondition(false, source, target, inst.Op.XferTarget, inst.Op.ArithmeticType);

            TargetAddress &= ADDRESS_MASK;

            if (isLink)
            {
                Writeback(31, (uint)PC + 8);
            }

            // Always true for RSP
            BranchDelay = true;

            if (!TakeBranch) {
                TargetAddress = 0;
            }
        }

        public override void Jump(DecodedInstruction inst)
        {
            bool isLink = inst.IsLink();
            bool isRegister = inst.IsRegister();
            BranchDelay = true;
            UnconditionalJump = true;

            TargetAddress = CoreUtils.ComputeTargetPC(isRegister, PC, ReadGPR(inst.Source), inst.Inst.target);
            TargetAddress &= ADDRESS_MASK;

            if (isLink) {
                if (isRegister && (inst.Target & 0b11) != 0) {
                    Writeback(inst.Target, (uint)PC + 8);
                }
                else {
                    Writeback(31, (uint)PC + 8);
                }
            }
        }

        public override void Load(DecodedInstruction inst)
        {
            ulong address;
            int size;

            bool upperImm = inst.IsImmediate();
            bool unsigned = inst.IsUnsigned();

            if (upperImm)
            {
                uint imm = inst.Immediate;
                imm <<= 16;

                Writeback(inst.Target, imm);
            }
            else
            {
                size = inst.DataSize();

                long baseAddress = (long)ReadGPR(inst.Source);
                long offset = (short)inst.Immediate;
                address = (ulong)(baseAddress + offset);
                address &= ADDRESS_MASK;

                DMem.ReadData((long)address, size);

                if (unsigned)
                {
                    switch (size)
                    {
                        default: throw new InvalidOperationException("How did this happen (unsigned)?");
                        case 1: Writeback(inst.Target, DMem.Data8); break;
                        case 2: Writeback(inst.Target, DMem.Data16); break;
                        case 4: Writeback(inst.Target, DMem.Data32); break;
                    }
                }
                else
                {
                    switch (size)
                    {
                        default: throw new InvalidOperationException("How did this happen?");
                        case 1: Writeback(inst.Target, (uint)(sbyte)DMem.Data8); break;
                        case 2: Writeback(inst.Target, (uint)(short)DMem.Data16); break;
                        case 4: Writeback(inst.Target, (uint)(int)DMem.Data32); break;
                    }
                }
            }
        }

        public override void SetOnLessThan(DecodedInstruction inst)
        {
            bool unsigned = inst.IsUnsigned();
            bool immediate = inst.IsImmediate();
            int dest = immediate ? inst.Target : inst.Destination;
            byte result = 0;

            uint operandA = ReadGPR(inst.Source);
            uint operandB = 0;

            if (immediate)
            {
                operandB = (uint)(short)inst.Immediate;
            }
            else
            {
                operandB = ReadGPR(inst.Target);
            }

            if (unsigned)
            {
                if (operandA < operandB)
                {
                    result = 1;
                }
            }
            else
            {
                if ((int)operandA < (int)operandB)
                {
                    result = 1;
                }
            }

            Writeback(dest, result);
        }

        public override void Shift(DecodedInstruction inst)
        {
            int shiftAmount = 0;

            if (inst.IsVariableShift())
            {
                shiftAmount = (int)(ReadGPR(inst.Source) & 0x3F);
            }
            else
            {
                shiftAmount = inst.ShiftAmount;
            }

            uint value = ReadGPR(inst.Target);

            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                value <<= shiftAmount;
            }
            else
            {
                bool sign = ((value >> 31) == 1);

                value >>= shiftAmount;

                if (sign && !inst.IsUnsigned())
                {
                    /* Sign extend */
                    value |= ~(~0U >> shiftAmount);
                }
            }

            Writeback(inst.Destination, value);
        }

        public override void Store(DecodedInstruction inst)
        {
            ulong address;
            int size = inst.DataSize();
            int baseAddress = (int)ReadGPR(inst.Source);
            int offset = (short)inst.Immediate;
            address = (ulong)(baseAddress + offset);
            address &= ADDRESS_MASK;

            switch (size)
            {
                default: throw new InvalidOperationException("How did this happen?");
                case 1: DMem.Data8 = (byte)ReadGPR(inst.Target); break;
                case 2: DMem.Data16 = (ushort)ReadGPR(inst.Target); break;
                case 4: DMem.Data32 = (uint)ReadGPR(inst.Target); break;
            }

            DMem.WriteData((long)address, size);
        }

        public override void Subtract(DecodedInstruction inst)
        {
            bool isUnsigned = inst.IsUnsigned();
            uint operandA = ReadGPR(inst.Source);
            uint operandB = ReadGPR(inst.Target);
            uint result;

            if (isUnsigned)
            {
                unchecked
                {
                    result = operandA - operandB;
                }
            }
            else
            {
                int r = 0;
                int _a = (int)operandA;
                int _b = (int)operandB;

                unchecked
                {
                    r = _a - _b;
                }


                result = (uint)r;
            }

            Writeback(inst.Destination, result);
        }

        public override void TransferReg(DecodedInstruction inst)
        {
            uint value = 0;

            RegTransferGprHelper(inst, out int regSource, out int regDest);

            /* Source value to copy */
            switch (inst.Op.XferSource)
            {
                case RegBoundType.Gpr:
                    {
                        value = ReadGPR(regSource); break;
                    }

                case RegBoundType.Cp0:
                    {
                        if (regSource >= 0 && regSource < 8)
                    	{
                            /* Read from SP Interface Regs */

                            // Log.Debug("SP Interface -> RSP: {0} {1:X8}", regSource, value);
                            value = Interface.ReadRegForRsp(regSource);

                            if (regSource == 7) {
                                // This matters more when RSP/CPU bth run in parallel and trying to share the DMA
                                //Log.Debug("RSP read semaphore");

                                // value = 1;
                                // m_CpuRspWait.Set();
                            }
                    	}
                    	else if (regSource >= 8 && regSource < 16)
                    	{
                            var select = regSource - 8;

                            //Log.Debug("RDP -> RSP Command: {0} {1:X8}", select, value);
                            value = RdpInterface.ReadRegForRsp(select);
                    	}
                    	else
                    	{
                    		Log.Error("Unknown RSP COP0 Read: " + regSource.ToString());
                    	}

                        break;
                    }

                case RegBoundType.Cp2:
                    {
                        value = m_VecRegs[inst.VSource].PackedU16(inst.Element); break;
                    }

                case RegBoundType.Cp2Ctl:
                    {
                        switch (inst.VSource) {
                            case 0: {
                                value = ReadVCO();
                                break;
                            }

                            case 1: {
                                value = ReadVCC();
                                break;
                            }

                            case 2: {
                                value = ReadVCE();
                                break;
                            }

                            default: Console.WriteLine("FIXME: Read a vector control reg into GPR: " + inst.VSource.ToString()); break;
                        }

                        break;
                    }

                default: throw new NotSupportedException();
            }

            /* Target */
            switch (inst.Op.XferTarget)
            {
                case RegBoundType.Gpr:
                    {
                        Writeback(regDest, value);
                        break;
                    }

                case RegBoundType.Cp0:
                    {
                        if (regDest >= 0 && regDest < 8)
                    	{
                    		/* Write to SP Interface Regs */

                            // Log.Debug("RSP -> SP Interface: {0} {1:X8}", regDest, value);
                            Interface.RegWriteFromRsp(regDest, value);
                    	}
                    	else if (regDest >= 8 && regDest < 16)
                    	{
                            /* Write to RDP Command Regs */

                            var select = regDest - 8;

                            //Log.Debug("RSP -> RDP Command: {0} {1:X8}", select, value);
                            RdpInterface.RegWriteFromRsp(select, value);
                    	}
                    	else
                    	{
                    		Log.Error("Unknown RSP COP0 Write: " + regDest.ToString());
                    	}

                        break;
                    }

                case RegBoundType.Cp2:
                    {
                         m_VecRegs[inst.VSource].PackedU16(inst.Element, (ushort)value);
                         break;
                    }

                case RegBoundType.Cp2Ctl:
                    {
                        switch (inst.VSource) {
                            case 0: {
                                m_VCarry.RegValue = (ushort)value;
                                break;
                            }

                            case 1: {
                                WriteVCC((ushort)value);
                                break;
                            }

                            case 3: {
                                m_Vce.Value = (byte)value;
                                break;
                            }

                            default: Console.WriteLine("FIXME: Write GPR into vector ctrl: " + inst.VSource.ToString()); break;
                        }

                        break;
                    }

                default: throw new NotSupportedException();
            }
        }

        public override void Break(DecodedInstruction inst)
        {
            /* Break is used to halt the RSP processor */
            Status.StatusFlags |= StatusFlags.Broke;

            if (m_InterruptOnBroke) {
                RcpInterface.SetInterrupt(MipsInterface.INT_SP, true);
                Log.Debug("RSP Interrupt has been set on break");
            }

            SetHaltMode(true);
        }

       /* ----------------------------------------------------------------
        * Mips Vector Unit Instruction Core
        * ----------------------------------------------------------------
        */

        public override void VectorUnitReserved(DecodedInstruction inst)
        {
            throw new MipsException(ExceptionType.Reserved);
        }

        public override void VectorLoad(DecodedInstruction inst)
        {
            var executeFlag = inst.Op.Flags;
            var vectorFlag  = inst.Op.VectorFlags;
            var target =  m_VecRegs[inst.Target];
            var element = inst.Inst.lsde;
            var baseAddress = ReadGPR(inst.Source);
            var offset = inst.Offset;
            long address;

            if (executeFlag != ExecutionFlags.None) {
                switch (executeFlag) {
                    case ExecutionFlags.Data8: {
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadByteIntoVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.Data16: {
                        offset <<= 1;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadU16IntoVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.Data32: {
                        offset <<= 2;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadU32IntoVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.DataD: {
                        offset <<= 3;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadDoubleIntoVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.Data128: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadU128IntoVector(DMem, address, target);
                        break;
                    }

                    case ExecutionFlags.Unsigned: {
                        offset <<= 3;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadPacked_Unsigned(DMem, address, target);
                        break;
                    }

                    default: throw new EmuException("Invalid RSP VectorLoad Execution Flag");
                }
            }

            if (vectorFlag != VectorOpFlags.None) {
                switch (vectorFlag) {
                    case VectorOpFlags.Forth: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadPacked_Forth(DMem, address, target, element);
                        break;
                    }

                    case VectorOpFlags.Half: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadPacked_Half(DMem, address, target); 
                        break;
                    }

                    case VectorOpFlags.Upper: {
                        offset <<= 3;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadPacked_Upper(DMem, address, target);
                        break;
                    }
                    case VectorOpFlags.Wrap: {
                        throw new NotSupportedException("LWV is an undocumented opcode");
                    }

                    case VectorOpFlags.Transpose: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.LoadTranposed(DMem, address, m_VecRegs, inst.VTarget, element);
                        break;
                    }
                    default: throw new EmuException("Invalid RSP VectorLoad Vector Flag");
                }
            }
        }

        public override void VectorStore(DecodedInstruction inst)
        {
            var executeFlag = inst.Op.Flags;
            var vectorFlag  = inst.Op.VectorFlags;
            var target =  m_VecRegs[inst.Target];
            var element = inst.Inst.lsde;
            var baseAddress = ReadGPR(inst.Source);
            var offset = inst.Offset;
            long address;

            if (executeFlag != ExecutionFlags.None) {
                switch (executeFlag) {
                    case ExecutionFlags.Data8: {
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StoreByteFromVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.Data16: {
                        offset <<= 1;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StoreU16FromVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.Data32: {
                        offset <<= 2;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StoreU32FromVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.DataD: {
                        offset <<= 3;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StoreDoubleFromVector(DMem, address, target, element);
                        break;
                    }

                    case ExecutionFlags.Data128: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StoreU128FromVector(DMem, address, target);
                        break;
                    }

                    case ExecutionFlags.Unsigned: {
                        offset <<= 3;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StorePacked_Unsigned(DMem, address, target);
                        break;
                    }

                    default: throw new EmuException("Invalid RSP VectorStore Execution Flag");
                }
            }

            if (vectorFlag != VectorOpFlags.None) {
                switch (vectorFlag) {
                    case VectorOpFlags.Forth: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StorePacked_Forth(DMem, address, target, element);
                        break;
                    }

                    case VectorOpFlags.Half: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StorePacked_Half(DMem, address, target); 
                        break;
                    }

                    case VectorOpFlags.Upper: {
                        offset <<= 3;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StorePacked_Upper(DMem, address, target);
                        break;
                    }
                    case VectorOpFlags.Wrap: {
                        throw new NotSupportedException("SWV is an undocumented opcode");
                    }

                    case VectorOpFlags.Transpose: {
                        offset <<= 4;
                        address = VectorLoadStoreHelper.ComputeAlignedVectorAddress(baseAddress, offset);
                        VectorLoadStoreHelper.StoreTranposed(DMem, address, m_VecRegs, inst.VTarget, element);
                        break;
                    }
                    default: throw new EmuException("Invalid RSP VectorStore Vector Flag");
                }
            }
        }

        public override void VectorAdd(DecodedInstruction inst) {
            var carry = (inst.Op.Flags & ExecutionFlags.Carry) == ExecutionFlags.Carry;
            var vt = m_VecRegs[inst.VTarget].Resolve(inst.Element);

            for (int i = 0; i < 8; i++) {
                if (!carry) {
                    int result = (short)m_VecRegs[inst.VSource].PackedU16(i) + (short)vt.PackedU16(i) + m_VCarry.GetCarryBit(i);
                    m_VecRegs[inst.VDest].PackedU16(i, ClampS16(result));
                    Acc[i] = (ushort)result;
                }
                else {
                    uint result = (uint)m_VecRegs[inst.VSource].PackedU16(i) + (uint)vt.PackedU16(i);
                    m_VecRegs[inst.VDest].PackedU16(i, (ushort)result);
                    m_VCarry.SetNotEqualBit(i, 0);
                    m_VCarry.SetCarryBit(i, (byte)(result >> 16));
                    Acc[i] = (ushort)result;
                }
            }

            if (!carry) {
                m_VCarry.ClearCarryBits();
            }
        }

        public override void VectorSubtract(DecodedInstruction inst) {
            var carry = (inst.Op.Flags & ExecutionFlags.Carry) == ExecutionFlags.Carry;
            var vt = m_VecRegs[inst.VTarget].Resolve(inst.Element);

            if (carry) {
                m_VCarry.Clear();
            }

            for (int i = 0; i < 8; i++) {
                if (!carry) {
                    int result = (short)m_VecRegs[inst.VSource].PackedU16(i) - (short)vt.PackedU16(i) - m_VCarry.GetCarryBit(i);
                    m_VecRegs[inst.VDest].PackedU16(i, ClampS16(result));
                    Acc[i] = (ushort)result;
                }
                else {
                    uint result = (uint)m_VecRegs[inst.VSource].PackedU16(i) - vt.PackedU16(i);
                    m_VecRegs[inst.VDest].PackedU16(i, (ushort)result);
                    Acc[i] = (ushort)result;

                    var signBit = (1 & (result >> 16)) != 0;

                    if (result == 0) {
                        m_VCarry.SetCarryBit(i, 0);
                        m_VCarry.SetNotEqualBit(i, 0);
                    }
                    else {
                        if (signBit) {
                            /* result < 0 */
                            m_VCarry.SetCarryBit(i, 1);
                            m_VCarry.SetNotEqualBit(i, 1);
                        }
                        else {
                            /* result > 0 */
                            m_VCarry.SetCarryBit(i, 0);
                            m_VCarry.SetNotEqualBit(i, 1);
                        }
                    }
                }
            }

            if (!carry) {
                m_VCarry.Clear();
            }
        }

        public override void VectorMultiply(DecodedInstruction inst) {
            var unsigned = inst.IsUnsigned();
            var acc = (inst.Op.VectorFlags & VectorOpFlags.Accumulate) == VectorOpFlags.Accumulate;
            var lp = (inst.Op.VectorFlags & VectorOpFlags.PartialL) == VectorOpFlags.PartialL;
            var mp = (inst.Op.VectorFlags & VectorOpFlags.PartialM) == VectorOpFlags.PartialM;
            var np = (inst.Op.VectorFlags & VectorOpFlags.PartialN) == VectorOpFlags.PartialN;
            var hp = (inst.Op.VectorFlags & VectorOpFlags.PartialH) == VectorOpFlags.PartialH;
            var vt = m_VecRegs[inst.VTarget].Resolve(inst.Element);
            var vs = m_VecRegs[inst.VSource];
            var vd = m_VecRegs[inst.VDest];

            for (int i = 0; i < 8; i++)
            {
                ulong product;

                // Multiply fractions (with/without scalars)
                if (mp) product      = (ulong)(vs.PackedS16(i) * vt.PackedU16(i));
                else if (np) product = (ulong)(vs.PackedU16(i) * vt.PackedS16(i));
                else if (lp) product = (ulong)(vs.PackedU16(i) * vt.PackedU16(i));
                else product         = (ulong)(vs.PackedS16(i) * vt.PackedS16(i));

                // Product shifting and rounding
                if (!mp && !np)
                {
                    if (lp)
                    {
                        product >>= 16;
                        product &= 0xFFFF;
                    }
                    else if (hp)
                    {
                        product <<= 16;
                    }
                    else
                    {
                        product <<= 1;
                        if (!acc) product += 0x8000; // Product Rounding
                    }
                }

                // Product accumulation
                if (acc)
                    product += Acc[i];

                Acc[i] = product;

                // Write accumulator into writeback vector register with clamping
                if (lp)
                {
                    vd.StoreProductClampedSignedAL(Acc, i);
                }
                else if (np)
                {
                    vd.PackedU16(i, Acc.Lo(i));
                }
                else
                {
                    if (unsigned)
                    {
                        vd.StoreProductClampedUnsigned(Acc, i);
                    }
                    else
                    {
                        vd.StoreProductClampedSignedAM(Acc, i);
                    }
                }
            }
        }

        public override void VectorAccumulatorRead(DecodedInstruction inst) {
            int e = inst.Element & 3;

            for (int i = 0; i < 8; i++) {
                if (e == 0) {
                    m_VecRegs[inst.VDest].PackedU16(i, Acc.Hi(i));
                    Acc.Hi(i, m_VecRegs[inst.VSource].PackedU16(i));
                }
                else if (e == 1) {
                    m_VecRegs[inst.VDest].PackedU16(i, Acc.Mi(i));
                    Acc.Mi(i, m_VecRegs[inst.VSource].PackedU16(i));
                }
                else if (e == 2) {
                    m_VecRegs[inst.VDest].PackedU16(i, Acc.Lo(i));
                    Acc.Lo(i, m_VecRegs[inst.VSource].PackedU16(i));
                }
                else {
                    Console.WriteLine("VSAR element = " + inst.Element.ToString() + " ?");
                }
            }
        }

        public override void VectorBitwise(DecodedInstruction inst) {
            for (int i = 0; i < 8; i++) {
                var source = (int) m_VecRegs[inst.VSource].PackedU16(i);
                var target = (int) m_VecRegs[inst.VTarget].Resolve(inst.Element).PackedU16(i);
                int result;

                switch (inst.Op.ArithmeticType) {
                    default: throw new EmuException("vector bitwise op not supported: " + inst.Op.ArithmeticType.ToString());
                    case ArithmeticOp.AND:
                    case ArithmeticOp.NAND: {
                        result = source & target;
                        break;
                    }

                    case ArithmeticOp.OR:
                    case ArithmeticOp.NOR: {
                        result = source | target;
                        break;
                    }

                    case ArithmeticOp.XOR:
                    case ArithmeticOp.NXOR: {
                        result = source ^ target;
                        break;
                    }

                    case ArithmeticOp.MOVE: {
                        result = target;
                        m_VecRegs[inst.VDest].PackedU16(inst.VSource & 7, m_VecRegs[inst.VTarget].PackedU16(inst.VSource & 7));
                        break;
                    }

                    case ArithmeticOp.ABSOLUTE: {
                        var s = (short)source;
                        var t = (short)target;
                        int r = 0;

                        if (s < 0) {
                            r = -t;
                        }
                        else if (s == 0) {
                            r = 0;
                        }
                        else if (s > 0) {
                            r = t;
                        }

                        result = (short)r;

                        break;
                    }
                }

                if (inst.Op.ArithmeticType == ArithmeticOp.NAND || inst.Op.ArithmeticType == ArithmeticOp.NOR || inst.Op.ArithmeticType == ArithmeticOp.NXOR) {
                    result = ~result;
                }

                ushort value = (ushort)result;
                Acc[i] = value;

                if (inst.Op.ArithmeticType != ArithmeticOp.MOVE)
                    m_VecRegs[inst.VDest].PackedU16(i, value);
            }
        }

        public override void VectorReciprocal(DecodedInstruction inst) {
            var target = m_VecRegs[inst.VTarget];
            var dest = m_VecRegs[inst.VDest];
            var useSqrt = (inst.Op.VectorFlags & VectorOpFlags.SquareRoot) == VectorOpFlags.SquareRoot;
            var high = (inst.Op.VectorFlags & VectorOpFlags.PartialH) == VectorOpFlags.PartialH;
            var low = (inst.Op.VectorFlags & VectorOpFlags.PartialL) == VectorOpFlags.PartialL;
            var doublePrecision = high || m_lastDoublePrecision;

            int shift = high ? 16 : 0;

            if (low && m_lastDoublePrecision) {
                m_DivIn |= (int) (target.PackedS16(inst.Element) & 0xFFFF);
            }
            else {
                m_DivIn = ((int)target.PackedS16(inst.Element)) << shift;
            }

            if (!high) {
                int _data = m_DivIn;
                int _address;
                int _shift;

                if (!doublePrecision)
                {
                    _data = (_data < 0) ? -_data : _data;
                }

                if (doublePrecision && _data < 0)
                {
                    _data = (_data >= -32768) ? -_data : ~_data;
                }

                /*
                 * Note, from the code just above, that data cannot be negative.
                 * (data >= 0) is unconditionally forced by the above algorithm.
                 */

                _address = _data;

                if (_data == 0)
                {
                    _shift = !doublePrecision ? 16 : 0;
                    _address <<= _shift;
                }
                else
                {
                    _shift = 0;

                    while (_address >= 0)
                    {
                        _address <<= 1;
                        _shift++;
                    }
                }

                _address = (_address >> 22) & 0x1FF;

                if (useSqrt)
                {
                    _address &= 0x1FE;
                    _address |= 0x200 | (_shift & 1);
                }

                /* flipping shift direction from left- to right- */
                _shift ^= 31;

                if (useSqrt)
                    _shift >>= 1;

                m_DivOut = (int) ((0x40000000U | ((uint)DivideTable.ReadValue(_address) << 14)) >> _shift);

                if (m_DivIn == 0) {
                    /* corner case:  overflow via division by zero */
                    m_DivOut = 0x7FFFFFFF;
                }
                else if (m_DivIn == -32768) {
                    /* corner case:  signed underflow barrier */
                    m_DivOut = -0x00010000;
                }
                else {
                    m_DivOut ^= (m_DivIn < 0) ? ~0 : 0;
                }
            }

            Acc.SetLoFromVector(target);

            dest.PackedS16(inst.DestElement & 7, (short)(m_DivOut >> shift));

            m_lastDoublePrecision = high;
        }

        public override void VectorCompare(DecodedInstruction inst) {
            var source = m_VecRegs[inst.VSource];
            var target = m_VecRegs[inst.VTarget];
            var dest   = m_VecRegs[inst.VDest];

            /* Discard the previous data */
            M_VccCompare.Clear();

            for (int i = 0; i < 8; i++) {
                var source_value = source.PackedS16(i);
                var target_value = target.PackedS16(i);
                var equal = source_value == target_value;

                switch (inst.Op.ArithmeticType) {
                    default: throw new EmuException("Invalid arithmetic flag for vector compare");

                    case ArithmeticOp.EQUAL: {
                        M_VccCompare.SetBool(i, equal);
                        M_VccCompare[i] &= (byte)(m_VCarry.GetNotEqualBit(i) ^ 1);
                        Acc.Lo(i, target.PackedU16(i));
                        break;
                    }

                    case ArithmeticOp.NOT_EQUAL: {
                        M_VccCompare.SetBool(i, !equal);
                        M_VccCompare[i] |= m_VCarry.GetNotEqualBit(i);
                        Acc.Lo(i, source.PackedU16(i));
                        break;
                    }

                    case ArithmeticOp.GREATER_THAN_OR_EQUAL: {
                        equal &= !(m_VCarry.GetNotEqualBool(i) && m_VCarry.GetCarryBitBool(i));
                        M_VccCompare.SetBool(i, (source_value > target_value) || equal);
                        Acc.Lo(i, Merge(M_VccCompare.GetBool(i), source.PackedS16(i), target.PackedS16(i)));
                        break;
                    }

                    case ArithmeticOp.LESS_THAN_OR_EQUAL: {
                        equal &= (m_VCarry.GetNotEqualBool(i) && m_VCarry.GetCarryBitBool(i));
                        M_VccCompare.SetBool(i, (source_value < target_value) || equal);
                        Acc.Lo(i, Merge(M_VccCompare.GetBool(i), source.PackedS16(i), target.PackedS16(i)));
                        break;
                    }
                }

                /* Copy ACC_L into DST */
                dest.PackedU16(i, Acc.Lo(i));
            }

            /* Zero out flags */
            m_VCarry.Clear();
            m_VccClip.Clear();
        }

        public override void VectorClip(DecodedInstruction inst) {
            var source = m_VecRegs[inst.VSource].Resolve(inst.Element);
            var target = m_VecRegs[inst.VTarget].Resolve(inst.Element);
            var dest   = m_VecRegs[inst.VDest];

            bool isMerge = (inst.Op.VectorFlags & VectorOpFlags.Merge) == VectorOpFlags.Merge;

            bool flag_compare = false;
            bool flag_clip = false;
            bool compare;
            short pass;
            short fail;

            for (int i = 0; i < 8; i++) {
                var target_slice = target.PackedU16(i);

                var source_u16 = source.PackedU16(i);
                var source_s16 = source.PackedS16(i);
                var target_u16 = target.PackedU16(i);
                var target_s16 = target.PackedS16(i);

                var carry = m_VCarry.GetCarryBitBool(i);

                switch (inst.Op.VectorFlags) {
                    case VectorOpFlags.PartialL: {
                        var equal = !m_VCarry.GetNotEqualBool(i);
                        var is_signed = carry;

                        /* Apply the carry */
                        if (is_signed) {
                            target_slice ^= 0xFFFF;
                            target_slice++;
                        }
                        
                        var sign = ((source_u16 + (target.PackedU16(i) - 65535)) >> 31) == 1;
                        var match = (source_u16 - target_slice) == 0;

                        bool sign_ext = m_Vce.GetBool(i);

                        var _clip = (match || sign) &&  sign_ext;
                        var _compare = (match && sign  && !sign_ext) || _clip;

                        flag_compare = Merge(equal && is_signed, _compare, M_VccCompare.GetBool(i));
                        flag_clip =    Merge(equal && !is_signed, source_u16 >=target_slice, m_VccClip.GetBool(i));

                        pass = (short)target_slice;
                        fail = source_s16;
                        compare =  Merge(carry, flag_compare, flag_clip);

                        m_VCarry.SetCarryBit(i, 0);
                        m_Vce.SetBool(i, false);

                        break;
                    }

                    case VectorOpFlags.PartialH: {
                        bool cch = target_s16 == S16_MIN; /* Corner Case Hack */
                        bool is_signed = (short)(source_u16 ^ target_u16) < 0;
                        bool is_equal = false;

                        m_VCarry.SetCarryBit(i, is_signed);

                        if (is_signed) {
                            target_slice ^= 0xFFFF;

                            is_equal = source_u16 == target_slice;
                            m_Vce.SetBool(i, is_equal);
                            
                            if (!cch) {
                                target_slice++;
                                is_equal |= source_u16 == target_slice;
                            }
                        }
                        else {
                            if (!cch) {
                                is_equal = source_u16 == target_slice;
                                m_Vce.SetBool(i, false);
                            }
                        }

                        m_VCarry.SetNotEqualBit(i, !is_equal);

                        flag_clip = (short)(is_signed ? 0xFFFF : source_u16) >= target_s16;

                        var is_diff = ((short)target_slice - source_s16) >= 0;

                        flag_compare = target.PackedS16(i) < 0;
                        flag_compare = Merge(is_signed, is_diff, flag_compare);
                        
                        compare = Merge(is_signed, flag_compare, flag_clip);

                        pass = (short) target_slice;
                        fail = source.PackedS16(i);

                        break;
                    }

                    case VectorOpFlags.Crimp: {
                        var is_signed = (short)(source_u16 ^ target_u16) < 0;

                        flag_compare = is_signed ? target_s16 <= ~source_s16 : target_s16 <= ~0;
                        flag_clip = is_signed ? ~0 >= target_s16 : source_s16 >= target_s16;

                        if (is_signed) {
                            target_slice ^= 0xFFFF;
                        }

                        pass = (short) target_slice;
                        fail = source_s16;
                        compare = Merge(is_signed, flag_compare, flag_clip);

                        m_VCarry.SetCarryBit(i, 0);
                        m_Vce.SetBool(i, false);

                        break;
                    }

                    case VectorOpFlags.Merge: {
                        pass = source_s16;
                        fail = target_s16;
                        compare = M_VccCompare.GetBool(i);
                        break;
                    }

                    default: throw new EmuException("Invalid RSP clipping mode");
                }

                Acc.Lo(i, Merge(compare, pass, fail));
                dest.PackedU16(i, Acc.Lo(i));

                if (!isMerge) {
                    m_VccClip.SetBool(i, flag_clip);
                    M_VccCompare.SetBool(i, flag_compare);
                }
            }
        }
    }
}