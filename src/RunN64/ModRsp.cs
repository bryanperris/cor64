using System.Net;
using System;
using cor64;
using cor64.Mips;
using cor64.Mips.Rsp;
using RunN64.External.Rsp;
using M = RunN64.External.Rsp.module;
using R = RunN64.External.Rsp.rsp;
using System.IO;
using cor64.RCP;
using NLog;

namespace RunN64 {
    public sealed class ModRsp : InterpreterBaseRsp
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ExecutionState m_State = new ExecutionState();
        private readonly RSP_INFO m_RspInfo = new RSP_INFO();

        private bool m_InterruptOnBroke = false;

        private const uint PC_ALIGN_MASK = 0xFFC;
        private const uint DATA_ALIGN_MASK = 0xFFF;

        private readonly InstDebugFunc m_Callback_InstDebug;
        private readonly PFunc m_Callback_CheckInterrupts;
        private readonly PFunc m_Callback_ProcessRdpList;
        private readonly PFunc m_Callback_ProcessAList;
        private readonly PFunc m_Callback_ProcessDList;
        private readonly PFunc m_Callback_ShowCFB;


        public override string Description => "Iconoclast RSP Interpreter";

        public ModRsp() : base(new Disassembler("o32"))
        {
            // R.PluginLoaded();
            m_Callback_InstDebug = new InstDebugFunc(DebugInst);
            m_Callback_CheckInterrupts = new PFunc(CheckInterrupts);
            m_Callback_ProcessAList = new PFunc(ProcessAList);
            m_Callback_ProcessDList = new PFunc(ProcessDList);
            m_Callback_ProcessRdpList = new PFunc(ProcessRdpList);
            m_Callback_ShowCFB = new PFunc(ShowCFB);
        }

        public SHARE_STATE GetPluginCoreState() => R.GetCoreState();

        ~ModRsp() {
            R.RomClosed();
        }

        public override ExecutionState State => m_State;


        public override void Add(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void AttachBootManager(BootManager bootManager)
        {
            throw new NotSupportedException();
        }

        public override void BitwiseLogic(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Branch(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Break(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Init()
        {
            // R.RomOpen();

            Status.StatusFlags = StatusFlags.Halt;
        }

        protected override uint FetchInstruction(long address)
        {
            throw new NotImplementedException();
        }

        public unsafe override void AttachInterface(MipsInterface rcpInterface, SPInterface iface, DPCInterface rdpInterface)
        {
            base.AttachInterface(rcpInterface, iface, rdpInterface);

            iface.PCSet += (pc) => {
                PC = pc & PC_ALIGN_MASK;
                iface.SetPC((uint)PC);
                // Log.Debug("RSP PC Set: {0:X8}", PC);
            };

            iface.DmaReadOverride(() => {
                R.DmaRead();
            });

            iface.DmaWriteOverride(() => {
                R.DmaWrite();
            });

            Status.Change += () =>
            {
                /*
                    The way the plugin handles RSP Status is by reading the read-side of flags.
                    It doesn't handle the write side, the emulator host is in charge of that,
                    then reflecting the updates into the read-side
                */

                // Log.Debug("RSP Plugin: status change");

                if (Status.IsCmdEmpty)
                    return;

                if (Status.TestCmdFlags(StatusCmdFlags.ClearInterrupt)) {
                    RcpInterface.ClearInterrupt(MipsInterface.INT_SP);
                }
                else if (Status.TestCmdFlags(StatusCmdFlags.SetInterrupt)) {
                    RcpInterface.SetInterrupt(MipsInterface.INT_SP, true);
                }

                if (Status.TestCmdFlags(StatusCmdFlags.ClearInterruptOnBreak)) {
                    m_InterruptOnBroke = false;
                    Status.StatusFlags &= ~StatusFlags.InterruptOnBreak;
                }
                else if (Status.TestCmdFlags(StatusCmdFlags.SetInterruptOnBreak)) {
                    m_InterruptOnBroke = true;
                    Status.StatusFlags |= StatusFlags.InterruptOnBreak;
                }

                ProcessFlag(StatusCmdFlags.ClearSingleStep, StatusCmdFlags.SetSingleStep, StatusFlags.SingleStep);
                ProcessFlag(StatusCmdFlags.ClearSignal0, StatusCmdFlags.SetSignal0, StatusFlags.Signal0Set);
                ProcessFlag(StatusCmdFlags.ClearSignal1, StatusCmdFlags.SetSignal1, StatusFlags.Signal1Set);
                ProcessFlag(StatusCmdFlags.ClearSignal2, StatusCmdFlags.SetSignal2, StatusFlags.Signal2Set);
                ProcessFlag(StatusCmdFlags.ClearSignal3, StatusCmdFlags.SetSignal3, StatusFlags.Signal3Set);
                ProcessFlag(StatusCmdFlags.ClearSignal4, StatusCmdFlags.SetSignal4, StatusFlags.Signal4Set);
                ProcessFlag(StatusCmdFlags.ClearSignal5, StatusCmdFlags.SetSignal5, StatusFlags.Signal5Set);
                ProcessFlag(StatusCmdFlags.ClearSignal6, StatusCmdFlags.SetSignal6, StatusFlags.Signal6Set);
                ProcessFlag(StatusCmdFlags.ClearSignal7, StatusCmdFlags.SetSignal7, StatusFlags.Signal7Set);

                if (Status.TestCmdFlags(StatusCmdFlags.ClearHalt))
                {
                    // Allow SP to start execution
                    IsHalted = false;
                    // Log.Debug("RSP clear halt");
                }
                else if (Status.TestCmdFlags(StatusCmdFlags.SetHalt))
                {
                    IsHalted = true;
                    // Log.Debug("RSP set halt");
                }

                Status.ClearCmdFlags();
            };

            m_RspInfo.CheckInterrupts = m_Callback_CheckInterrupts;
            m_RspInfo.ProcessRdpList = m_Callback_ProcessRdpList;
            m_RspInfo.ProcessAList = m_Callback_ProcessAList;
            m_RspInfo.ProcessDList = m_Callback_ProcessDList;
            m_RspInfo.ShowCFB  = m_Callback_ShowCFB;

            var rspExports = iface.ExportPointers();

            m_RspInfo.IMEM = (byte*)rspExports.IMEMPtr;
            m_RspInfo.DMEM = (byte*)rspExports.DMEMPtr;
            m_RspInfo.RDRAM = (byte*)rspExports.RDRAMPtr;

            m_RspInfo.SP_DMA_BUSY_REG = (uint*)rspExports.BusyPtr;
            m_RspInfo.SP_DMA_FULL_REG = (uint*)rspExports.FullPtr;
            m_RspInfo.SP_DRAM_ADDR_REG = (uint*)rspExports.DramAddressPtr;
            m_RspInfo.SP_MEM_ADDR_REG = (uint*)rspExports.SpMemAddressPtr;
            m_RspInfo.SP_PC_REG = (uint*)rspExports.PCPtr;
            m_RspInfo.SP_RD_LEN_REG = (uint*)rspExports.ReadLenPtr;
            m_RspInfo.SP_SEMAPHORE_REG = (uint*)rspExports.SemaphorePtr;
            m_RspInfo.SP_STATUS_REG = (uint*)rspExports.StatusPtr;
            m_RspInfo.SP_WR_LEN_REG = (uint*)rspExports.BusyPtr;

            var rdpExports = rdpInterface.ExportPointers();

            m_RspInfo.DPC_BUFBUSY_REG = (uint*)rdpExports.BufferBusyCounterPtr;
            m_RspInfo.DPC_CLOCK_REG = (uint*)rdpExports.ClockPtr;
            m_RspInfo.DPC_CURRENT_REG = (uint*)rdpExports.CurrentPtr;
            m_RspInfo.DPC_END_REG = (uint*)rdpExports.EndPtr;
            m_RspInfo.DPC_PIPEBUSY_REG = (uint*)rdpExports.PipeBusyCounterPtr;
            m_RspInfo.DPC_START_REG = (uint*)rdpExports.StartPtr;
            m_RspInfo.DPC_STATUS_REG = (uint*)rdpExports.StatusPtr;
            m_RspInfo.DPC_TMEM_REG = (uint*)rdpExports.TmemLoadCounterPtr;

            m_RspInfo.MI_INTR_REG = (uint*)rcpInterface.ExportInterruptPtr();

            uint cycleCount = 0;

            R.InitiateRSP(m_RspInfo, ref cycleCount);

            // R.SetInstDebugHandler(m_Callback_InstDebug);

            Log.Debug("Finished RSP plugin initialization");
        }

        private void DebugInst(uint pc, uint inst) {
            Console.WriteLine("RSP {0:X8} {1:X8} {2}", pc, inst, Disassembler.Disassemble(pc, Disassembler.Decode(inst)));
        }

        private void ProcessFlag(StatusCmdFlags c, StatusCmdFlags s, StatusFlags f) {
            // Clear
            if (Status.TestCmdFlags(c)) {
                Status.StatusFlags &= ~f;
            }
            // Set
            else if (Status.TestCmdFlags(s)) {
                Status.StatusFlags |= f;
            }
        }

        public override void SafeSetPC(long address)
        {
            base.SafeSetPC(address);
            Interface.SetPC((uint)address);
        }

        private void CheckInterrupts() {

        }

        private void ProcessAList() {

        }

        private void ProcessDList() {

        }

        private unsafe void ProcessRdpList() {
            RdpInterface.DirectDLExecute();
        }

        private void ShowCFB() {

        }

        public override void Step()
        {
            if (!IsHalted) {
                R.DoRspCycles(1);
            }
        }

        public void CoreSingleStep() {
            if (!IsHalted) {
                R.SingleStep();
            }
        }

        public override void Jump(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Load(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void ManualStart(long pc)
        {
            throw new NotImplementedException();
        }

        public override ushort ReadVCC()
        {
            return R.GetVcc();
        }

        public override byte ReadVCE()
        {
            return (byte)R.GetVce();
        }

        public override ushort ReadVCO()
        {
            return R.GetVc0();
        }

        public override void SetOnLessThan(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Shift(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Store(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Subtract(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void TransferReg(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorAccumulatorReadWrite(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorAdd(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorBitwise(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorClip(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorCompare(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorLoad(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorMove(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorMultiply(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorReciprocal(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorStore(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorSubtract(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void VectorUnitReserved(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void WriteVCC(ushort vcc)
        {
            R.SetVcc(vcc);
        }

        public override void WriteVC0(ushort value) {
            R.SetVc0(value);
        }

        public void WriteVCE(byte value) {
            R.SetVce(value);
        }

        public override bool IsHalted {
            get {
                return Status.IsHaltSet;
            }

            protected set {
                if (value)
                {
                    Status.StatusFlags |= StatusFlags.Halt;
                }
                else
                {
                    Status.StatusFlags &= ~StatusFlags.Halt;
                    Status.StatusFlags &= ~StatusFlags.Broke;
                }
            }
        }
    }
}