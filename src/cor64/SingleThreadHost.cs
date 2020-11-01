using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private bool m_BreakPoint = false;
        private bool m_StepOnce = false;
        public event Action Break;

        private Thread m_RspThread;

        public SingleThreadHost(N64System system)
        {
            m_System = system;
            m_StartWaitEvent = new AutoResetEvent(false);
        }

        private void RunLoop()
        {
            m_Exception = null;
            m_Running = true;
            m_StartWaitEvent.Set();

            try
            {
                Log.Info("CONTINUE...@" + m_System.DeviceCPU.ReadPC().ToString("X8"));

                while (true)
                {
                    if (m_Exception != null)
                    {
                        throw m_Exception;
                    }

                    if (m_System.Dbg.StepNext)
                    {
                        m_StepOnce = true;
                        m_BreakPoint = false;
                    }

                    if (!m_StepOnce && m_System.Dbg.IsBreakActive)
                    {
                        if (!m_BreakPoint)
                        {
                            Break?.Invoke();
                            Log.Info("BREAK...@" + m_System.DeviceCPU.ReadPC().ToString("X8"));
                            m_BreakPoint = true;
                        }

                        continue;
                    }

                    if (m_BreakPoint)
                    {
                        m_BreakPoint = false;
                        continue;
                    }

                    m_System.Tick();

                    if (m_StepOnce)
                    {
                        m_System.Dbg.Break();
                        m_StepOnce = false;
                    }
                }
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
            }
        }

        private void RspStart()
        {
            StringBuilder errorMessage = new StringBuilder();

            m_RspThread = new Thread(() =>
            {
                Log.Debug("RSP Core Execution has started...");

                while (true)
                {
                    try
                    {
                        while (true)
                        {
                            m_System.DeviceRcp.DeviceRsp.Step();

                            // We must sleep some to allow other events be processed
                            if (m_System.DeviceRcp.DeviceRsp.IsHalted) {
                                Thread.Sleep(100);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // var lastInst = m_Rsp.LastReadInst;

                        // errorMessage.AppendLine(String.Format("Last inst read: 0x{0:X8} 0x{1:X8} {2}", 
                        //     lastInst.Address, lastInst.Inst.inst, m_System.DeviceRcp.DeviceRsp.Disassembler.GetFullDisassembly(lastInst)));

                        errorMessage.Append("Thrown exception: ").AppendLine(e.Message);

                        Log.Error(errorMessage.ToString());

                        Log.Info("RSP will be halted now");

                        m_System.DeviceRcp.DeviceRsp.Halt();
                    }
                }
            })
            {
                Name = "RSP Core Thread"
            };

            m_RspThread.Start();
        }

        public void Start()
        {
            var t = new Thread(new ThreadStart(RunLoop))
            {
                Name = "MIPS R4300I CPU Thread"
            };
            
            t.Start();

            RspStart();

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
