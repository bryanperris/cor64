using System.Runtime.CompilerServices;

namespace cor64.Mips.Rsp
{
    public static class RspVectorHelper {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RspVector Resolve(this RspVector vector, int e)
        {
            if (e != 0)
            {
                if ((e & 0xE) == 0x2)
                {
                    return RspVector.ScalarQuarter(vector, e);
                }
                else if ((e & 0xC) == 0x4)
                {
                    return RspVector.ScalarHalf(vector, e);
                }
                else if ((e & 0x8) == 0x8)
                {
                    return RspVector.ScalarWhole(vector, e);
                }
                else
                {
                    return vector;
                }
            }
            else
            {
                return vector;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreProductClampedSignedAM(this RspVector vector, Accumulator accumulator, int index) {
            // Thanks to cxd4

            int acc_h = accumulator.SignedHi(index);
            int acc_m = accumulator.SignedMi(index);

            bool hi = (acc_h >  0) || (acc_h == 0 && (acc_m <  0));
            bool lo = (acc_h < -1) || (acc_h <  0 && (acc_m >= 0));

            int result = acc_m;
            result &= -(lo.ToInt() ^ 1);
            result |= -(hi.ToInt() ^ 0);
            result ^= (hi || lo) ? 0x8000 : 0x0000;

            vector.PackedS16(index, (short)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreProductClampedSignedAL(this RspVector vector, Accumulator accumulator, int index) {
            // Thanks to cxd4

            StoreProductClampedSignedAM(vector, accumulator, index);

            int result = vector.PackedU16(index);
            int acc_m = accumulator.SignedMi(index);

            if (result != acc_m) {
                result ^= 0x8000;
            }
            else {
                result = accumulator.SignedLo(index);
            }

            vector.PackedS16(index, (short)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreProductClampedUnsigned(this RspVector vector, Accumulator accumulator, int index) {
            // Thanks to cxd4

            StoreProductClampedSignedAM(vector, accumulator, index);

            int acc_m = accumulator.SignedMi(index);
            int val   = vector.PackedS16(index);
            int result = val & ~(val >> 15);

            if (val > acc_m) {
                result |= 0xFFFF;
            }

            vector.PackedU16(index, (ushort)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(this bool value) {
            return value ? 1 : 0;
        }
    }
}