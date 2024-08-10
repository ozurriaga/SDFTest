#ifndef RAY_MARCHING_LIBRARY_SDF_INCLUDED
#define RAY_MARCHING_LIBRARY_SDF_INCLUDED

#include "UnityCG.cginc"
#include "RayMarchingShaderLibraryOP.cginc"

#define PI 3.1415925359
#define TWO_PI 6.2831852

const float4x4 IdentityMatrix =
{
    { 1, 0, 0, 0 },
    { 0, 1, 0, 0 },
    { 0, 0, 1, 0 },
    { 0, 0, 0, 1 }
};

//RMVolumeTypeSphere
float RMDEllipsoid(in float3 p, in float3 r)
{
    float k0 = length(p / r);
    float k1 = length(p / (r * r));
    return k0 * (k0 - 1.0) / k1;
}

//RMVolumeTypePlane
float RMDPlaneY(in float3 p)
{
    return p.y;
}

//RMVolumeTypeBox
float RMDBox(in float3 p, in float3 b)
{
    float3 d = abs(p) - b * 0.5f;
    return min(max(max(d.x, d.y), d.z), 0.0) + length(max(d, 0.0));
}

//RMVolumeTypeBoxFrame
float RMDBoxFrame(in float3 p, in float3 b, float e)
{
    p = abs(p) - b * 0.5f;
    float3 q = abs(p + e * 0.5f) - e * 0.5f;
    return min(min(
        length(max(float3(p.x, q.y, q.z), 0.0)) + min(max(p.x, max(q.y, q.z)), 0.0),
        length(max(float3(q.x, p.y, q.z), 0.0)) + min(max(q.x, max(p.y, q.z)), 0.0)),
        length(max(float3(q.x, q.y, p.z), 0.0)) + min(max(q.x, max(q.y, p.z)), 0.0));
}

//RMVolumeTypeCylinder - InTrance approach
float RMDCylinder(in float3 p, in float3 b)
{
    float dy = abs(p.y) - abs(b.y);
    b *= 0.5f;
    float k0 = length(p.xz / b.xz);
    float k1 = length(p.xz / (b.xz * b.xz));
    return max(k0 * (k0 - 1.0) / k1, dy);
}

//RMVolumeTypeTorus
float RMDTorus(in float3 p, in float3 scale, in float ra, in float rb)
{
    return length(float2(length(p.xz / scale.xz) - ra, p.y) / scale.xy) - rb;
}

//RMVolumeTypeCappedTorus
float RMDCappedTorus(in float3 p, in float3 scale, in float2 sc, in float ra, in float rb)
{
    p /= scale;
    p.x = abs(p.x);
    float k = (sc.y * p.x > sc.x * p.z) ? dot(p.xz, sc) : length(p.xz);
    return sqrt(dot(p, p) + (ra * ra) - (2.0 * ra * k)) - rb;
}

/*float sdArc(in float2 p, in float2 scb, in float ra)
{
    p.x = abs(p.x);
    float k = (scb.y * p.x > scb.x * p.y) ? dot(p.xy, scb) : length(p.xy);
    return sqrt(dot(p, p) + ra * ra - 2.0 * ra * k);
}*/

//RMVolumeTypeLink
float RMDLink(float3 p, float3 scale, float r1, float r2)
{
    p /= float3(scale.x, 1, scale.z);
    float3 q = float3(p.x, max(abs(p.y) - (scale.y * 0.5f - 0.5f), 0.0), p.z);

    return length(float2(length(q.xy) - (0.5 - r2), q.z)) - r2;
}

//RMVolumeTypeCone
float RMDCone(in float3 p, in float3 scale, in float2 c)
{
    p /= scale;
    p.y -= 0.5;
    float2 q = float2(length(p.xz), p.y);

    float2 a = q - c * clamp((q.x * c.x + q.y * c.y) / dot(c, c), 0.0, 1.0);
    float2 b = q - c * float2(clamp(q.x / c.x, 0.0, 1.0), 1.0);

    float s = -sign(c.y);
    float2 d = min(float2(dot(a, a), s * (q.x * c.y - q.y * c.x)),
        float2(dot(b, b), s * (q.y - c.y)));
    return -sqrt(d.x) * sign(d.y);
}

//RMVolumeTypeCapsule
float RMDVerticalCapsule(in float3 p, in float3 scale)
{
    p.y -= clamp(p.y, -(scale.y * 0.5f)+0.5f, scale.y*0.5f-0.5f);
    p.xz /= scale.xz;
    return length(p) - 0.5f;
}

/*#if 1
float4 sdBezier(float3 p, float3 va, float3 vb, float3 vc)
{
    float3 w = normalize(cross(vc - vb, va - vb));
    float3 u = normalize(vc - vb);
    float3 v = (cross(w, u));
    //----  
    float2 m = float2(dot(va - vb, u), dot(va - vb, v));
    float2 n = float2(dot(vc - vb, u), dot(vc - vb, v));
    float3 q = float3(dot(p - vb, u), dot(p - vb, v), dot(p - vb, w));
    //----  
    float mn = det(m, n);
    float mq = det(m, q.xy);
    float nq = det(n, q.xy);
    //----  
    float2  g = (nq + mq + mn) * n + (nq + mq - mn) * m;
    float f = (nq - mq + mn) * (nq - mq + mn) + 4.0 * mq * nq;
    float2  z = 0.5 * f * float2(-g.y, g.x) / dot(g, g);
    //float t = clamp(0.5+0.5*(det(z,m+n)+mq+nq)/mn, 0.0 ,1.0 );
    float t = clamp(0.5 + 0.5 * (det(z - q.xy, m + n)) / mn, 0.0, 1.0);
    float2 cp = m * (1.0 - t) * (1.0 - t) + n * t * t - q.xy;
    //----  
    float d2 = dot(cp, cp);
    return float4(sqrt(d2 + q.z * q.z), t, q.z, -sign(f) * sqrt(d2));
}
#else
float det(float3 a, float3 b, in float3 v) { return dot(v, cross(a, b)); }

float4 sdBezier(float3 p, float3 b0, float3 b1, float3 b2)
{
    b0 -= p;
    b1 -= p;
    b2 -= p;

    float3  d21 = b2 - b1;
    float3  d10 = b1 - b0;
    float3  d20 = (b2 - b0) * 0.5;

    float3  n = normalize(cross(d10, d21));

    float a = det(b0, b2, n);
    float b = det(b1, b0, n);
    float d = det(b2, b1, n);
    float3  g = b * d21 + d * d10 + a * d20;
    float f = a * a * 0.25 - b * d;

    float3  z = cross(b0, n) + f * g / dot(g, g);
    float t = clamp(dot(z, d10 - d20) / (a + b + d), 0.0, 1.0);
    float3 q = lerp(lerp(b0, b1, t), lerp(b1, b2, t), t);

    float k = dot(q, n);
    return float4(length(q), t, -k, -sign(f) * length(q - n * k));
}
#endif

float2 sdSegment(float3 p, float3 a, float3 b)
{
    float3 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return float2(length(pa - ba * h), h);
}

float2 sdSegmentOri(float2 p, float2 b)
{
    float h = clamp(dot(p, b) / dot(b, b), 0.0, 1.0);
    return float2(length(p - b * h), h);
}

float sdFakeRoundCone(float3 p, float b, float r1, float r2)
{
    float h = clamp(p.y / b, 0.0, 1.0);
    p.y -= b * h;
    return length(p) - lerp(r1, r2, h);
}*/

/*float sdRhombus(float3 p, float la, float lb, float h, float ra)
{
    p = abs(p);
    float2 b = float2(la, lb);
    float f = clamp((ndot(b, b - 2.0 * p.xz)) / dot(b, b), -1.0, 1.0);
    float2 q = float2(length(p.xz - 0.5 * b * float2(1.0 - f, 1.0 + f)) * sign(p.x * b.y + p.z * b.x - b.x * b.y) - ra, p.y - h);
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0));
}

float4 opElongate(in float3 p, in float3 h)
{
    float3 q = abs(p) - h;
    return float4(max(q, 0.0), min(max(q.x, max(q.y, q.z)), 0.0));
}

float2 iConeY(in float3 ro, in float3 rd, in float k)
{
    float a = dot(rd.xz, rd.xz) - k * rd.y * rd.y;
    float b = dot(ro.xz, rd.xz) - k * ro.y * rd.y;
    float c = dot(ro.xz, ro.xz) - k * ro.y * ro.y;

    float h = b * b - a * c;
    if (h < 0.0) return float2(-1.0, -1.0);
    h = sqrt(h);
    return float2(-b - h, -b + h) / a;
}*/

#endif