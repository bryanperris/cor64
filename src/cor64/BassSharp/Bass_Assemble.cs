using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        protected virtual bool Assemble(String statement)
        {
            String s = statement;

            if (s.Match("block {")) return true;
            if (s.Match("} endblock")) return true;

            /* constant name(value) */

            var m = s.MatchAndTrimBoth("constant ", ")", true);

            if (m != null) {
                s = m;
                String[] p = s.Split('(');
                SetConstant(p[0], Evaluate(p[1]));
                return true;
            }

            /* scope name { */
            if (s.Match("scope (.*){")) {
                s = s.MatchAndTrimBoth("scope ", "{").Trim();
                if (s.EndsWith(":")) {
                    s = s.RightMatchAndTrim(":");
                    SetConstant(s, Pc);
                    AppendSymFile(s, Pc);
                }

                if (!String.IsNullOrEmpty(s))
                    ValidateName(s);

                m_ScopeStack.Push(s);
                return true;
            }

            /* } */
            if (s.Match("} endscope")) {
                m_ScopeStack.Pop();
                return true;
            }

            /* label: or label: { */
            if (s.Match("(.*):( {)?")) {
                s = s.RightMatchAndTrim(" {").RightMatchAndTrim(":");
                SetConstant(s, Pc);
                AppendSymFile(s, Pc);
                return true;
            }

            /* - or - { */
            if (s.Match("^-( {)?")) {
                SetConstant("lastLabel#" + (m_LastLabelCounter++).ToString(), Pc);
                return true;
            }

            /* + or + { */
            if (s.Match("^\\+( {)?")) {
                SetConstant("nextlabel#" + (m_NextLabelCounter++).ToString(), Pc);
                return true;
            }

            /* } */
            if (s.Match("} endconstant")) {
                return true;
            }

            /* output */
            if (s.Match("output .*")) {
                /* We simply do nothing here, since we are using streams */
                /* In the future, we could pass the target name to someone */
                return true;
            }

            /* endian (lsb|msb) */
            m = s.LeftMatchAndTrim("endian ", true);

            if (m != null) {
                s = m;

                if (s == "lsb") {
                    m_Endian = Endian.LSB; return true;
                }

                if (s == "msb") {
                    m_Endian = Endian.MSB; return true;
                }

                throw new Error("Invalid endian mode");
            }

            /* origin offset */
            m = s.LeftMatchAndTrim("origin ", true);

            if (m != null) {
                s = m;
                m_Origin = Evaluate(s);
                Seek(m_Origin);
                return true;
            }

            /* base offset */
            m = s.LeftMatchAndTrim("base ", true);

            if (m != null) {
                s = m;
                m_Base = Evaluate(s) - m_Origin;
                return true;
            }

            /* push variable [, ...] */
            m = s.LeftMatchAndTrim("pushvar ", true);

            if (m != null) {
                s = m;
                var p = s.Split(',').ToList().Strip();

                foreach (var t in p) {
                    if (t == "origin") {
                        m_PushStack.Push(m_Origin.ToString());
                    }
                    else if (t == "base") {
                        m_PushStack.Push(m_Base.ToString());
                    }
                    else if (t == "pc") {
                        m_PushStack.Push(m_Origin.ToString());
                        m_PushStack.Push(m_Base.ToString());
                    }
                    else {
                        throw new Error("Unrecognized push variable: " + t);
                    }
                }

                return true;
            }

            /* pull variable [, ...] */
            m = s.LeftMatchAndTrim("pullvar ", true);

            if (m != null) {
                s = m;
                var p = s.Split(',').ToList().Strip();

                foreach (var t in p) {
                    if (t == "origin") {
                        m_Origin = long.Parse(m_PushStack.Pop());
                        Seek(m_Origin);
                    }
                    else if (t == "base") {
                        m_Base = long.Parse(m_PushStack.Pop());
                    }
                    else if (t == "pc") {
                        m_Base = long.Parse(m_PushStack.Pop());
                        m_Origin = long.Parse(m_PushStack.Pop());
                        Seek(m_Origin);
                    }
                    else {
                        throw new Error("Unrecognized pull variable: " + t);
                    }
                }

                return true;
            }

            /* insert [name, ] filename [, offset] [, length] */
            m = s.LeftMatchAndTrim("insert ", true);

            if (m != null) {
                s = m;
                var p = s.Split(',').ToList().Strip();

                String name = null;

                /* match name */
                if (!p[0].Match("\".*\"")) name = p.TakeItem(0);

                if (!p[0].Match("\".*\""))
                    throw new Error("Missing source name");

                String sourceName = p.TakeItem(0).MatchAndTrimBoth("\"", "\"");

                var source = RequestBinarySource(sourceName);
                Stream stream = null;

                if (source == null)
                    throw new Error("Source not found");
                else
                    stream = source.GetStream();

                if (stream == null)
                    throw new Error("Source found, but stream is null");

                long offset = p.Count > 0 ? Evaluate(p.TakeItem(0)) : 0;
                long length = p.Count > 0 ? Evaluate(p.TakeItem(0)) : 0;

                if (offset > stream.Length)
                    offset = stream.Length;

                if (length == 0)
                    length = stream.Length - offset;

                if (name != null) {
                    SetConstant(name, Pc);
                    SetConstant(name + ".size", length);
                }

                if (stream.CanSeek)
                    stream.Position = stream.Seek(offset, SeekOrigin.Begin);
                else
                    stream.Position = offset;

                while (stream.Position < stream.Length && (length--) > 0) {
                    Write(stream.ReadByte(), 1);
                }

                return true;
            }

            /* fill length [, with] */
            m = s.LeftMatchAndTrim("fill ", true);

            if (m != null) {
                s = m;
                var p = s.Split(',').ToList().Strip();
                long length = Evaluate(p[0]);
                long b = Evaluate(p.TryGetValue(1, "0"));
                while (length-- > 0) Write(b);
                return true;
            }

            /* map 'char' [, value] [, length] */
            m = s.LeftMatchAndTrim("map ", true);

            if (m != null) {
                s = m;
                var p = s.Split(',').ToList().Strip();
                int index = (int)Evaluate(p[0]);
                long value = Evaluate(p.TryGetValue(1, "0"));
                long length = Evaluate(p.TryGetValue(2, "1"));

                for (int n = 0; n < length; n++)
                    m_StringTable[index + n] = value + n;

                return true;
            }

            /* d[bwldq] ("string"|variable) [, ...] */
            {
                int dataLength = 0;
                int tokenLength = 0;

                foreach (var d in m_Directives.EmitBytes) {
                    if (s.StartsWith(d.Token)) {
                        dataLength = d.Length;
                        tokenLength = d.Token.Length;
                        break;
                    }
                }

                if (dataLength > 0) {
                    s = s.Substring(tokenLength); /* Remove prefix */
                    var p = s.Split(',').ToList().Strip();

                    foreach (var t in p) {
                        if (t.Match("\".*\"")) {
                            var text = Text(t);
                            foreach (var b in text) {
                                Write(m_StringTable[b], dataLength);
                            }
                        }
                        else {
                            Write(Evaluate(t), dataLength);
                        }
                    }

                    return true;
                }
            }

            if (s.StartsWith("float32 ")) {
                s = s.Substring(8); /* Remove directive */
                var p = s.Split(',').ToList().Strip();

                foreach (var t in p) {
                    float data = float.Parse(t);
                    unsafe {
                        int* ptr = (int*)&data;
                        Write(*ptr, 4);
                    }
                }

                return true;
            }

            if (s.StartsWith("float64 ")) {
                s = s.Substring(8); /* Remove directive */
                var p = s.Split(',').ToList().Strip();

                foreach (var t in p) {
                    double data = double.Parse(t);
                    unsafe {
                        long* ptr = (long*)&data;
                        Write(*ptr, 8);
                    }
                }

                return true;
            }

            m = s.LeftMatchAndTrim("print ", true);

            if (m != null) {
                s = m.Trim();

                if (IsWritePhase) {
                    var p = s.Split(',').ToList().Strip();

                    foreach (var t in p) {
                        if (t.Match("\".*\"")) {
                            m_PrintLines.Add(Text(t));
                        }
                        else {
                            m_PrintLines.Add(Evaluate(t).ToString());
                        }
                    }
                }

                return true;
            }

            /* notice "string" */
            m = s.MatchAndTrimBoth("notice \"", "\"", true);

            if (m != null) {
                if (IsWritePhase) {
                    s = m.Trim();
                    Notice(Text(s));
                }

                return true;
            }

            /* warning "string" */
            m = s.MatchAndTrimBoth("warning \"", "\"", true);

            if (m != null) {
                if (IsWritePhase) {
                    s = m.Trim();
                    Warning(Text(s));
                }

                return true;
            }

            /* error "string" */
            m = s.MatchAndTrimBoth("error \"", "\"", true);

            if (m != null) {
                if (IsWritePhase) {
                    s = m.Trim();
                    throw new Error(Text(s));
                }

                return true;
            }

            return false;
        }
    }
}