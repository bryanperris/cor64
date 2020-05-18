using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public class Profiler
    {
        private List<List<long>> m_TimeLog = new List<List<long>>();
        private List<Opcode> m_OpcodeLog = new List<Opcode>();
        private Dictionary<String, int> m_ProfiledInstructions = new Dictionary<string, int>();
        private Stopwatch m_Stopwatch = new Stopwatch();
        private int m_CurrentInd;
        private Object m_Lock = new object();

        public Profiler()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartSession(Opcode opcode)
        {
            lock (m_Lock)
            {

                if (!m_ProfiledInstructions.ContainsKey(opcode.Op))
                {
                    m_TimeLog.Add(new List<long>());
                    m_OpcodeLog.Add(opcode);
                    m_ProfiledInstructions.Add(opcode.Op, m_TimeLog.Count - 1);
                }

                m_CurrentInd = m_ProfiledInstructions[opcode.Op];

                m_Stopwatch.Reset();
                m_Stopwatch.Start();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopSession()
        {
            m_Stopwatch.Stop();

            lock (m_Lock)
            {
                m_TimeLog[m_CurrentInd].Add(m_Stopwatch.ElapsedTicks);
            }
        }

        private long GetOpTimeAverage(string key) {
            int ind = m_ProfiledInstructions[key];
            var list = m_TimeLog[ind];
            long sum = 0;

            for (int i = 0; i < list.Count; i++)
            {
                sum += list[i];
            }

            return sum / list.Count;
        }

        private int GetLength(string key)
        {
            return m_TimeLog[m_ProfiledInstructions[key]].Count;
        }

        public String GenerateReport()
        {
            StringBuilder sb = new StringBuilder();

            lock (m_Lock)
            {
                var results = from e in m_ProfiledInstructions
                              let len = GetLength(e.Key)
                              let time = GetOpTimeAverage(e.Key)
                              orderby len descending
                              select e;

                foreach (var entry in results)
                {
                    sb.AppendFormat("{0} T:{2} #{1} ", entry.Key.PadRight(8), GetLength(entry.Key), GetOpTimeAverage(entry.Key).ToString().PadRight(5));
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
