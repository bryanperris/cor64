using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public static class CpuKromTests
    {


        /* A | B | LO | HI */
        private static readonly String[] DMULT = new String[]
        {
            "002BDC5461646522|00000000499602D7|A5C5654A8807338E|00000000000C9B87",
            "00000000499602D7|002BDC545E14D64E|A4D1C4E8FCE09782|00000000000C9B87",
            "FFD423ABA1EB29B2|FFFFFFFFB669FD29|A4D1C4E8FCE09782|00000000000C9B87",
            "002BDC545E14D64E|FFD423ABA1EB29B2|5135FDBC8869803C|FFFFF87C3E07477E",
            "FFFFFFFFFFFFFFFF|0000000000000002|FFFFFFFFFFFFFFFE|FFFFFFFFFFFFFFFF",
            "FFFFFFFFFFFFFFFF|FFFFFFFFFFFFFFFF|0000000000000001|0000000000000000",
            "FFFFFFFFB669FD29|FFD423AB9E9B9ADE|A5C5654A8807338E|00000000000C9B87"
        };

        /* A | B | LO | HI */
        private static readonly String[] DMULTU = new String[]
        {
            "FFFFFFFFFFFFFFFF|0000000000000002|FFFFFFFFFFFFFFFE|0000000000000001",
            "FFFFFFFFFFFFFFFF|FFFFFFFFFFFFFFFF|0000000000000001|FFFFFFFFFFFFFFFE",
            "002BDC5461646522|00000000499602D7|A5C5654A8807338E|00000000000C9B87",
            "00000000499602D7|002BDC545E14D64E|A4D1C4E8FCE09782|00000000000C9B87",
            "002BDC545E14D64E|01B69B4BACD05F15|D56ED599FA9C8666|00004B2593B73510",
            "01B69B4BACD05F15|00000002DFDC1C6C|6C388EE2B35A68DC|0000000004ECC0FC",
            "00000002DFDC1C6C|01B69B4BCDEBF359|CB6334F255A4658C|0000000004ECC0FC"
        };

        private static byte Hex2Dec(char v)
        {
            switch (v)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                default: throw new FormatException(nameof(v));
            }
        }

        private static ulong ParseUL(String value)
        {
            ulong parsedValue = 0;

            unchecked
            {
                for (int i = value.Length - 1; i >= 0; i--)
                {
                    int shift = value.Length - 1 - i;
                    parsedValue = ((ulong)Hex2Dec(value[i]) << (4 * shift)) | parsedValue;
                }
            }

            return parsedValue;
        }

        public static void Test_DMULT()
        {
            foreach (var test in DMULT)
            {
                var split = test.Replace("BREAK", "").Split('|');
                ulong a = ParseUL(split[0].ToUpper());
                ulong b = ParseUL(split[1].ToUpper());
                ulong lo = ParseUL(split[2].ToUpper());
                ulong hi = ParseUL(split[3].ToUpper());

                "dmult".CPUTest(a, b).Only64().ExpectLo(lo).ExpectHi(hi).Run(test.StartsWith("BREAK"));
            }
        }

        public static void Test_DMULTU()
        {
            foreach (var test in DMULTU)
            {
                var split = test.Replace("BREAK", "").Split('|');
                ulong a = ParseUL(split[0].ToUpper());
                ulong b = ParseUL(split[1].ToUpper());
                ulong lo = ParseUL(split[2].ToUpper());
                ulong hi = ParseUL(split[3].ToUpper());

                "dmultu".CPUTest(a, b).Only64().ExpectLo(lo).ExpectHi(hi).Run(test.StartsWith("BREAK"));
            }
        }
    }
}
