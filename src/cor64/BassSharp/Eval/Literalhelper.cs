using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cor64.BassSharp.Eval
{
    internal static class LiteralHelper
    {
        public static bool IsLiteral(String s)
        {
            if (s.Length == 0)
                return false;

            char n = s[0];

            return
                (n >= 'A' && n <= 'Z') ||
                (n >= 'a' && n <= 'z') ||
                (n >= '0' && n <= '9') ||
                (n == '%' || n == '$' || n == '_' || n == '.') ||
                (n == '\\' || n == '\"');
        }

        public static String LiteralNumber(ref String s)
        {
            if (s.Length < 1) return s;

            /* Match for hex */
            var match = Regex.Match(s, "^(?:0x|\\$)([0-9a-fA-F]*)");

            /* Match for binary */
            if (!match.Success) {
                match = Regex.Match(s, "^(?:0b|%)([0-1]*)");
            }

            /* Match for octal */
            if (!match.Success) {
                match = Regex.Match(s, "^(?:0o)([0-7]*)");
            }

            /* Match for float */
            if (!match.Success) {
                match = Regex.Match(s, "^([0-9]*\\.[0-9]*)");
            }

            /* Match for decumal */
            if (!match.Success) {
                match = Regex.Match(s, "^([0-9]*)");
            }

            if (!match.Success) {
                return s;
            }
            else {
                var result = s.Substring(match.Index, match.Length);
                s = s.Substring(match.Length, s.Length - match.Length);
                return result;
            }
        }

        public static String LiteralString(ref String s)
        {
            String p = s;
            int o = 0;
            char escape = p.Substring(o++)[0];

            while ((o + 1) < p.Length && p[o] != escape) o++;

            if (p[o++] != escape) {
                throw new Error("Unclosed String literal");
            }

            String result = s.Substring(p.Length - o);
            s = s.Substring(o);
            return result;
        }

        public static String LiteralVariable(ref String s)
        {
            var match = Regex.Match(s, "^([_.A-Za-z0-9]*)");

            if (match.Success) {
                var result = s.Substring(match.Index, match.Length);
                s = s.Substring(match.Length, s.Length - match.Length);
                return result;
            }
            else {
                return s;
            }
        }

        public static String Literal(ref String s)
        {
            String p = s;

            if (p[0] >= '0' && p[0] <= '9') return LiteralNumber(ref s);
            if (p[0] == '%' || p[0] == '$') return LiteralNumber(ref s);
            if (p[0] == '\'' || p[0] == '\"') return LiteralString(ref s);
            if (p[0] == '_' || p[0] == '.' || 
                (p[0] >= 'A' && p[0] <= 'Z') || 
                (p[0] >= 'a' && p[0] <= 'z')) return LiteralVariable(ref s);

            throw new Error("Invalid literal");
        }
    }
}
