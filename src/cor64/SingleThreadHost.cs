using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cor64.Mips;
using cor64.Mips.R4300I;
using NLog;

namespace cor64
{
    public class SingleThreadHost
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly N64System m_System;
        private bool m_Running;
        private Exception m_Exception;
        private readonly AutoResetEvent m_StartWaitEvent;
        public event Action Break;
        private Timer m_SiReadyDelayTimer;

        private Thread m_RspThread;

        private bool m_SignalExit = false;
        private readonly ManualResetEvent m_ExitWait = new ManualResetEvent(true);

        public SingleThreadHost(N64System system)
        {
            m_System = system;
            m_StartWaitEvent = new AutoResetEvent(false);
        }

        public void SignalExit() {
            m_SignalExit = true;
            m_ExitWait.WaitOne();
        }

        private void RunLoop()
        {
            m_Exception = null;
            m_Running = true;
            m_StartWaitEvent.Set();

            bool paused = true;

            var debugger = m_System.Dbg;

            // m_SiReadyDelayTimer = new Timer((_) => m_System.DeviceRcp.SerialDevice.SignalSiReady(), null, 7000, 0);

            debugger.DebugBreak += () => {
                m_System.DeviceCPU.Debugger.Break();
                m_System.DeviceRcp.DeviceRsp.Debugger.Break();
                Log.Info("BREAK...@" + m_System.DeviceCPU.ReadPC().ToString("X8"));
            };

            debugger.DebugContinue += () => {
                m_System.DeviceCPU.Debugger.Continue();
                m_System.DeviceRcp.DeviceRsp.Debugger.Continue();
                Log.Info("CONTINUE...@" + m_System.DeviceCPU.ReadPC().ToString("X8"));
            };

            try
            {
                if (CoreConfig.Current.WorkbenchMode) {
                    while (!m_SignalExit)
                    {
                        if (m_Exception != null) throw m_Exception;

                        // Debugger break active, do not do anything
                        if (debugger.IsBreakActive) {

                            if (!paused) {
                                // TODO: Report RSP break
                                paused = true;
                            }

                            Thread.Sleep(100);

                            continue;
                        }

                        if (paused) {
                            paused = false;
                        }

                        // System core step
                        m_System.Tick();

                        // RSP step
                        // while (!m_System.DeviceRcp.DeviceRsp.IsHalted)
                        m_System.DeviceRcp.DeviceRsp.Step();

                        if (debugger.StepNext || debugger.StepRspNext) {
                            debugger.Break();
                        }

                        if (!debugger.IsBreakActive && m_System.DeviceCPU.Debugger.IsBreakActive) {
                            debugger.Break();
                        }

                        if (!debugger.IsBreakActive && m_System.DeviceRcp.DeviceRsp.Debugger.IsBreakActive) {
                            debugger.Break();
                        }
                    }
                }
                else {
                    while (!m_SignalExit)
                    {
                        if (m_Exception != null) throw m_Exception;

                        // System core step
                        m_System.Tick();

                        // RSP step
                        // while (!m_System.DeviceRcp.DeviceRsp.IsHalted)
                        m_System.DeviceRcp.DeviceRsp.Step();
                    }
                }

                m_ExitWait.Set();
            }
            catch (Exception e)
            {
                Log.Error(e);
                m_Exception = e;
            }
            finally
            {
                m_Running = false;
                m_StartWaitEvent.Reset();
                m_System.TickFinally();
                m_ExitWait.Set();
            }
        }

        // private void RspStart()
        // {
        //     StringBuilder errorMessage = new StringBuilder();

        //     m_RspThread = new Thread(() =>
        //     {
        //         Log.Debug("RSP Core Execution has started...");

        //         while (true)
        //         {
        //             try
        //             {
        //                 while (true)
        //                 {
        //                     m_System.DeviceRcp.DeviceRsp.Step();

        //                     #if DEBUG
        //                     // We must sleep some to allow other events be processed
        //                     // This is needed to allow the .NET debugger to function
        //                     if (m_System.DeviceRcp.DeviceRsp.IsHalted) {
        //                         Thread.Sleep(100);
        //                     }
        //                     #endif
        //                 }
        //             }
        //             catch (Exception e)
        //             {
        //                 // var lastInst = m_Rsp.LastReadInst;

        //                 // errorMessage.AppendLine(String.Format("Last inst read: 0x{0:X8} 0x{1:X8} {2}", 
        //                 //     lastInst.Address, lastInst.Inst.inst, m_System.DeviceRcp.DeviceRsp.Disassembler.GetFullDisassembly(lastInst)));

        //                 errorMessage.Append("Thrown exception: ").AppendLine(e.Message);

        //                 Log.Error(e.StackTrace.ToString());

        //                 Log.Error(errorMessage.ToString());

        //                 Log.Info("RSP will be halted now");

        //                 m_System.DeviceRcp.DeviceRsp.Halt();
        //             }
        //         }
        //     })
        //     {
        //         Name = "RSP Core Thread"
        //     };

        //     // m_RspThread.Start();
        // }

        public void Start()
        {
            var t = new Thread(new ThreadStart(RunLoop))
            {
                Name = "MIPS R4300I CPU Thread"
            };
            
            t.Start();

            //RspStart();

            if (!m_Running)
                m_StartWaitEvent.WaitOne();
        }

        public void Interrupt()
        {
            m_Exception = new Exception("The system was interrupted");
        }

        public void Resume()
        {
            m_Exception = null;
            Start();
        }

        public bool IsRunning => m_Running;

        public Exception ThrownException => m_Exception;
    }
}
