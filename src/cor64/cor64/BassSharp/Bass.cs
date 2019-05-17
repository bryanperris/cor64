using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NLog;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();

        private Directives m_Directives = new Directives();
        private ITarget m_Target;
        private List<ISource> m_Sources = new List<ISource>();
        private List<Instruction> m_Program = new List<Instruction>();

        private HashSet<Define> m_Defines = new HashSet<Define>();
        private HashSet<Constant> m_Constants = new HashSet<Constant>();

        /* State machine stacks */
        private Stack<Block> m_BlockStack = new Stack<Block>();
        private Stack<Frame> m_FrameStack = new Stack<Frame>();
        private Stack<bool> m_IfStack = new Stack<bool>();
        private Stack<String> m_PushStack = new Stack<string>();
        private Stack<String> m_ScopeStack = new Stack<string>();

        /* Debug stuff */
        private List<String> m_PrintLines = new List<string>();


        private long[] m_StringTable = new long[256];
        private Phase m_Phase = Phase.Analyze;
        private Endian m_Endian = Endian.LSB;
        private int m_MacroInvocationCount;
        private int m_Ip;
        private long m_Origin = 0;
        private long m_Base;
        private int m_LastLabelCounter = 1;
        private int m_NextLabelCounter = 1;
        private bool m_Strict;
        private StringBuilder m_SymFileBuffer = new StringBuilder();


        public Stream Output => m_Target.GetStream();

        protected void SetTarget(ITarget target)
        {
            m_Target = target;
        }

        public enum Phase
        {
            Analyze,
            Query,
            Write
        }

        public enum Endian
        {
            LSB,
            MSB
        }

        public enum Evaluation
        {
            Default = 0,
            Strict
        }

        public Endian Endianess
        {
            get { return m_Endian; }
        }

        protected Directives CurrentDirectives
        {
            get { return m_Directives; }
            set { m_Directives = value; }
        }

        protected abstract ISource RequestCodeSource(String name);

        protected abstract ISource RequestBinarySource(String name);

        protected Stream OpenResourceStream(Type type, String name)
        {
            return type.Assembly.GetManifestResourceStream(type, name);
        }

        protected String ReadStringResource(Type type, String name)
        {
            using (var stream =OpenResourceStream(type, name)) {
                if (stream == null)
                    return null;

                StreamReader reader = new StreamReader(stream, ASCIIEncoding.ASCII, false);
                reader.BaseStream.Position = 0;
                return reader.ReadToEnd();
            }
        }

        protected bool Target(ITarget target, bool create)
        {
            if (create && m_Target != null) {
                m_Target.Close();
            }

            return true;
        }

        protected bool Source(ISource source)
        {
            if (source == null)
                return false;

            int fileNumber = m_Sources.Count;
            m_Sources.Add(source);

            StreamReader reader = new StreamReader(source.getStream());
            reader.BaseStream.Position = 0;
            String textSource = reader.ReadToEnd();

            textSource.Replace("\t\r", "  ");

            String[] lines = textSource.Split('\n');

            for (int i = 0; i < lines.Length; i++) {
                String line = lines[i];

                /* Look for single line comments and filter them out */
                Match match = Regex.Match(line, "(\\/\\/)+.*");

                if (match.Success) {
                    line = line.Substring(0, match.Index);
                }

                if (line.Length < 0)
                    continue;

                line = FixWhitespace(line);

                String[] blocks = line.Split(';');

                for (int j = 0; j < blocks.Length; j++) {
                    String statement = blocks[j].Trim();

                    if (String.IsNullOrEmpty(statement)) continue;

                    var m = statement.MatchAndTrimBoth("include \"", "\"", true);

                    if (m != null) {
                        Source(RequestCodeSource(m));
                        Log.Debug("Added source: {0}", m);
                    }
                    else {
                        m_Program.Add(
                            new Instruction() {
                                statement = statement,
                                fileNumber = fileNumber,
                                lineNumber = i + 1,
                                blockNumber = j + 1
                            });
                    }
                }
            }

            return true;
        }

        protected void Define(String name, String value)
        {
            m_Defines.Add(new Define(name, value));
        }

        protected void Constant(String name, String value)
        {
            m_Constants.Add(
                new Constant(name, Evaluate(value, Evaluation.Strict)));
        }

        protected virtual void Assemble(bool strict = false)
        {
            m_Strict = strict;

            m_Phase = Phase.Analyze;
            Analyze();

            m_Phase = Phase.Query;
            Execute();

            m_Phase = Phase.Write;
            Execute();
        }

        public long Pc => m_Origin + m_Base;

        protected void Seek(long offset)
        {
            if (m_Target != null) {
                if (IsWritePhase) {
                    m_Target.Seek(offset);
                }
            }
        }

        protected void Write(long data, int length = 1)
        {
            Write((ulong)data, length);
        }

        protected void Write(ulong data, int length = 1)
        {
            if (m_Target != null) {
                if (IsWritePhase) {
                    if (m_Endian == Endian.LSB)
                        m_Target.WriteLE(data, length);

                    if (m_Endian == Endian.MSB)
                        m_Target.WriteBE(data, length);
                }
            }

            m_Origin += length;
        }

        public static String FixWhitespace(String s)
        {
            var matches = Regex.Matches(s, "(\\s+)|(?:\".*\")");
            var sb = new StringBuilder(s);
            int offset = 0;

            foreach (Match m in matches) {
                if (m.Success) {
                    if (m.Length > 1 && m.Groups.Count > 1) {
                        foreach (var capture in m.Groups[1].Captures) {
                            sb.Remove((m.Index - offset) + 1, m.Length - 1);
                            offset += m.Length - 1;
                        }
                    }
                }
            }

            return sb.ToString();
        }

        protected void Notice(String message)
        {
            Log.Info(message);
        }

        protected void Warning(String message, params object[] args)
        {
            Log.Warn(message, args);
        }

        protected virtual void Initialize()
        {
            m_PushStack.Clear();
            m_ScopeStack.Clear();

            for (int i = 0; i < m_StringTable.Length; i++)
                m_StringTable[i] = i;

            m_Endian = Endian.LSB;
            m_Origin = 0;
            m_Base = 0;
            m_LastLabelCounter = 1;
            m_NextLabelCounter = 1;
        }

        public bool IsAnalyzePhase => m_Phase == Phase.Analyze;

        public bool IsQueryPhase => m_Phase == Phase.Query;

        public bool IsWritePhase => m_Phase == Phase.Write;

        public long Origin => m_Origin;

        public long Base => m_Base;

        public int Ip => m_Ip;

        public ISet<Constant> Constants => m_Constants;

        public ISet<Define> Defines => m_Defines;

        public int MacroCount => m_Program.Where(x => x.statement == "} endmacro").ToList().Count;

        public IList<String> PrintLines => m_PrintLines.AsReadOnly();
    }
}
