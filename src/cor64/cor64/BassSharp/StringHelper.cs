using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public static class StringHelper
    {
        public static bool Match(this String str, String pattern)
        {
            return Regex.Match(str, pattern).Success;
        }

        public static String LeftMatchAndTrim(this String str, String left, bool n = false)
        {
            if (str == null)
                return null;

            var m = Regex.Match(str, String.Format("{0}(.*)$", Regex.Escape(left)));

            if (m.Success && m.Groups.Count > 1)
                    return m.Groups[1].Captures[0].ToString();

            return n ? null : str;
        }

        public static String RightMatchAndTrim(this String str, String right, bool n = false)
        {
            if (str == null)
                return null;

            var m = Regex.Match(str, String.Format("^(.*){0}", Regex.Escape(right)));

            if (m.Success && m.Groups.Count > 1)
                return m.Groups[1].Captures[0].ToString();

            return n ? null : str;
        }

        public static String MatchAndTrimBoth(this String str, String left, String right, bool n = false)
        {
            var r = str.LeftMatchAndTrim(left, n).RightMatchAndTrim(right, n);

            return n ? r : (r == null ? str : r);
        }

        public static String Merge(this Stack<String> stack, String seperator)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;

            foreach (String s in stack) {
                sb.Append(s);

                if (i < stack.Count() - 1) {
                    sb.Append(seperator);
                }

                i++;
            }

            return sb.ToString();
        }

        public static bool Tokenize(this String str, String pattern)
        {
            return Regex.Match(str, "^" + pattern).Success;
        }

        public static void Prepend(this IList<String> list, String value)
        {
            list.Insert(0, value);
        }

        public static bool Tokenize(this IList<String> list, String s, String p)
        {
            var match = Regex.Match(s, p);

            if (match.Success) {
                foreach (var grp in match.Groups.OfType<Group>().Skip(1)) {
                    list.Add(grp.Value);
                }
            }

            return match.Success;
        }

        public static IList<String> Strip(this IList<String> l)
        {
            for (int i = 0; i < l.Count; i++) {
                l[i] = l[i].Trim();
            }

            return l;
        }

        public static String[] Strip(this String[] l)
        {
            for (int i = 0; i < l.Length; i++)
                l[i] = l[i].Trim();

            return l;
        }

        public static String TryGetValue(this IList<String> l, int offset, String def)
        {
            if (l.Count >= (offset + 1)) {
                return l[offset];
            }
            else {
                return def;
            }
        }

        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (predicate == null) throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items) {
                if (predicate(item)) return retVal;
                retVal++;
            }
            return -1;
        }

        public static String TakeItem(this IList<String> l, int item)
        {
            String val = l[item];
            l.RemoveAt(item);
            return val;
        }
    }
}
