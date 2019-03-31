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
        private N64System m_System;
        private bool m_Running;
        private Exception m_Exception;
        private AutoResetEvent m_StartWaitEvent;

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
                Log.Debug("CPU execution starting at 0x{0:X8}", ((CoreR4300I)m_System.DeviceCPU).ReadPC());

                while (true)
                {
                    if (m_Exception != null)
                    {
                        throw m_Exception;
                    }

                    m_System.StepOnce();
                }
            }
            catch (Exception e)
            {
                m_Exception = e;
            }
            finally
            {
                m_Running = false;
                m_StartWaitEvent.Reset();
            }
        }

        public void Start()
        {
            var t = new Thread(new ThreadStart(RunLoop));
            t.Start();

            if (!m_Running)
                m_StartWaitEvent.WaitOne();
        }

        public void Interrupt()
        {
            m_Exception = new Exception("The system was interrupted");
        }

        public bool IsRunning => m_Running;

        public Exception ThrownException => m_Exception;
    }
}
