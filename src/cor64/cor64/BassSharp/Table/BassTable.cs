using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using NLog;

namespace cor64.BassSharp.Table
{
    public abstract partial class BassTable : Bass
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();
        private static Logger TableLog = LogManager.GetLogger("TableAssembler");
        private List<Opcode> m_Table = new List<Opcode>();
        private ulong m_BitVal;
        private long m_BitPos;
        private bool m_Keep;

        private class EmbeddedSource : ISource
        {
            private MemoryStream m_Stream;

            public EmbeddedSource(Stream s)
            {
                byte[] buffer = new byte[s.Length];
                s.Read(buffer, 0, buffer.Length);
                m_Stream = new MemoryStream(buffer, 0, buffer.Length);
            }

            public Stream getStream()
            {
                return m_Stream;
            }
        }

        public IList<Opcode> GetOpcodes()
        {
            return m_Table.ToList();
        }

        protected override ISource RequestCodeSource(string name)
        {
            try {
                using (var stream = OpenResourceStream(this.GetType(), name)) {
                    if (stream == null)
                        return null;

                    stream.Position = 0;
                    return new EmbeddedSource(stream);
                }

            }
            catch (FileNotFoundException) {
                return RequestStreamSource(name);
            }
        }

        protected abstract ISource RequestStreamSource(String name);

        protected override void Initialize()
        {
            base.Initialize();
            m_BitPos = 0;
            m_BitVal = 0;

            if (!m_Keep) m_Table.Clear();
        }

        internal String ReadResourceData(String name)
        {
            return ReadStringResource(typeof(BassTable), name);
        }

        public bool AssembleArchTable(String data)
        {
            m_Keep = true;
            m_Table.Clear();
            ParseTable(data);
            return true;
        }

        protected override bool Assemble(string statement)
        {
            if (base.Assemble(statement))
                return true;

            String s = statement;

            var m = s.LeftMatchAndTrim("arch ", true);

            if (m != null) {
                String data = "";

                if (m == "") data = "";
                else if (m == "n64.cpu") data = "n64-cpu.arch";
                else if (m == "n64.rdp") data = "n64-rdp.arch";
                else if (m == "n64.rsp") data = "n64-rsp.arch";
                else {
                    throw new NotSupportedException("Arch " + m + " not supported");
                }

                if (data != null && data.Length > 0) {
                    Log.Info("Instruction Set: " + m);
                    DirectiveParser directiveParser = new DirectiveParser(this);
                    data = directiveParser.ParseDirective(data);
                    m_Table.Clear();
                    ParseTable(data);
                }

                return true;
            }

            m = s.MatchAndTrimBoth("instrument \"", "\"", true);

            if (m != null) {
                s = m;
                ParseTable(s);
                return true;
            }

            long pc = this.Pc;

            foreach (var opcode in m_Table) {
                if (!s.Tokenize(opcode.Pattern))
                    continue;

                List<String> args = new List<string>();
                args.Tokenize(s, opcode.Pattern);

                if (args.Count != opcode.NumberList.Count)
                    continue;

                bool mismatch = false;


                /* This checks if the active operands match the opcode's operand format
                 * We call ArgumentBitLength for a quick measure
                 * If operands don't match, and we care, we skip to next opcode in table
                 * othwewise it is a match
                 */

                foreach (var format in opcode.FormatList.Where((f) => f.FType == Format.Type.Absolute && f.FMatch == Format.Match.Weak )) {
                    String arg = args[format.Argument];
                    int bits = ArgumentBitLength(ref arg);
                    args[format.Argument] = arg;

                    if (bits != opcode.NumberList[format.Argument].Bits) {
                        if (format.FMatch == Format.Match.Exact || bits != 0) {
                            mismatch = true;
                            break;
                        }
                    }
                }

                if (mismatch) continue;

                foreach (var format in opcode.FormatList) {

                    var arg = format.Argument;
                    var num = arg < opcode.NumberList.Count ? opcode.NumberList[arg] : null;
                    var bits = num == null ? 0 : num.Bits;

                    switch (format.FType) {
                        case Format.Type.Static: {
                                WriteBits(format.Data, format.Bits);
                                break;
                            }

                        case Format.Type.Absolute: {
                                var data = (uint) Evaluate(args[arg]);
                                WriteBits(data, bits);
                                break;
                            }

                        case Format.Type.Relative: {
                                var data = (int) Evaluate(args[arg]) - (pc + format.Displacement);
                                var min = -(1 << (bits - 1));
                                var max = Math.Abs(1 << (bits - 1)) - 1;

                                if (data < min || data > max)
                                    throw new Error("branch out of bounds");

                                WriteBits((ulong)data, bits);
                                break;
                            }

                        case Format.Type.Repeat: {
                                var data = (uint) Evaluate(args[arg]);
                                
                                for (uint n = 0; n < data; n++) {
                                    WriteBits(format.Data, bits);
                                }

                                break;
                            }

                        case Format.Type.ShiftRight: {
                                var data = (ulong)Evaluate(args[arg]);
                                WriteBits(data >> (int)format.Data, bits);
                                break;
                            }

                        case Format.Type.ShiftLeft: {
                                var data = (ulong)Evaluate(args[arg]);
                                WriteBits(data << (int)format.Data, bits);
                                break;
                            }

                        case Format.Type.RelativeShiftRight: {
                                var data = (int)Evaluate(args[arg]) - (pc + format.Displacement);
                                var min = -(1 << (bits - 1));
                                var max = Math.Abs(1 << (bits - 1)) - 1;

                                if (data < min || data > max)
                                    throw new Error("branch out of bounds");

                                bits -= (int)format.Data;

                                if (Endianess == Endian.LSB) {
                                    WriteBits((ulong)(data >> (int)format.Data), bits);
                                }
                                else {
                                    data >>= (int)format.Data;
                                    WriteBits(SwapEndian((ulong)data, bits), bits);
                                }

                                break;
                            }

                        case Format.Type.Negative: {
                                var data = (int)Evaluate(args[arg]);
                                WriteBits((uint)-data, bits);
                                break;
                            }

                        case Format.Type.NegativeShiftRight: {
                                var data = Evaluate(args[arg]);
                                WriteBits((((ulong)-data) >> (int)format.Data), bits);
                                break;
                            }

                        default: break;
                    }
                }

                return true;
            }

            return false;
        }

        public ulong SwapEndian(ulong data, int bits)
        {
            ulong t_data = 0;

            switch ((bits - 1) / 8) {
                case 3: { // 4 bytes
                        t_data = ((data & 0xFF000000) >> 24) |  ((data & 0x00FF0000) >> 8) |   ((data & 0x0000FF00) << 8) |  ((data & 0x000000FF) << 24);
                        break;
                    }
                case 2: { // 3 bytes
                        t_data = ((data & 0xFF0000) >> 16) | ((data & 0x00FF00)) | ((data & 0x0000FF) << 16);
                        break;
                    }
                case 1: { // 2 bytes
                        t_data = ((data & 0xFF00) >> 8) | ((data & 0x00FF) << 8);
                        break;
                    }
                case 0: { // byte
                        t_data = data;
                        break;
                    }
                default: {
                        throw new Error("Invalid number of bits for BassTable::swapEndian");
                    }
            }
            return t_data;
        }

        private static int GetBinaryLength(String value)
        {
            int i = 0;

            for (i = 0; i < value.Length; i++)
            {
                if (value[0] != '0' && value[0] != '1')
                {
                    return 0;
                }
            }

            return i;
        }

        private static int GetHexLength(String value)
        {
            int i = 0;

            for (i = 0; i < value.Length; i++)
            {
                if (value[0] < '0' && value[0] > '9') { return 0; }
                if (value[0] < 'a' && value[0] > 'f') { return 0; }
                if (value[0] < 'A' && value[0] > 'F') { return 0; }
            }

            return i * 4;
        }

        public static int ArgumentBitLength(ref String text)
        {
            int len = 0;

            if (text.Length > 0)
            {
                switch (text[0])
                {
                    default: return 0;
                    case '<': len = 8; break;
                    case '>': len = 16; break;
                    case '^': len = 24; break;
                    case '?': len = 32; break;
                    case ':': len = 64; break;
                    case '%': return GetBinaryLength(text.Substring(1));
                    case '$': return GetHexLength(text.Substring(1));
                    case '0':
                        {
                            if (text.Length >= 2)
                            {
                                switch (text[1])
                                {
                                    default: return 0;
                                    case 'b': return GetBinaryLength(text.Substring(2));
                                    case 'x': return GetHexLength(text.Substring(2));
                                }
                            }

                            return 0;
                        }
                }
            }

            /* We reach here when using fixed lengths */
            text = text.Remove(0, 1).Insert(0, " ");
            return len;
        }

        protected void WriteBits(ulong data, int length)
        {
            Func<int, ulong> setBits = n => {
                /* Create a bit mask with the n least significant bits set */
                return (1UL << n) - 1;
            };

            m_BitVal <<= length;
            m_BitVal |= data & setBits(length);
            m_BitPos += length;

            while (m_BitPos >= 8) {
                Write(m_BitVal);
                m_BitVal >>= 8;
                m_BitPos -= 8;
            }
        }

        protected void ParseDirective(String line)
        {
            var part = line.Split(';');
            var rhs = part[1].Split(':');
            String token = rhs[0];

            Log.Trace("Parsing Directive: " + line);

            if (token == "EMIT_BYTES") {
                int i = int.Parse(rhs[1]);
                CurrentDirectives.EmitBytes[i].Token = part[0];
            }
            else {
                Warning("Unrecognized " + token);
            }
        }

        protected bool ParseTable(String text)
        {
            var lines = text.Split('\n')
                .Where(x => !String.IsNullOrEmpty(x))
                .Select(x => x.Trim())
                .ToList();

            if (lines[0] == "//DIRECTIVES") {
                int n = 0;

                lines.RemoveAt(0);
                foreach (var line in lines) {
                    ++n;

                    if (line == "//INSTRUCTIONS")
                        break;

                    ParseDirective(line);
                }

                lines.RemoveRange(0, n);
            }
            else {
                // reset
                CurrentDirectives = new Directives();
            }

            foreach (var line in lines.Where(x => x.Length > 0)) {
                String l = line;


                /* Strip comments */
                var position = line.IndexOf("//");

                if (position >= 0) {
                    l = l.Substring(0, position);
                }

                var part = l.Split(';').ToList().Strip();

                if (part.Count != 2) continue;

                Opcode opcode = new Opcode();
                AssembleTableLHS(opcode, part[0]);
                AssembleTableRHS(opcode, part[1]);
                m_Table.Add(opcode);
            }

            StringBuilder sb = new StringBuilder();

            foreach (var x in m_Table)
            {
                sb.Append(Regex.Unescape(x.Pattern.Split(' ')[0]).Trim() + " ");
            }

            Log.Trace("Table: " + sb.ToString());

            return true;
        }

        protected void AssembleTableLHS(Opcode opcode, String text)
        {
            int offset = 0;

            Func<int> length = () => {
                int len = 0;

                while (offset + len < text.Length) {
                    char n = text[offset + len];
                    if (n == '*') break;
                    len++;
                }

                return len;
            };

            while (offset < text.Length) {
                int size = length();
                opcode.PrefixList.Add(new Prefix(text.Substring(offset, size), size));
                offset += size;

                if (offset >= text.Length) continue;
           
                if (text[offset] != '*') continue;

                int bits = 10 * (text[offset + 1] - '0');
                bits += text[offset + 2] - '0';
                opcode.NumberList.Add(new Number() { Bits = bits });
                offset += 3;
            }

            foreach (var prefix in opcode.PrefixList) {
                opcode.Pattern += Regex.Escape(prefix.Text);
                opcode.Pattern += "(.*)";
            }

            opcode.Pattern = opcode.Pattern.RightMatchAndTrim("(.*)");

            if (opcode.NumberList.Count == opcode.PrefixList.Count)
                opcode.Pattern += "(.*)";
        }

        private Format AppendFormat(Opcode opcode, String item, Format.Type type)
        {
            return AppendFormat(opcode, item, type, Format.Match.Exact, 0, false, false);
        }

        private Format AppendFormat(
                Opcode opcode,
                String item,
                Format.Type type,
                Format.Match match,
                int m,
                bool displacement,
                bool argument)
        {
            return AppendFormat(opcode, item, type, match, m, displacement, argument, false);
        }

        private unsafe Format AppendFormat(
                Opcode opcode,
                String item,
                Format.Type type,
                Format.Match match,
                int m,
                bool displacement,
                bool argument,
                bool postiveDisplacment)
        {
            var format = new Format() { FType = type, FMatch = match };
            opcode.FormatList.Add(format);

            if (argument) {
                char operand = item[m];

                /* if the letter is upper case, make it lowercase */
                if (operand >= 'A' && operand <= 'Z') {
                    operand = operand.ToString().ToLower()[0];
                }

                /* Now convert the letter into an index (based on alphabet) */
                format.Argument = operand - 'a';
            }

            if (displacement) {
                if (!postiveDisplacment)
                    format.Displacement = -(item[1] - '0');
                else
                    format.Displacement = Math.Abs(item[1] - '0');
            }

            return format;
        }

        protected void AssembleTableRHS(Opcode opcode, String text)
        {
            var list = text.Split(' ');

            foreach (var item in list) {
                if (text[0] == '$' && item.Length == 3) {
                    var format = AppendFormat(opcode, item, Format.Type.Static);
                    format.Data = Convert.ToUInt32(item.Substring(1), 16);
                    format.Bits = (item.Length - 1) * 4;
                }

                if (item[0] == '%') {
                    var format = AppendFormat(opcode, item, Format.Type.Static);
                    format.Data = Convert.ToUInt32(item.Substring(1), 2);
                    format.Bits = (item.Length - 1);
                }

                if (item[0] == '!') {
                    AppendFormat(opcode, item, 
                        Format.Type.Absolute, Format.Match.Exact, 1, false, true);
                }

                if (item[0] == '=') {
                    AppendFormat(opcode, item,
                        Format.Type.Absolute, Format.Match.Strong, 1, false, true);
                }

                if (item[0] == '~') {
                    AppendFormat(opcode, item,
                        Format.Type.Absolute, Format.Match.Weak, 1, false, true);
                }

                if (item[0] == '+' && item[2] != '>') {
                    AppendFormat(opcode, item,
                        Format.Type.Relative, Format.Match.Exact, 2, true, true);
                }

                if (item[0] == '-') {
                    AppendFormat(opcode, item,
                        Format.Type.Relative, Format.Match.Exact, 2, true, true);
                }

                if (item[0] == '*') {
                    var format = AppendFormat(opcode, item,
                        Format.Type.Repeat, Format.Match.Exact, 1, true, true);
                    format.Data = Convert.ToUInt32(item.Substring(3));
                }

                // >>XXa
                if (item[0] == '>' && item[1] == '>') {
                    var format = AppendFormat(opcode, item, 
                        Format.Type.ShiftRight, Format.Match.Weak, 4, false, true);

                    format.Data = (uint)((item[2] - '0') * 10 + (item[3] - '0'));
                }

                if (item[0] == '<' && item[1] == '<') {
                    var format = AppendFormat(opcode, item,
                        Format.Type.ShiftLeft, Format.Match.Weak, 4, false, true);

                    format.Data = (uint)((item[2] - '0') * 10 + (item[3] - '0'));
                }

                // +X>>YYa
                if (item[0] == '+' && item[2] == '>' && item[3] == '>') {
                    var format = AppendFormat(opcode, item,
                        Format.Type.RelativeShiftRight, Format.Match.Weak, 6, false, true, true);

                    format.Data = (uint)((item[4] - '0') * 10 + (item[5] - '0'));
                }

                // N>>XXa
                if (item[0] == 'N' && item[1] == '>' && item[2] == '>') {
                    var format = AppendFormat(opcode, item,
                        Format.Type.NegativeShiftRight, Format.Match.Weak, 5, false, true, true);

                    format.Data = (uint)((item[3] - '0') * 10 + (item[4] - '0'));
                }

                // Na
                if (item[0] == 'N' && item[1] != '>') {
                    AppendFormat(opcode, item, 
                        Format.Type.Negative, Format.Match.Weak, 1, false, true);
                }
            }
        }
    }
}
