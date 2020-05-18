using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public static class RoundHelper
    {
        public static float Round(float value, RoundingMode mode)
        {
            switch (mode)
            {
                default: return value;
                //case MipsRoundingMode.Nearest: return (float)Math.Round(value);
                case RoundingMode.TowardPostive: return (float)Math.Ceiling(value);
                case RoundingMode.TowardNegative: return (float)Math.Floor(value);
                case RoundingMode.TowardZero: return (float)Math.Truncate(value);
            }
        }

        public static double Round(double value, RoundingMode mode)
        {
            switch (mode)
            {
                default: return value;
                //case MipsRoundingMode.Nearest: return Math.Round(value);
                case RoundingMode.TowardPostive: return Math.Ceiling(value);
                case RoundingMode.TowardNegative: return Math.Floor(value);
                case RoundingMode.TowardZero: return Math.Truncate(value);
            }
        }
    }
}
