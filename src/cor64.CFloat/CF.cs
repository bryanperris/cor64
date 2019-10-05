using System;
using System.Runtime.InteropServices;

namespace cor64.CFloat
{
    internal static class CF
    {
        [DllImport("cf.dll")]
        public static extern bool has_errors();

        [DllImport("cf.dll")]
        public static extern int has_flag_dz();

        [DllImport("cf.dll")]
        public static extern int has_flag_ie();

        [DllImport("cf.dll")]
        public static extern int has_flag_iv();

        [DllImport("cf.dll")]
        public static extern int has_flag_uf();

        [DllImport("cf.dll")]
        public static extern int has_flag_of();

        [DllImport("cf.dll")]
        public static extern void set_rounding_nearest();

        [DllImport("cf.dll")]
        public static extern void set_rounding_zero();

        [DllImport("cf.dll")]
        public static extern void set_rounding_up();

        [DllImport("cf.dll")]
        public static extern void set_rounding_down();

        [DllImport("cf.dll")]
        public static extern float add_s(float a, float b);

        [DllImport("cf.dll")]
        public static extern double add_d(double a, double b);

        [DllImport("cf.dll")]
        public static extern float sub_s(float a, float b);

        [DllImport("cf.dll")]
        public static extern double sub_d(double a, double b);

        [DllImport("cf.dll")]
        public static extern float mul_s(float a, float b);

        [DllImport("cf.dll")]
        public static extern double mul_d(double a, double b);

        [DllImport("cf.dll")]
        public static extern float div_s(float a, float b);

        [DllImport("cf.dll")]
        public static extern double div_d(double a, double b);

        [DllImport("cf.dll")]
        public static extern float sqrt_s(float a);

        [DllImport("cf.dll")]
        public static extern double sqrt_d(double a);

        [DllImport("cf.dll")]
        public static extern float ceil_s(float a);

        [DllImport("cf.dll")]
        public static extern double ceil_d(double a);

        [DllImport("cf.dll")]
        public static extern float floor_s(float a);

        [DllImport("cf.dll")]
        public static extern double floor_d(double a);

        [DllImport("cf.dll")]
        public static extern float trunc_s(float a);

        [DllImport("cf.dll")]
        public static extern double trunc_d(double a);

        [DllImport("cf.dll")]
        public static extern float round_s(float a);

        [DllImport("cf.dll")]
        public static extern double round_d(double a);
    }
}
