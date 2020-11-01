using System.Security.AccessControl;
using NLog;

namespace cor64.Rdp.LLE
{
    public static class ZBuffer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public const int ZMODE_OPAQUE = 0;
        public const int ZMODE_INTERPENETRATING = 1;
        public const int ZMODE_TRANSPARENT = 2;
        public const int ZMODE_DECAL = 3;

        public static readonly ushort[] ZComTable = new ushort[0x40000];
        public static readonly uint[] ZCompleteDecTable = new uint[0x4000];
        public static readonly ushort[] DeltaZComparatorLut = new ushort[0x10000];

        public struct ZDecEntry
        {
            public readonly int shift;
            public readonly uint add;

            internal ZDecEntry(int shift, uint add)
            {
                this.shift = shift;
                this.add = add;
            }
        }

        public static readonly ZDecEntry[] ZDecTable = {
            new ZDecEntry ( 6, 0x00000 ),
            new ZDecEntry ( 5, 0x20000 ),
            new ZDecEntry ( 4, 0x30000 ),
            new ZDecEntry ( 3, 0x38000 ),
            new ZDecEntry ( 2, 0x3C000 ),
            new ZDecEntry ( 1, 0x3E000 ),
            new ZDecEntry ( 0, 0x3F000 ),
            new ZDecEntry ( 0, 0x3F800 ),
        };

        public static void InitZBuffer()
        {
            // ZComTable

            for (int z = 0; z < 0x40000; z++)
            {
                ushort altmem;
                switch ((z >> 11) & 0x7F)
                {
                    case 0x00:
                    case 0x01:
                    case 0x02:
                    case 0x03:
                    case 0x04:
                    case 0x05:
                    case 0x06:
                    case 0x07:
                    case 0x08:
                    case 0x09:
                    case 0x0a:
                    case 0x0b:
                    case 0x0c:
                    case 0x0d:
                    case 0x0e:
                    case 0x0f:
                    case 0x10:
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 0x14:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x1a:
                    case 0x1b:
                    case 0x1c:
                    case 0x1d:
                    case 0x1e:
                    case 0x1f:
                    case 0x20:
                    case 0x21:
                    case 0x22:
                    case 0x23:
                    case 0x24:
                    case 0x25:
                    case 0x26:
                    case 0x27:
                    case 0x28:
                    case 0x29:
                    case 0x2a:
                    case 0x2b:
                    case 0x2c:
                    case 0x2d:
                    case 0x2e:
                    case 0x2f:
                    case 0x30:
                    case 0x31:
                    case 0x32:
                    case 0x33:
                    case 0x34:
                    case 0x35:
                    case 0x36:
                    case 0x37:
                    case 0x38:
                    case 0x39:
                    case 0x3a:
                    case 0x3b:
                    case 0x3c:
                    case 0x3d:
                    case 0x3e:
                    case 0x3f:
                        altmem = (ushort)((z >> 4) & 0x1FFC);
                        break;
                    case 0x40:
                    case 0x41:
                    case 0x42:
                    case 0x43:
                    case 0x44:
                    case 0x45:
                    case 0x46:
                    case 0x47:
                    case 0x48:
                    case 0x49:
                    case 0x4a:
                    case 0x4b:
                    case 0x4c:
                    case 0x4d:
                    case 0x4e:
                    case 0x4f:
                    case 0x50:
                    case 0x51:
                    case 0x52:
                    case 0x53:
                    case 0x54:
                    case 0x55:
                    case 0x56:
                    case 0x57:
                    case 0x58:
                    case 0x59:
                    case 0x5a:
                    case 0x5b:
                    case 0x5c:
                    case 0x5d:
                    case 0x5e:
                    case 0x5f:
                        altmem = (ushort)(((z >> 3) & 0x1FFC) | 0x2000);
                        break;
                    case 0x60:
                    case 0x61:
                    case 0x62:
                    case 0x63:
                    case 0x64:
                    case 0x65:
                    case 0x66:
                    case 0x67:
                    case 0x68:
                    case 0x69:
                    case 0x6a:
                    case 0x6b:
                    case 0x6c:
                    case 0x6d:
                    case 0x6e:
                    case 0x6f:
                        altmem = (ushort)(((z >> 2) & 0x1FFC) | 0x4000);
                        break;
                    case 0x70:
                    case 0x71:
                    case 0x72:
                    case 0x73:
                    case 0x74:
                    case 0x75:
                    case 0x76:
                    case 0x77:
                        altmem = (ushort)(((z >> 1) & 0x1FFC) | 0x6000);
                        break;
                    case 0x78:
                    case 0x79:
                    case 0x7a:
                    case 0x7b:
                        altmem = (ushort)((z & 0x1FFC) | 0x8000);
                        break;
                    case 0x7c:
                    case 0x7d:
                        altmem = (ushort)(((z << 1) & 0x1FFC) | 0xA000);
                        break;
                    case 0x7e:
                        altmem = (ushort)(((z << 2) & 0x1FFC) | 0xC000);
                        break;
                    case 0x7f:
                        altmem = (ushort)(((z << 2) & 0x1FFC) | 0xE000);
                        break;
                    default:
                        throw new RdpException("Building Z com table failed");
                }

                ZComTable[z] = altmem;
            }


            // ZCompleteDecTable

            uint exponent;
            uint mantissa;
            for (int i = 0; i < 0x4000; i++)
            {
                exponent = (uint)((i >> 11) & 7);
                mantissa = (uint)(i & 0x7FF);
                ZCompleteDecTable[i] = ((mantissa << ZDecTable[exponent].shift) + ZDecTable[exponent].add) & 0x3FFFF;
            }

            // DeltaZComparatorLut

            DeltaZComparatorLut[0] = 0;

            for (int i = 1; i < 0x10000; i++)
            {
                int k;
                for (k = 15; k >= 0; k--)
                {
                    if (i.IsTrue(1 << k))
                    {
                        DeltaZComparatorLut[i] = (ushort)(1 << k);
                        break;
                    }
                }
            }
        }

        public static uint ZDecompress(uint zb)
        {
            return ZCompleteDecTable[(zb >> 2) & 0x3FFF];
        }

        public static void ZStore(Rdram rdram, uint zcurpixel, uint z, int dzpixenc)
        {
            ushort zval = (ushort)(ZComTable[z & 0x3FFFF] | (dzpixenc >> 2));
            byte hval = (byte)(dzpixenc & 3);
            rdram.WritePair16(zcurpixel, zval, hval);
        }

        public static uint DzDecompress(uint dzCompressed)
        {
            return 1U << (int)dzCompressed;
        }

        public static uint DzCompress(uint value)
        {
            uint j = 0;

            if (value.IsTrue(0xFF00))
            {
                j |= 8;
            }

            if (value.IsTrue(0xF0F0))
            {
                j |= 4;
            }

            if (value.IsTrue(0xCCCC))
            {
                j |= 2;
            }

            if (value.IsTrue(0xAAAA))
            {
                j |= 1;
            }

            return j;
        }

        public static bool ZCompare(MadCatRdp rdp, uint zcurpixel, uint sz, ushort dzpix, int dzpixenc, ref bool enableBlend, ref int prewrap, ref uint curpixel_cvg, uint curpixel_memcvg)
        {
            var state = rdp.GraphicsState;

            bool forceCoplanar = false;

            sz &= 0x3FFFF;

            byte hval = 0;
            ushort zval = 0;
            uint oz;
            uint dzmem;

            if (state.OtherModes.EnableZCompare)
            {
                rdp.RdramMemory.ReadPair16(ref zval, ref hval, zcurpixel);
                oz = ZDecompress(zval);
                int rawdzmem = ((zval & 3) << 2) | hval;
                dzmem = DzDecompress((uint)rawdzmem);

                if (rdp.RdpBlender.RealShifterNeeded)
                {
                    rdp.RdpBlender.ShiftA = Utils.Clamp(dzpixenc - rawdzmem, 0, 4);
                    rdp.RdpBlender.ShiftB = Utils.Clamp(rawdzmem - dzpixenc, 0, 4);
                }

                if (rdp.RdpBlender.InterPixelShifterNeeded)
                {
                    rdp.RdpBlender.PastShiftA = Utils.Clamp(dzpixenc - state.PastRawDZmem, 0, 4);
                    rdp.RdpBlender.PastShiftB = Utils.Clamp(state.PastRawDZmem - dzpixenc, 0, 4);
                }

                state.PastRawDZmem = rawdzmem;

                int precisionFactor = (zval >> 13) & 0xF;

                if (precisionFactor < 3)
                {
                    if (dzmem != 0x8000)
                    {
                        uint dzMemModifier = (uint)(16 >> precisionFactor);
                        dzmem <<= 1;

                        if (dzmem < dzMemModifier)
                        {
                            dzmem = dzMemModifier;
                        }
                    }
                    else
                    {
                        forceCoplanar = true;
                        dzmem = 0xFFFF;
                    }
                }

                uint dznew = DeltaZComparatorLut[dzpix | dzmem];
                uint dznotshift = dznew;

                dznew <<= 3;

                bool farther = forceCoplanar || ((sz + dznew) >= oz);

                int overflow = (int)((curpixel_memcvg + curpixel_cvg) & 8);

                enableBlend = state.OtherModes.EnableForceBlend || (overflow == 0 && state.OtherModes.EnableAntiAliasing && farther);
                prewrap = overflow;
                int diff;
                bool nearer, max, infront;

                switch (state.OtherModes.ZMode)
                {
                    case ZMODE_OPAQUE:
                        {
                            infront = sz < oz;
                            diff = (int)sz - (int)dznew;
                            nearer = forceCoplanar || (diff <= (int)oz);
                            max = oz == 0x3FFFF;
                            return max || (overflow != 0 ? infront : nearer);
                        }

                    case ZMODE_INTERPENETRATING:
                        {
                            infront = sz < oz;

                            if (!infront || !farther || overflow == 0)
                            {
                                diff = (int)sz - (int)dznew;
                                nearer = forceCoplanar || (diff <= (int)oz);
                                max = oz == 0x3FFFF;
                                return max || (overflow != 0 ? infront : nearer);
                            }
                            else
                            {
                                uint dzenc = DzCompress(dznotshift & 0xFFFF);
                                int cvgcoeff = (int)(((oz >> (int)dzenc) - (sz >> (int)dzenc)) & 0xF);
                                curpixel_cvg = (uint)(((cvgcoeff * curpixel_cvg) >> 3) & 0xF);
                                return true;
                            }
                        }

                    case ZMODE_TRANSPARENT:
                        {
                            infront = sz < oz;
                            max = oz == 0x3FFFF;
                            return infront || max;
                        }

                    case ZMODE_DECAL:
                        {
                            diff = (int)sz - (int)dznew;
                            nearer = forceCoplanar || (diff <= (int)oz);
                            max = oz == 0x3FFFF;
                            return farther && nearer && !max;
                        }

                    default: return false;
                }
            }
            else
            {
                if (rdp.RdpBlender.RealShifterNeeded)
                {
                    rdp.RdpBlender.ShiftA = 0;

                    if (dzpixenc < 0xB)
                    {
                        rdp.RdpBlender.ShiftB = 4;
                    }
                    else
                    {
                        rdp.RdpBlender.ShiftB = 0xF - dzpixenc;
                    }
                }

                if (rdp.RdpBlender.InterPixelShifterNeeded)
                {
                    rdp.RdpBlender.PastShiftA = 0;

                    if (dzpixenc < 0xB)
                    {
                        rdp.RdpBlender.PastShiftB = 4;
                    }
                    else
                    {
                        rdp.RdpBlender.PastShiftB = 0xF - dzpixenc;
                    }
                }

                state.PastRawDZmem = 0xF;

                int overflow = (int)((curpixel_memcvg + curpixel_cvg) & 8);
                enableBlend = state.OtherModes.EnableForceBlend || (overflow == 0 && state.OtherModes.EnableAntiAliasing);
                prewrap = overflow;

                return true;
            }
        }
    }
}