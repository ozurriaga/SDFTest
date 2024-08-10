#ifndef RAY_MARCHING_LIBRARY_INCLUDED
#define RAY_MARCHING_LIBRARY_INCLUDED

#include "UnityCG.cginc"
#include "RayMarchingShaderLibraryHLSDF.cginc"
#include "RayMarchingShaderLibraryDataTypes.cginc"

struct SurfaceDistanceOutput
{
    float ds;
    int m;
    float2 uv;
    float3 n;
    float ns;
};

struct SDRayMarchOutput
{
    float d;
    float3 p;
    int s;    
    SurfaceDistanceOutput surface;
};

struct RayMarchOutput
{
    float3 p;
    float d;
    int s;
};

interface DistanceProvider
{
    float GetDist(float3 p);
};

interface SurfaceDistanceProvider
{
    SurfaceDistanceOutput GetSurfaceDistance(float3 p);
};

float3 SimpleGetNormal(float3 p, DistanceProvider distanceProvider, float sharpness)
{
    float2 e = float2(0.15 / sharpness, 0); // Epsilon

    float3 n = float3(
        distanceProvider.GetDist(p + e.xyy) - distanceProvider.GetDist(p - e.xyy),
        distanceProvider.GetDist(p + e.yxy) - distanceProvider.GetDist(p - e.yxy),
        distanceProvider.GetDist(p + e.yyx) - distanceProvider.GetDist(p - e.yyx));

    n = normalize(n);

    return n;
}

float3 SimpleGetNormal(float3 p, SurfaceDistanceProvider distanceProvider, float sharpness)
{
    float2 e = float2(0.15 / max(sharpness, 0.001f), 0); // Epsilon

    float3 n = float3(
        distanceProvider.GetSurfaceDistance(p + e.xyy).ds - distanceProvider.GetSurfaceDistance(p - e.xyy).ds,
        distanceProvider.GetSurfaceDistance(p + e.yxy).ds - distanceProvider.GetSurfaceDistance(p - e.yxy).ds,
        distanceProvider.GetSurfaceDistance(p + e.yyx).ds - distanceProvider.GetSurfaceDistance(p - e.yyx).ds);

    n = normalize(n);

    return n;
}

float SimpleRayMarch(float3 origin, float3 rayDirection, DistanceProvider distanceProvider, float maxDistance, int maxSteps, float surfaceDist)
{
    float distanceToOrigin = 0;
    for (int i = 0; i < maxSteps && distanceToOrigin < maxDistance; i++)
    {
        float3 currentPoint = origin + rayDirection * distanceToOrigin;

        float distanceToSurface = distanceProvider.GetDist(currentPoint);
        distanceToOrigin += distanceToSurface;
        if (distanceToSurface < surfaceDist) return distanceToOrigin;
    }
    return maxDistance;
}

RayMarchOutput CompleteRayMarch(float3 origin, float3 rayDirection, DistanceProvider distanceProvider, float maxDistance, int maxSteps, float surfaceDist)
{
    RayMarchOutput o;
    o.d = 0;
    o.p = origin;
    for (o.s = 0; o.s < maxSteps && o.d < maxDistance; o.s++)
    {
        float distanceToSurface = distanceProvider.GetDist(o.p);
        o.d += distanceToSurface;
        o.p = origin + rayDirection * o.d;
        if (distanceToSurface < surfaceDist) return o;
    }
    o.d = maxDistance;
    return o;
}

SDRayMarchOutput MaterialRayMarch(float3 origin, float3 rayDirection, SurfaceDistanceProvider distanceProvider, float maxDistance, int maxSteps, float surfaceDist)
{
    SDRayMarchOutput o;
    o.p = origin;
    o.d = 0;
    o.surface.ds = 0;
    o.surface.m = -1;
    o.surface.uv = 0;
    o.surface.n = 0;

    float minDs = maxDistance;
    bool goneNegative = false;
    for (o.s = 0; (o.s < maxSteps) && (o.d < maxDistance); o.s++)
    {        
        o.surface = distanceProvider.GetSurfaceDistance(o.p);
        minDs = min(minDs, o.surface.ds);
        if (o.surface.ds <= 0) goneNegative = true;
        o.d += o.surface.ds * 0.5f;
        o.p = origin + rayDirection * o.d;
        //o.surface.n.x = minDs;
        if (abs(o.surface.ds) <= surfaceDist || (goneNegative && o.surface.ds > 0)) return o;
    }
    //o.d = maxDistance;
    return o;
}

#endif