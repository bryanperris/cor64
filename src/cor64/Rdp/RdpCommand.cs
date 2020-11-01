using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Collections.Generic;

namespace cor64.Rdp
{
    public class RdpCommand
    {
        private byte[] m_Data;
        [ThreadStatic] private static readonly StringBuilder s_AsmParams = new StringBuilder();
        [ThreadStatic] private static bool s_FlagHit;

        protected RdpCommand()
        {

        }

        public RdpCommand(RdpCommandType type, byte[] data)
        {
            Type = type;
            m_Data = data;
        }

        public TRdpCommand As<TRdpCommand>()
            where TRdpCommand : RdpCommand
        {
            return (TRdpCommand)Activator.CreateInstance(typeof(TRdpCommand), new object[] { this.Type, this.m_Data });
        }

        internal RdpCommand As(RuntimeTypeHandle typeHandle) {
            return (RdpCommand)Activator.CreateInstance(System.Type.GetTypeFromHandle(typeHandle), new object[] { this.Type, this.m_Data });
        }

        protected ulong ReadU64(int index, int shiftRight) {
            unsafe
            {
                fixed (byte* ptr = m_Data)
                {
                    var v = *((ulong*)ptr + index);
                    return v >> shiftRight;
                }
            }
        }

        protected uint ReadU32(int index, int shiftRight) {
            unsafe
            {
                fixed (byte* ptr = m_Data)
                {
                    var v = *((uint*)ptr + index);
                    return v >> shiftRight;
                }
            }
        }

        public void PrintHexLines() {
            for (int i = 0; i < (m_Data.Length / 8); i++) {
                Console.WriteLine("CmdHex {0:00} {1:X16}", i, ReadU64(i, 0));
            }
        }

        public void PrintHexLines32() {
            for (int i = 0; i < (m_Data.Length / 8); i++) {
                Console.WriteLine("CmdHex {0:00} {1:X8}", i, ReadFieldUn(i, 0, 0xFFFFFFFF));
                Console.WriteLine("CmdHex {0:00} {1:X8}", i + 1, ReadFieldUn(i, 32, 0xFFFFFFFF));
            }
        }

        private static void GenerateCArray32(StringBuilder stringBuilder, String name, ulong value, bool start, bool lastLine) {
            if (start) {
                stringBuilder.Append("static const uint32_t ").Append(name).AppendLine("[0x2C] = {");
            }
            else {
                uint a = (uint)(value >> 32);
                uint b = (uint)(value);

                stringBuilder.AppendFormat("    0x{0:X8}, 0x{1:X8}{2}\n", a, b, lastLine ? "" : ",");

                if (lastLine)
                    stringBuilder.AppendLine("};");
            }
        }

        public void PrintCommandCArray(string name = "data") {
            StringBuilder sb = new StringBuilder();

            var len = m_Data.Length / 8;

            for (int i = 0; i <= len; i++) {
                GenerateCArray32(sb, name, ReadU64(len - i, 0), i == 0, i == len);
            }

            Console.WriteLine(sb.ToString());
        }

        protected int ReadField(int index, int offset, int mask) => (int) ReadU64(index, offset) & mask;

        protected uint ReadFieldUn(int index, int offset, uint mask) => (uint) ReadU64(index, offset) & mask;

        protected int ReadField(int offset, int mask) => (int) ReadU64(0, offset) & mask;

        protected bool ReadFlag(int index, int offset) => ReadField(index, offset, 1) != 0;

        protected bool ReadFlag(int offset) => ReadField(0, offset, 1) != 0;

        public uint ReadFieldU32(int offset, uint mask = 0) => (uint)ReadU64(0, offset) & mask;

        public String ToAsm()
        {
            string p = Params();

            if (String.IsNullOrWhiteSpace(p)) {
                return Type.Name;
            }
            else {
                return String.Format("{0} {1}", Type.Name, p);
            }
        }

        protected virtual String Params()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < m_Data.Length; i++)
            {
                sb.Append(m_Data[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public IReadOnlyList<byte> Data => m_Data;

        public RdpCommandType Type { get; private set; }

        protected void AsmParams_Start() {
            s_FlagHit = false;
            s_AsmParams.Clear();
        }

        protected void AsmParams_AppendFlag(String flagName) {
            if (s_FlagHit) s_AsmParams.Append("|");
            s_AsmParams.Append(flagName);
            s_FlagHit = true;
        }

        protected void AsmParams_AppendParam(String paramValue) {
            s_AsmParams.Append(paramValue);
        }

        protected String AsmParams_End() {
            s_FlagHit = false;
            return s_AsmParams.ToString();
        }

        public bool DoesMatchType(RdpCommandType type)
        {
            return this.Type == type;
        }

        public override String ToString()
        {
            return ToAsm();
        }
    }
}