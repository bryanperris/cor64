#include <stdio.h>
#include <fenv.h>
#include <math.h>

#pragma STDC FENV_ACCESS ON

#ifdef WIN32
    #define DLLEXPORT __declspec(dllexport)
#else
    #define DLLEXPORT
#endif

const int DEFAULT_ROUNDING = FE_TONEAREST;

int g_FpuFlags = 0;
int g_RoundMode = FE_TONEAREST;

double _internalRound(double x)
{
    fenv_t save_env;
    feholdexcept(&save_env);
    double result = rint(x);
    if (fetestexcept(FE_INEXACT)) {
        fesetround(FE_TOWARDZERO);
        result = rint(copysign(0.5 + fabs(x), x));
    }
    feupdateenv(&save_env);
    return result;
}

DLLEXPORT void set_rounding_nearest() {
    g_RoundMode = FE_TONEAREST;
}

DLLEXPORT void set_rounding_zero() {
    g_RoundMode = FE_TOWARDZERO;
}

DLLEXPORT void set_rounding_up() {
    g_RoundMode = FE_UPWARD;
}

DLLEXPORT void set_rounding_down() {
    g_RoundMode = FE_DOWNWARD;
}

DLLEXPORT int has_errors() {
    return g_FpuFlags != 0;
}

DLLEXPORT int has_flag_dz() {
    return (g_FpuFlags & FE_DIVBYZERO) == FE_DIVBYZERO;
}

DLLEXPORT int has_flag_ie() {
    return (g_FpuFlags & FE_INEXACT) == FE_INEXACT;
}

DLLEXPORT int has_flag_iv() {
    return (g_FpuFlags & FE_INVALID) == FE_INVALID;
}

DLLEXPORT int has_flag_uf() {
    return (g_FpuFlags & FE_UNDERFLOW) == FE_UNDERFLOW;
}

DLLEXPORT int has_flag_of() {
    return (g_FpuFlags & FE_OVERFLOW) == FE_OVERFLOW;
}

DLLEXPORT float add_s(float a, float b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    float r = a + b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT double add_d(double a, double b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    double r = a + b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT float div_s(float a, float b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    float r = a / b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT double div_d(double a, double b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    double r = a / b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT float mul_s(float a, float b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    float r = a * b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT double mul_d(double a, double b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    double r = a * b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT float sub_s(float a, float b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    float r = a - b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT double sub_d(double a, double b) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    double r = a - b;
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT float sqrt_s(float a) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    float r = sqrtf(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT double sqrt_d(double a) {
    fesetround(g_RoundMode);
    feclearexcept(FE_ALL_EXCEPT);
    double r = sqrt(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    fesetround(DEFAULT_ROUNDING);
    return r;
}

DLLEXPORT float ceil_s(float a) {
    feclearexcept(FE_ALL_EXCEPT);
    float r = ceilf(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT double ceil_d(double a) {
    feclearexcept(FE_ALL_EXCEPT);
    double r = ceil(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT float floor_s(float a) {
    feclearexcept(FE_ALL_EXCEPT);
    float r = floorf(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT double floor_d(double a) {
    feclearexcept(FE_ALL_EXCEPT);
    double r = floor(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT float round_s(float a) {
    feclearexcept(FE_ALL_EXCEPT);
    float r = _internalRound(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT double round_d(double a) {
    feclearexcept(FE_ALL_EXCEPT);
    double r = _internalRound(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT float trunc_s(float a) {
    feclearexcept(FE_ALL_EXCEPT);
    float r = truncf(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}

DLLEXPORT double trunc_d(double a) {
    feclearexcept(FE_ALL_EXCEPT);
    double r = trunc(a);
    g_FpuFlags = fetestexcept(FE_ALL_EXCEPT);
    feclearexcept(FE_ALL_EXCEPT);
    return r;
}