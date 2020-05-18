using System;
using System.Text;

namespace cor64.Utils
{
    public static class HexTools
    {
        public static String ToHex(byte[] buffer, int offset, int len)
        {
            StringBuilder sb = new StringBuilder();

            for (var i = 0; i < len; i++)
            {
                sb.Append(buffer[offset + i].ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
