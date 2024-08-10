#ifndef RAY_MARCHING_LIBRARY_OP_INCLUDED
#define RAY_MARCHING_LIBRARY_OP_INCLUDED

#define RMOperationTypeGroup    0
#define RMOperationTypeAdd      1
#define RMOperationTypeSub      2
#define RMOperationTypeInt      3

#define IS_ROOT_OPERATION(op) ((op==RMOperationTypeRAdd)||(op==RMOperationTypeRSub)||(op==RMOperationTypeRInt))

float smin(float a, float b, float k)
{
    float h = max(k - abs(a - b), 0.0);
    return min(a, b) - h * h * 0.25 / k;
}

float smax(float a, float b, float k)
{
    k *= 1.4;
    float h = max(k - abs(a - b), 0.0);
    return max(a, b) + h * h * h / (6.0 * k * k);
}

float smin3(float a, float b, float k)
{
    k *= 1.4;
    float h = max(k - abs(a - b), 0.0);
    return min(a, b) - h * h * h / (6.0 * k * k);
}

float sclamp(in float x, in float a, in float b)
{
    float k = 0.1;
    return smax(smin(x, b, k), a, k);
}

float sabs(in float x, in float k)
{
    return sqrt(x * x + k);
}

float opOnion(in float sdf, in float thickness)
{
    return abs(sdf) - thickness;
}

float opRepLim(in float p, in float s, in float lima, in float limb)
{
    return p - s * clamp(round(p / s), lima, limb);
}

float det(float2 a, float2 b) { return a.x * b.y - b.x * a.y; }
float ndot(float2 a, float2 b) { return a.x * b.x - a.y * b.y; }
float dot2(in float2 v) { return dot(v, v); }
float dot2(in float3 v) { return dot(v, v); }

//SDF Operations

float RMAdd(float a, float b)
{
    return min(a, b);
}

float RMSub(float a, float b)
{
    return max(a, -b);
}

float RMInt(float a, float b)
{
    return max(a, b);
}

float RMOpp(float a, float b, float l, float oS) // oS -> 1 or -1 to determine operation
{
    float h = clamp(0.5 + oS * 0.5 * (b - a) / max(l, 0.00001), 0, 1);
    return lerp(b, a, h) - oS * l * h * (1 - h);
}

float RMAdd(float a, float b, float l)
{
    return RMOpp(a, b, l, 1);
}

float RMSub(float a, float b, float l)
{
    return RMOpp(-b, a, l, -1);
}

float RMInt(float a, float b, float l)
{
    return RMOpp(a, b, l, -1);
}

#endif