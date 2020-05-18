using System.Text;
using System;
using System.Threading;
using cor64.IO;
using cor64.Mips;
using cor64.Mips.Rsp;
using NLog;

namespace cor64.RCP
{
    public class RcpCore
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private const int DUMMY_SECTION_SIZE = 0x100000;

        private InterpreterBaseRsp m_Rsp;
        private IRdpDevice m_Rdp;
        private N64MemoryController m_Memory;

        private StringBuilder m_ErrorMessageBuilder = new StringBuilder();

        private Thread m_RcpThread;

        public RcpCore()
        {
            m_Rsp = new RspInterpreter();
            m_Rsp.SetInstructionDebugMode(InstructionDebugMode.Full);
        }

        public void AttachToMemory(N64MemoryController controller)
        {
            m_Memory = controller;
            RcpInterface = new MipsInterface(controller);
            SerialInterface = new SerialMemory(controller);
            VideoInterface = new Video(controller, RcpInterface);
            RspInterface = new SPInterface(controller);
            ParellelInterface = new PIMemory(controller);
            DisplayProcessorCommandInterface = new DummyMemory(DUMMY_SECTION_SIZE, "Display Processor Command Interface");
            DisplayProcessorSpanInterface = new DummyMemory(DUMMY_SECTION_SIZE, "Display Processor Span Interface");
            AudioInterface = new DummyMemory(DUMMY_SECTION_SIZE, "Audio Interface");

            m_Rsp.AttachInterface(RspInterface);

            m_Rsp.AttachIStream(RspInterface.CreateIMemorySream());
            m_Rsp.AttachDStream(RspInterface.CreateDMemorySream());

            controller.Model.SPRegs = RspInterface;
            controller.Model.DPCmdRegs = DisplayProcessorCommandInterface;
            controller.Model.DPSpanRegs = DisplayProcessorSpanInterface;
            controller.Model.MIRegs = RcpInterface;
            controller.Model.VIRegs = VideoInterface;
            controller.Model.AIRegs = AudioInterface;
            controller.Model.PIRegs = ParellelInterface;
            controller.Model.SIRegs = SerialInterface;
        }

        public void Start()
        {
            m_RcpThread = new Thread(() =>
            {
                Log.Debug("RSP Core Execution has started...");

                while (true)
                {
                    try
                    {
                        while (true)
                        {
                            m_Rsp.Step();
                        }
                    }
                    catch (Exception e)
                    {
                        m_ErrorMessageBuilder.Clear();

                        // var lastInst = m_Rsp.LastReadInst;

                        // m_ErrorMessageBuilder.AppendLine(String.Format("Last inst read: 0x{0:X8} 0x{1:X8} {2}", 
                        //     lastInst.Address, lastInst.Inst.inst, m_Rsp.Disassembler.GetFullDisassembly(lastInst)));

                        m_ErrorMessageBuilder.AppendLine("Thrown exception: " + e.Message);

                        Log.Error(m_ErrorMessageBuilder.ToString());

                        Log.Info("RSP will be halted now");

                        m_Rsp.Halt();
                    }
                }
            })
            {
                IsBackground = true
            };

            m_RcpThread.Start();
        }

        public InterpreterBaseRsp DeviceRsp => m_Rsp;

        public SerialMemory SerialInterface { get; private set; }

        public Video VideoInterface { get; private set; }

        public MipsInterface RcpInterface { get; private set; }

        public SPInterface RspInterface { get; private set; }

        public BlockDevice DisplayProcessorCommandInterface { get; private set; }

        public BlockDevice DisplayProcessorSpanInterface { get; private set; }

        public BlockDevice AudioInterface { get; private set; }

        public BlockDevice ParellelInterface { get; private set; }
    }
}