using System;
using NLog;

namespace cor64.Rdp.LLE
{
    public class Tile
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public int Format;
        public int Size;
        public int Line;
        public int TmemAddress;
        public int Palette;
        public TileCoordProperties S;
        public TileCoordProperties T;
        public int RdpNonTlutSwitch;
        public int RdpTlutSwitch;

        public void Reset() {
            Format = 0;
            Size = 0;
            Line = 0;
            TmemAddress = 0;
            Palette = 0;
            RdpNonTlutSwitch = 0;
            RdpTlutSwitch = 0;
            S.Reset();
            T.Reset();
        }

        public void Mask(ref int s, ref int t)
        {
            if (S.Mask != 0)
            {
                if (S.UseMirroring)
                {
                    int wrap = s >> S.RdpClampedMask;
                    wrap &= 1;
                    s ^= -wrap;
                }

                s &= TexCoord.LookupMaskBits(S.Mask);
            }

            if (T.Mask != 0)
            {
                if (T.UseMirroring)
                {
                    int wrap = t >> T.RdpClampedMask;
                    wrap &= 1;
                    t ^= -wrap;
                }

                t &= TexCoord.LookupMaskBits(T.Mask);
            }
        }

        public void ComputeClampDiffs()
        {
            S.RdpClampDiff = ((S.CoordHigh >> 2) - (S.CoordLow >> 2)) & 0x3FF;
            T.RdpClampDiff = ((T.CoordHigh >> 2) - (T.CoordLow >> 2)) & 0x3FF;
        }

        public void ComputeDerivs()
        {
            S.RdpUseClamping = S.UseClamping || S.Mask == 0;
            T.RdpUseClamping = T.UseClamping || T.Mask == 0;

            S.RdpClampedMask = S.Mask <= 10 ? S.Mask : 10;
            T.RdpClampedMask = T.Mask <= 10 ? T.Mask : 10;

            if (Format < 5)
            {
                RdpNonTlutSwitch = (Format << 2) | Size;
                RdpTlutSwitch = (Size << 2) | ((Format + 2) & 3);
            }
            else
            {
                RdpNonTlutSwitch = 0x10 | Size;
                RdpTlutSwitch = (Size << 2) | 2;
            }

            // Log.Debug("Tile Tlut Format: {0}", RdpTlutSwitch);
            // Log.Debug("Tile Texel Format: {0}", (TexelFormat)RdpNonTlutSwitch);
        }
    }

    public struct TileCoordProperties {
        public bool UseClamping;
        public bool UseMirroring;
        public int Mask;
        public int Shift;
        public int CoordLow;
        public int CoordHigh;
        public int RdpClampDiff;
        public bool RdpUseClamping;
        public int RdpClampedMask;

        public void Reset() {
            UseClamping = false;
            UseMirroring = false;
            Mask = 0;
            Shift = 0;
            CoordLow = 0;
            CoordHigh = 0;
            RdpClampDiff = 0;
            RdpUseClamping = false;
            RdpClampedMask = 0;
        }

        public override String ToString() {
            return String.Format("{0}, {1}, {2}, {3}, {4}, {5}", UseClamping, UseMirroring, Mask, Shift, CoordLow, CoordHigh);
        }
    }
}