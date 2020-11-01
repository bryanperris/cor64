using System;
using System.Collections.Generic;
using System.Linq;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        void SetToken<T>(String name, bool local, T t, String nameExtra, Func<Frame, ICollection<T>> stackFunc, IEnumerable<String> validationExtra)
        where T : Token
        {
            if (m_FrameStack.Count < 1)
                return;

            var tokens = stackFunc(
                m_FrameStack.ElementAt(local ? m_FrameStack.Count - 1 : 0));

            ValidateName(name);

            String scopedName = nameExtra == null ? name : (name + nameExtra);

            if (m_ScopeStack.Count > 0) {
                scopedName = String.Format("{0}.{1}", m_ScopeStack.Merge("."), scopedName);
            }

            if (validationExtra != null)
                foreach (String s in validationExtra) {
                    ValidateName(s);
                }

            var token = tokens.Where(x => x.Equals(scopedName));

            if (token.Count() > 0) {
                token.First().CloneFrom(t);
            }
            else {
                t.Name = scopedName;
                tokens.Add(t);
            }
        }

        void SetMacro(String name, List<String> parameters, int ip, bool scoped, bool local)
        {
            var v = parameters.Select(x => x.Split(' ').Last().Trim()).ToList();
            var m = new Macro(name, parameters, ip, scoped);
            var e = ":" + parameters.Count;

            SetToken<Macro>(name, local, m, e, (s) => s.Macros, v);

            Log.Trace("Added macro: {0}: {1} ({2})", local ? "local" : "global", name, String.Join(",", parameters));
        }

        T FindToken<T>(String name, bool? local, Func<Frame, ICollection<T>> stackFunc)
            where T : Token
        {
            if (local.HasValue) {
                if (m_FrameStack.Count < 1)
                    return null;

                var tokens = stackFunc(m_FrameStack.ElementAt(local.Value ? m_FrameStack.Count - 1 : 0));
                var s = new Stack<String>(m_ScopeStack);

                while (true) {
                    String scopedName = String.Format("{0}{1}{2}",
                        s.Merge("."), s.Count > 0 ? "." : "", name);

                    var results = tokens.Where(x => x.Equals(scopedName));

                    if (results.Count() > 0) {
                        return results.First();
                    }

                    if (s.Count < 1) break;
                    s.Pop();
                }

                return null;
            }
            else {
                var r = FindToken<T>(name, true, stackFunc);

                if (r == null)
                    return FindToken<T>(name, false, stackFunc);
                else
                    return r;
            }
        }

        Macro FindMacro(String name, bool local)
        {
            return FindToken<Macro>(name, local, s => s.Macros);
        }

        Macro FindMacro(String name)
        {
            return FindToken<Macro>(name, null, s => s.Macros);
        }

        void SetDefine(String name, String value, bool local)
        {
            var d = new Define(name, value);
            SetToken<Define>(name, local, d, null, s => s.Defines, null);
            Log.Trace("Added defintion: {0}: {1} = {2}", local ? "local" : "global", name, value);
        }

        Define FindDefine(String name, bool local)
        {
            return FindToken<Define>(name, local, s => s.Defines);
        }

        Define FindDefine(String name)
        {
            return FindToken<Define>(name, null, s => s.Defines);
        }

        void SetVariable(String name, long value, bool local)
        {
            var v = new Variable(name, value);
            SetToken<Variable>(name, local, v, null, s => s.Variables, null);
        }

        Variable FindVariable(String name, bool local)
        {
            return FindToken<Variable>(name, local, s => s.Variables);
        }

        Variable FindVariable(String name)
        {
            return FindToken<Variable>(name, null, s => s.Variables);
        }

        void SetConstant(String name, long value)
        {
            ValidateName(name);

            String scopedname = name;

            if (m_ScopeStack.Count > 0) {
                scopedname = String.Format("{0}.{1}", m_ScopeStack.Merge("."), name);
            }

            var constant = m_Constants.Where(x => x.Equals(scopedname));

            if (constant.Count() > 0) {
                if (IsQueryPhase) {
                    throw new Error("Constant cannot be modified: ", scopedname);
                }

                constant.First().Value = value;
            }
            else {
                m_Constants.Add(new Constant(scopedname, value));
                Log.Trace("Constant Added: {0} = {1}", scopedname, value);
            }
        }

        Constant FindConstant(String name)
        {
            var s = new Stack<String>(m_ScopeStack);

            while (true) {
                String scopedName = String.Format("{0}{1}{2}",
                    s.Merge("."), s.Count > 0 ? "." : "", name);

                var results = m_Constants.Where(x => x.Name.Equals(scopedName));

                if (results.Count() > 0) {
                    return results.First();
                }

                if (s.Count < 1) break;
                s.Pop();
            }

            return null;
        }

        void EvaluateDefines(ref String statement)
        {
            String s = statement;
            for (int x = statement.Length - 1, y = -1; x >= 0; x--) {
                if (s[x] == '}') y = x;

                if (s[x] == '{' && y > x) {
                    String name = s.Substring(x + 1, y - x - 1);

                    var m = name.LeftMatchAndTrim("defined ", true);

                    if (m != null) {
                        name = m.Trim();
                        statement = String.Format("{0}{1}{2}",
                            s.Substring(0, x),
                            FindDefine(name) != null ? 1 : 0,
                            s.Substring(y + 1));

                        EvaluateDefines(ref statement);
                        return;
                    }

                    var define = FindDefine(name);

                    if (define != null) {
                        statement = String.Format("{0}{1}{2}",
                            s.Substring(0, x),
                            define.Value,
                            s.Substring(y + 1));

                        EvaluateDefines(ref statement);
                        return;
                    }
                }
            }
        }

        String Text(String s)
        {
            String t = s;

            if (!t.Match("\"*\""))
                Warning("String value is unquoted: {0}" + s);

            t = t.Substring(1, t.Length - 2);
            t = t.Replace("\\s", "\'");
            t = t.Replace("\\d", "\"");
            t = t.Replace("\\b", ";");
            t = t.Replace("\\n", "\n");
            t = t.Replace("\\\\", "\\");

            return t;
        }

        long Character(String s)
        {
            if (s[0] != '\'') goto unknown;
            if (s[2] == '\'') return s[1];
            if (s[3] != '\'') goto unknown;
            if (s[1] != '\\') goto unknown;
            if (s[2] == 's') return '\'';
            if (s[2] == 'd') return '\"';
            if (s[2] == 'b') return ';';
            if (s[2] == 'n') return '\n';
            if (s[2] == '\\') return '\\';

            unknown:
            Warning("Unrecognized character constant: {0}", s);
            return 0;
        }

        void ValidateName(String name)
        {
            if (!IsQueryPhase) return;

            string p = name;

            if (!((p[0] >= 'A' && p[0] <= 'Z') ||
                (p[0] >= 'a' && p[0] <= 'z') ||
                p[0] == '_' || p[0] == '#')) {

                Warning("Invalid name: {0}", name);
                return;
            }

            while ((p = p.Substring(1)).Length > 0) {
                if (!((p[0] >= 'A' && p[0] <= 'Z') ||
                    (p[0] >= 'a' && p[0] <= 'z') ||
                    (p[0] >= '0' && p[0] <= '9') ||
                    p[0] == '_' ||
                    p[0] == '.' ||
                    p[0] == '#')) {

                    Warning("Invalid name: {0}", name);
                    return;
                }
            }
        }
    }
}