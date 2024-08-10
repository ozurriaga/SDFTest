#ifndef RAY_MARCHING_LIBRARY_HLSDF_INCLUDED
#define RAY_MARCHING_LIBRARY_HLSDF_INCLUDED

#include "UnityCG.cginc"
#include "RayMarchingShaderLibrarySDF.cginc"
#include "RayMarchingShaderLibraryDataTypes.cginc"

#define RMVolumeTypeNone 0
#define RMVolumeTypeSphere 1
float RMVSphere(in float3 p, in float3 scale) { return RMDEllipsoid(p, scale); }

#define RMVolumeTypePlane 2
float RMVPlane(in float3 p) { return RMDPlaneY(p); }

#define RMVolumeTypeBox 3
float RMVBox(in float3 p, in float3 scale) { return RMDBox(p, scale); }

#define RMVolumeTypeBoxFrame 4
float RMVBoxFrame(in float3 p, in float3 scale, float frameSize) { return RMDBoxFrame(p, scale, frameSize); }

#define RMVolumeTypeCylinder 5
float RMVCylinder(in float3 p, in float3 scale) { return RMDCylinder(p, scale); }

#define RMVolumeTypeTorus 6
float RMVTorus(in float3 p, in float3 scale, in float sectionRadius) { return RMDTorus(p, scale, 0.5-sectionRadius, sectionRadius);  }

#define RMVolumeTypeCappedTorus 7
float RMVCappedTorus(in float3 p, in float3 scale, in float2 params) { return RMDCappedTorus(p, scale, float2(sin(params.y), cos(params.y)), 0.5 - params.x, params.x); }

#define RMVolumeTypeLink 8
float RMVLink(in float3 p, in float3 scale, in float sectionRadius) { return RMDLink(p, scale, 0.5 - sectionRadius, sectionRadius); }

#define RMVolumeTypeCone 9
float RMVCone(in float3 p, in float3 scale) { return RMDCone(p, scale, float2(0.5f, -1.0f)); }

#define RMVolumeTypeCapsule 10
float RMVCapsule(in float3 p, in float3 scale) { return RMDVerticalCapsule(p, scale); }

#define RMVolumeTypeSDF 20

#define RMVolumeTypeReference 50

#endif