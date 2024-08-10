#if false

Shader "Custom/FullScreenRayMarching"
{
    Properties { }

    CGINCLUDE
        #include "AutoLight.cginc"
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityLightingCommon.cginc"
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue" = "Geometry" "LightMode" = "ForwardBase" }
        LOD 100
        ZTest Always
        
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 5.0
            #pragma require 2darray

            #define LIGHT_DISTANCE 100

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 rayDir : TEXCOORD1;
            };

            struct fragOutput
            {
                fixed4 color : SV_Target;
                float depth : SV_Depth;
            };

            #include "Library/RayMarchingShaderLibrary.cginc"

            uniform sampler2D _MainTex;
            uniform sampler2D _CameraDepthTexture;
            uniform float4 _MainTex_ST;
            uniform float4x4 _CameraFrustumCornersMatrixCS;
            uniform float _FarPlane;
            uniform int _MaxSteps;
            uniform float _SurfaceDistance;

#define MAX_DISTANCE _FarPlane
#define MAX_OBJECTS 100
#define ROOT_OBJECT_PARENT_INDEX -1

            //Buffers
            PARAMS_BUFFER(RMObjectData)
            PARAMS_BUFFER(int)
            PARAMS_BUFFER(int2)
            PARAMS_BUFFER(float)
            PARAMS_BUFFER(float2)
            PARAMS_BUFFER(float3)
            
            //UNITY_DECLARE_TEX2DARRAY(_Materials);

            class MySurfaceDistanceProvider : SurfaceDistanceProvider
            {
                float GetSurfaceDistanceToObject(float3 p, RMObjectData objectData)
                {
                    int paramsId = objectData.paramsId;

                    float4x4 transform = objectData.worldToObjectMatrix;
                    float4x4 iTransform = objectData.objectToWorldMatrix;

                    float3 lp = mul(transform, float4(p, 1.0));
                    float lpl = length(lp);
                    float3 scale = objectData.scale;
                    float3 slp = lp * scale;
                    float lds = MAX_DISTANCE;

                    bool ldsIsWorldSpace = true;

                    switch (objectData.volumeType)
                    {
                        case RMVolumeTypeNone: break;
                        case RMVolumeTypeSphere: lds = RMVSphere(slp, objectData.scale); break;
                        case RMVolumeTypePlane: lds = RMVPlane(slp); break;
                        case RMVolumeTypeBox: lds = RMVBox(slp, scale); break;
                        case RMVolumeTypeBoxFrame: lds = RMVBoxFrame(slp, scale, BoxFrameParameters(paramsId)); break;
                        case RMVolumeTypeCylinder: lds = RMVCylinder(slp, scale); break;
                        case RMVolumeTypeTorus: lds = RMVTorus(slp, scale, TorusParameters(paramsId)); break;
                        case RMVolumeTypeCappedTorus: lds = RMVCappedTorus(slp, scale, CappedTorusParameters(paramsId)); break;
                        case RMVolumeTypeLink: lds = RMVLink(slp, scale, TorusParameters(paramsId));  break;
                        case RMVolumeTypeCone: lds = RMVCone(slp, scale); break;
                        case RMVolumeTypeCapsule: lds = RMVCapsule(slp, scale); break;
                        case RMVolumeTypeReference: break;
                        case RMVolumeTypeSDF: break;
                        default: break;
                    }

                    //HOLLOW EFFECT
                    /*if (objectData.operation == RMOperationTypeAdd)
                    {
                        lds = abs(lds) - 0.01f;
                    }*/
                    return (ldsIsWorldSpace) ? lds : length(mul(iTransform, float4(lp / lpl, 0.0f))) * lds;
                }

                float Combine(float previousDs, float currentDs, RMObjectData objectData)
                {
                    switch (objectData.operation)
                    {
                        case RMOperationTypeSub:
                        //case RMOperationTypeRSub:
                            return lerp(previousDs, RMSub(previousDs, currentDs, objectData.operationSoftness), objectData.operationBlend);
                        
                        case RMOperationTypeInt:
                        //case RMOperationTypeRInt:
                            return  lerp(previousDs, RMInt(previousDs, currentDs, objectData.operationSoftness), objectData.operationBlend);
                        
                        case RMOperationTypeAdd:
                        //case RMOperationTypeRAdd:
                        default:
                            return lerp(previousDs, RMAdd(previousDs, currentDs, objectData.operationSoftness), objectData.operationBlend);
                    }
                }

                SurfaceDistanceOutput GetSurfaceDistance(float3 p)
                {
                    SurfaceDistanceOutput o;
                    o.ds = MAX_DISTANCE;
                    o.m = 0;
                    o.uv = -1;

                    int surfaceObject = 0;
                    float closerDistanceToSurface = MAX_DISTANCE;
                    float globalResult = MAX_DISTANCE;
                    float branchResult = MAX_DISTANCE;
                    int previousParent = ROOT_OBJECT_PARENT_INDEX;
                    for (int i = 1; i < SBCount(RMObjectData); i++)
                    {
                        RMObjectData objectData = SB(RMObjectData)[i];
                        float currentResult = GetSurfaceDistanceToObject(p, objectData);
                        if (abs(currentResult) < closerDistanceToSurface)
                        {
                            closerDistanceToSurface = abs(currentResult);
                            surfaceObject = i;
                        }

                        /*if (previousParent == objectData.parentIndex)
                        {*/
                            branchResult = Combine(branchResult, currentResult, objectData);
                        /*}
                        else
                        {
                            branchResult = Combine(currentResult, branchResult, objectData);
                        }*/

                        if (IS_ROOT_OBJECT(objectData))
                        {
                            globalResult = Combine(globalResult, branchResult, objectData);
                            branchResult = MAX_DISTANCE;
                        }

                        previousParent = objectData.parentIndex;
                    }

                    o.ds = globalResult;

                    return o;
                }
            };


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.rayDir = _CameraFrustumCornersMatrixCS[round(o.uv.x) + 2 * (1 - round(o.uv.y))];
                return o;
            }

            fragOutput frag(v2f i)
            {
                MySurfaceDistanceProvider surfaceDistanceProvider;
                fragOutput o;
                
                float rayMagnitude = length(i.rayDir.xyz);

                float3 ray = i.rayDir.xyz / rayMagnitude;

                float4 originalColor = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
                float originalLinearDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r) * rayMagnitude;

                float3 rayOrigin = _WorldSpaceCameraPos;
                float maxDist = min(originalLinearDepth, MAX_DISTANCE);

                SDRayMarchOutput sdro = MaterialRayMarch(rayOrigin, ray, surfaceDistanceProvider, maxDist, _MaxSteps, _SurfaceDistance);
                float d = sdro.d;

                if (d >= maxDist - _SurfaceDistance)
                {
                    o.color = originalColor;
                    o.color.a = 1;
                    o.depth = originalLinearDepth;
                    return o;
                }
                
                float3 rayHit = sdro.p;
                float3 worldNormal = SimpleGetNormal(rayHit, surfaceDistanceProvider, 5); // 5 is normal Sharpness

                /*if (sdro.surface.uv.x < 0)
                {
                    float3 absNormal = abs(worldNormal);

                    if (absNormal.x >= max(absNormal.y, absNormal.z))
                    {
                        sdro.surface.uv.x = frac(sdro.p.z);
                        sdro.surface.uv.y = frac(sdro.p.y);
                    } else if (absNormal.z >= max(absNormal.y, absNormal.x))
                    {
                        sdro.surface.uv.x = frac(sdro.p.x);
                        sdro.surface.uv.y = frac(sdro.p.y);
                    } else
                    {
                        if (absNormal.z >= absNormal.x)
                        {
                            sdro.surface.uv.x = frac(sdro.p.x);
                            sdro.surface.uv.y = frac(sdro.p.z);
                        }
                        else
                        {
                            sdro.surface.uv.x = frac(sdro.p.z);
                            sdro.surface.uv.y = frac(sdro.p.x);
                        }
                    }
                }

                fixed4 textureColor = UNITY_SAMPLE_TEX2DARRAY(_Materials, float3(sdro.surface.uv, sdro.surface.m));*/

                float3 light = 0.25;//ShadeSH9(half4(worldNormal, 1));
                float3 lightToPoint = normalize(-_WorldSpaceLightPos0.xyz);
                float3 lightPosition = rayHit - lightToPoint * LIGHT_DISTANCE;
                float lightD = MaterialRayMarch(lightPosition, lightToPoint, surfaceDistanceProvider, LIGHT_DISTANCE, _MaxSteps, _SurfaceDistance).d;
                float3 lightHitPoint = lightPosition + lightD * lightToPoint;
                float lightPointDiff = dot(rayHit - lightHitPoint, lightToPoint);
                float normalDot = (pow(1.2 + dot(lightToPoint, SimpleGetNormal(lightHitPoint, surfaceDistanceProvider, 1)), 0.1) - 1.2) * pow(1 + lightPointDiff, 1);
                float atten = 1;//LIGHT_ATTENUATION(i);

                float nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                nl = max(0, nl - abs(normalDot)) * atten;
                light += nl * _LightColor0;

                fixed4 col;
                //col.xyz = (float)sdro.s / (float)_MaxSteps;//light;// *textureColor;
                /*col.x = sdro.surface.ds;
                col.y = sdro.surface.ds >= 1 ? 1 : 0;*/
                //col.x = sdro.surface.n.x;
                //col.y = light;
                //col.z = (float)sdro.s / (float)_MaxSteps;
                col.xyz = light;
                //col.xyz = rayHit;
                //col.xyz = ray;
                //col.xyz = sdro.surface.n;
                col.a = 1;

                o.color = col;
                o.depth = mul(UNITY_MATRIX_VP, float4(rayHit, 1)).z;
                return o;
            }
            ENDCG
        }
    }
    Fallback Off
}
#endif