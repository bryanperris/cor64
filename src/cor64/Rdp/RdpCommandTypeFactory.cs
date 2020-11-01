using System;
using System.Collections.Generic;
using COMMAND = System.Action<cor64.Rdp.RdpCommand>;

namespace cor64.Rdp
{
    public abstract class RdpCommandTypeFactory
    {
        private static int s_CurrentId { get; set; }

        public static int Count { get; protected set; }

        public static RdpCommandType Define(String name)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, 8, RdpCommandFlags.None);
        }

        public static RdpCommandType Define(String name, RuntimeTypeHandle typeHandle)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, 8, RdpCommandFlags.None, typeHandle);
        }

        public static RdpCommandType Define(String name, RdpCommandFlags flags)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, 8, flags);
        }

        public static RdpCommandType Define(String name, RdpCommandFlags flags, RuntimeTypeHandle typeHandle)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, 8, flags, typeHandle);
        }

        public static RdpCommandType Define(String name, int size)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, size, RdpCommandFlags.None);
        }

        public static RdpCommandType Define(String name, int size, RuntimeTypeHandle typeHandle)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, size, RdpCommandFlags.None, typeHandle);
        }

        public static RdpCommandType Define(String name, int size, RdpCommandFlags flags)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, size, flags);
        }

        public static RdpCommandType Define(String name, int size, RdpCommandFlags flags, RuntimeTypeHandle typeHandle)
        {
            Count++;
            return new RdpCommandType(s_CurrentId++, name, size, flags, typeHandle);
        }

        public static CallTable CreateCallTable()
        {
            return new CallTable();
        }

        public class CallTable
        {
            private COMMAND[] m_Table;
            private Dictionary<int, COMMAND> m_MappedEntries = new Dictionary<int, COMMAND>();

            public CallTable()
            {
            }

            public CallTable Map(Action<RdpCommand> handler, params RdpCommandType[] types)
            {
                foreach (var t in types)
                {
                    m_MappedEntries.Add(t.Id, handler);
                }

                return this;
            }

            public void Finish()
            {
                m_Table = new COMMAND[Count];

                foreach (var x in m_MappedEntries)
                {
                    m_Table[x.Key] = x.Value;
                }

                m_MappedEntries.Clear();
            }

            public Action<RdpCommand> this[RdpCommand command]
            {
                get
                {
                    return m_Table[command.Type.Id];
                }
            }
        }
    }
}