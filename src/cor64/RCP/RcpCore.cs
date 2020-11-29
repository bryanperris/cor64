using System.Text;
using System;
using System.Threading;
using cor64.IO;
using cor64.Mips;
using cor64.Mips.Rsp;
using NLog;
using cor64.Rdp.Renderers;
using cor64.Rdp;

namespace cor64.RCP
{
    public class RcpCore
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private const int DUMMY_SECTION_SIZE = 0x100000;

        private InterpreterBaseRsp m_Rsp;
        private N64MemoryController m_Memory;
        private DrawProcessor m_Rdp;

        public RcpCore()
        {
            m_Rsp = new RspInterpreter();
            m_Rsp.SetInstructionDebugMode(InstructionDebugMode.Full);

            m_Rdp = new DummyRdp();
        }

        public void SetRdpDevice(DrawProcessor processor) {
            m_Rdp = processor;
        }

        public void AttachToMemory(N64MemoryController controller)
        {
            m_Memory = controller;

            SerialDevice = new SerialController(controller);

            RcpInterface = new MipsInterface(controller);
            SerialDevice = new SerialController(controller);
            VideoInterface = new Video(controller, RcpInterface);
            RspInterface = new SPInterface(controller);
            ParellelInterface = new PIMemory(controller);
            DisplayProcessorCommandInterface = new DPCInterface(controller);
            DisplayProcessorSpanInterface = new DummyMemory(DUMMY_SECTION_SIZE, "Display Processor Span Interface");
            AudioInterface = new DummyMemory(DUMMY_SECTION_SIZE, "Audio Interface");

            SerialDevice.AttachInterfaces(RcpInterface);
            ParellelInterface.AttachInterface(RcpInterface);

            m_Rsp.AttachInterface(RcpInterface, RspInterface, DisplayProcessorCommandInterface);

            m_Rsp.AttachIStream(RspInterface.CreateIMemorySream());
            m_Rsp.AttachDStream(RspInterface.CreateDMemorySream());

            m_Rdp.AttachInterface(RcpInterface, DisplayProcessorCommandInterface);
            m_Rdp.AttachMemory(controller.CreateMemoryStream());

            controller.Model.SPRegs = RspInterface;
            controller.Model.DPCmdRegs = DisplayProcessorCommandInterface;
            controller.Model.DPSpanRegs = DisplayProcessorSpanInterface;
            controller.Model.MIRegs = RcpInterface;
            controller.Model.VIRegs = VideoInterface;
            controller.Model.AIRegs = AudioInterface;
            controller.Model.PIRegs = ParellelInterface;
            controller.Model.SIRegs = SerialDevice;
        }

        public InterpreterBaseRsp DeviceRsp => m_Rsp;

        public DrawProcessor DeviceRdp => m_Rdp;

        public SerialController SerialDevice { get; private set; }

        public Video VideoInterface { get; private set; }

        public MipsInterface RcpInterface { get; private set; }

        public SPInterface RspInterface { get; private set; }

        public DPCInterface DisplayProcessorCommandInterface { get; private set; }

        public BlockDevice DisplayProcessorSpanInterface { get; private set; }

        public BlockDevice AudioInterface { get; private set; }

        public PIMemory ParellelInterface { get; private set; }
    }
}