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
        private cor64.HLE.GraphicsHLEDevice m_HLEGraphicsDevice;

        // Pointer to window of where HLE graphics will be rendered to
        public IntPtr WindowPtr { get; set; }

        public RcpCore()
        {
            m_Rsp = new RspInterpreter();
            // m_Rsp.SetInstructionDebugMode(InstructionDebugMode.Full);

            m_Rdp = new DummyRdp();
        }

        public void SetRdpDevice(DrawProcessor processor) {
            m_Rdp = processor;
        }

        public void SetRspDevice(InterpreterBaseRsp interpreter) {
            m_Rsp = interpreter;
        }

        public void SetHLEGraphicsDevice(cor64.HLE.GraphicsHLEDevice device) {
            m_HLEGraphicsDevice = device;
        }

        public void AttachToMemory(N64MemoryController controller)
        {
            m_Memory = controller;

            SerialDevice = new SerialController(controller);

            RcpInterface = new MipsInterface(controller);
            SerialDevice = new SerialController(controller);
            VideoInterface = new Video(controller, RcpInterface);
            RspInterface = new SPInterface(controller);
            ParellelInterface = new ParallelInterface(controller);
            DisplayProcessorCommandInterface = new DPCInterface(controller, RcpInterface);
            DisplayProcessorSpanInterface = new DummyMemory(DUMMY_SECTION_SIZE, "Display Processor Span Interface", controller);
            AudioInterface = new Audio(controller, RcpInterface);

            SerialDevice.AttachInterfaces(RcpInterface);
            ParellelInterface.AttachInterface(RcpInterface);

            m_Rsp.AttachInterface(RcpInterface, RspInterface, DisplayProcessorCommandInterface);
            m_Rsp.AttachMemory(controller);

            m_Rsp.AttachHLEGraphics(this.GraphicsHLEDevice);

            if (m_HLEGraphicsDevice != null) {
                // HLE graphics emulation
                m_HLEGraphicsDevice.AttachInterface(
                    WindowPtr,
                    ((MemMappedCart) controller.Cart).GetCartridgePointer(), // Yes a lousy hack for now
                    RcpInterface,
                    RspInterface,
                    DisplayProcessorCommandInterface,
                    VideoInterface
                );
            }
            else {
                m_Rdp.AttachInterface(RcpInterface, DisplayProcessorCommandInterface, VideoInterface);
                m_Rdp.AttachMemory(controller);
            }

            controller.MountRcpInterfaces(
                RspInterface,
                DisplayProcessorCommandInterface, DisplayProcessorSpanInterface,
                RcpInterface,
                VideoInterface,
                AudioInterface,
                ParellelInterface,
                SerialDevice
            );
        }

        public InterpreterBaseRsp DeviceRsp => m_Rsp;

        public DrawProcessor DeviceRdp => m_Rdp;

        public cor64.HLE.GraphicsHLEDevice GraphicsHLEDevice => m_HLEGraphicsDevice;

        public SerialController SerialDevice { get; private set; }

        public Video VideoInterface { get; private set; }

        public MipsInterface RcpInterface { get; private set; }

        public SPInterface RspInterface { get; private set; }

        public DPCInterface DisplayProcessorCommandInterface { get; private set; }

        public N64MemoryDevice DisplayProcessorSpanInterface { get; private set; }

        public Audio AudioInterface { get; private set; }

        public ParallelInterface ParellelInterface { get; private set; }
    }
}