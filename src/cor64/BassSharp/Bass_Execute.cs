using System;
using System.Collections.Generic;
using System.Linq;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        bool Execute()
        {
            m_FrameStack.Clear();
            m_IfStack.Clear();
            m_Ip = 0;
            m_MacroInvocationCount = 0;

            Initialize();

            m_FrameStack.Push(new Frame());

            foreach (Define define in m_Defines) {
                SetDefine(define.Name, define.Value, true);
            }

            while (m_Ip < m_Program.Count) {
                Instruction instruction = m_Program[m_Ip++];

                if (!ExecuteInstruction(instruction))
                    throw new Error("Unrecognized directive: " + instruction.statement);
            }

            m_FrameStack.Pop();
            return true;
        }

        bool ExecuteInstruction(Instruction instruction)
        {
            String s = instruction.statement;
            EvaluateDefines(ref s);

            if (s.Match("(global macro|macro) .*\\(.*\\) {")) {
                bool local = !s.StartsWith("global ");
                s = s.LeftMatchAndTrim("global ").MatchAndTrimBoth("macro ", ") {");
                String[] p = s.Split('(');
                bool scoped = p[0].StartsWith("scope ");
                p[0] = p[0].LeftMatchAndTrim("scope ");
                String[] a = String.IsNullOrEmpty(p[1]) ? new String[0] : p[1].Split(',');

                for (int i = 0; i < a.Length; i++)
                    a[i] = a[i].Trim();

                SetMacro(p[0], a.ToList(), m_Ip, scoped, local);
                m_Ip = instruction.ip;
                return true;
            }

            if (s.Match("(global define|define) .*\\(.*\\)")) {
                bool local = !s.StartsWith("global ");
                s = s.LeftMatchAndTrim("global ").MatchAndTrimBoth("define ", ")");
                String[] p = s.Split('(');
                SetDefine(p[0], p[1], local);
                return true;
            }

            if (s.Match("(global evaluate|evaluate) .*\\(.*\\)")) {
                bool local = !s.StartsWith("global ");
                s = s.LeftMatchAndTrim("global ").MatchAndTrimBoth("evaluate ", ")");
                String[] p = s.Split('(');
                SetDefine(p[0], Evaluate(p[1]).ToString(), local);
                return true;
            }

            if (s.Match("(global variable|variable) .*\\(.*\\)")) {
                bool local = !s.StartsWith("global ");
                s = s.LeftMatchAndTrim("global ").MatchAndTrimBoth("variable ", ")");
                String[] p = s.Split('(');
                SetVariable(p[0], Evaluate(p[1]), local);
                return true;
            };

            var m = s.MatchAndTrimBoth("if ", " {", true);

            if (m != null) {
                s = m.Trim();
                bool match = Evaluate(s, Evaluation.Strict) != 0;

                m_IfStack.Push(match);

                if (!match) {
                    m_Ip = instruction.ip;
                }

                return true;
            }

            m = s.MatchAndTrimBoth("} else if ", "{", true);

            if (m != null) {
                if (m_IfStack.Peek()) {
                    m_Ip = instruction.ip;
                }
                else {
                    s = m.Trim();
                    bool match = Evaluate(s, Evaluation.Strict) != 0;

                    m_IfStack.Pop();
                    m_IfStack.Push(match);

                    if (!match) {
                        m_Ip = instruction.ip;
                    }

                    return true;
                }
            }

            if (s.Match("} else {")) {
                if (m_IfStack.Peek()) {
                    m_Ip = instruction.ip;
                }
                else {
                    m_IfStack.Pop();
                    m_IfStack.Push(true);
                }

                return true;
            }

            if (s.Match("} endif")) {
                m_IfStack.Pop();
                return true;
            }

            m = s.MatchAndTrimBoth("while ", " {", true);

            if (m != null) {
                s = m.Trim();
                bool match = Evaluate(s, Evaluation.Strict) != 0;

                if (!match)
                    m_Ip = instruction.ip;

                return true;
            }

            if (s.Match("} endwhile")) {
                m_Ip = instruction.ip;
                return true;
            }

            if (s.Match(".*\\(.*\\)")) {
                IList<String> getParams(String[] value)
                {
                    if (value != null) {
                        if (value.Length >= 2) {
                            return value[1].Split(',').Strip();
                        }
                    }

                    return new String[] { };
                }

                var p = s.RightMatchAndTrim(")").Split(new char[] { '(' }, 2);
                var a = getParams(p);

                String name = String.Format("{0}:{1}", p[0], a.Count);
                Macro macro = FindMacro(name);

                if (macro != null) {
                    List<Parameter> paramsters = new List<Parameter>();

                    for (int n = 0; n < a.Count; n++) {
                        var p2 = macro.Parameters[n].Split(' ').ToList().Strip();

                        if (p2.Count == 1)
                            p2.Prepend("define");

                        if (p2[0] == "define")
                            paramsters.Add(new Parameter(Parameter.Type.Define, p2[1], a[n]));

                        else if (p2[0] == "string")
                            paramsters.Add(new Parameter(Parameter.Type.Define, p2[1], Text(a[n])));

                        else if (p2[0] == "evaluate")
                            paramsters.Add(new Parameter(Parameter.Type.Define, p2[1], Evaluate(a[n]).ToString()));

                        else if (p2[0] == "variable")
                            paramsters.Add(new Parameter(Parameter.Type.Variable, p2[1], Evaluate(a[n]).ToString()));

                        else throw new Error("Unsupported parameter type: " + p2[0]);
                    }

                    Frame frame = new Frame();
                    m_FrameStack.Push(frame);
                    frame.Ip = m_Ip;
                    frame.InvokedBy = instruction;
                    frame.Scoped = macro.Scoped;

                    if (macro.Scoped) {
                        m_ScopeStack.Push(p[0]);
                    }

                    SetDefine("#", "_" + (m_MacroInvocationCount++).ToString(), true);

                    foreach (Parameter parameter in paramsters) {
                        if (parameter.PType == Parameter.Type.Define)
                            SetDefine(parameter.Name, parameter.Value, true);

                        if (parameter.PType == Parameter.Type.Variable)
                            SetVariable(parameter.Name, long.Parse(parameter.Value), true);
                    }

                    m_Ip = macro.Ip;
                    return true;
                }
            }

            if (s.Match("} endmacro")) {
                m_Ip = m_FrameStack.Peek().Ip;
                if (m_FrameStack.Peek().Scoped) m_ScopeStack.Pop();
                m_FrameStack.Pop();
                return true;
            }

            if (Assemble(s)) {
                return true;
            }

            Evaluate(s);
            return true;
        }
    }
}