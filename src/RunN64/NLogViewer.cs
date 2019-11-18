using System;
using System.Collections.Generic;
using NLog.Common;
using NLog.Targets;

namespace RunN64
{
    [Target("NLogViewer")]
    public class NLogViewer : Target
    {
        public event Action<AsyncLogEventInfo> LogReceived;
        
        private List<AsyncLogEventInfo> m_Cache = new List<AsyncLogEventInfo>();

        protected override void Write(AsyncLogEventInfo logEvent)
        {
            base.Write(logEvent);

            if (LogReceived != null) {
                LogReceived.Invoke(logEvent);
            }
            else {
                m_Cache.Add(logEvent);
            }
        }

        public IEnumerable<AsyncLogEventInfo> CheckMissedLogs() {
            var missed = new List<AsyncLogEventInfo>();
            missed.AddRange(m_Cache);
            m_Cache.Clear();
            return missed;
        }
    }
}