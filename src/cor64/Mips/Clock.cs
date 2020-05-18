using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public class Clock
    {

        private int m_TickCount;
        private int m_TickComplete = 10;
        private int m_Divider = 2;

        public event Action<int> Tick;
        

        public Clock(int periodSize, int divider = 2)
        {
            m_TickComplete = periodSize;
            m_Divider = divider;
        }

        protected virtual void OnTick(int numTicks)
        {
            Tick?.Invoke(numTicks);
        }

        public bool MasterClock { get; private set; }

        public bool SClock { get; private set; }

        public bool PClock { get; private set; }

        /* This is used to track when count register should increment */
        public bool CountClock { get; private set; }

        public void ClearCountClock()
        {
            CountClock = false;
        }

        public int Divider { get; set; } = 2;

        public void SkipAhead(int cycleCount)
        {
            m_TickCount = (m_TickComplete / cycleCount);
            OnTick(cycleCount);
        }

        public void NextTick()
        {
            m_TickCount++;

            if ((m_TickCount % (m_TickComplete / m_Divider)) == 0)
            {
                MasterClock = !MasterClock;
                SClock = !SClock;
            }

            int pclockSpeed = m_TickComplete / (m_Divider * 2);

            if ((m_TickCount % (pclockSpeed)) == 0)
            {
                PClock = !PClock;
            }

            if ((m_TickCount % (pclockSpeed / 2)) == 0) {
                CountClock = true;
            }

            if (m_TickCount >= m_TickComplete)
            {
                m_TickCount = 0;
                OnTick(1);
            }
        }

    }
}
