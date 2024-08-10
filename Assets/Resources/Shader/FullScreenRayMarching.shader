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
            uniform float _SceneStartObject;

#define MAX_DISTANCE _FarPlane
#define MAX_OBJECTS 100
#define ROOT_OBJECT_PARENT_INDEX -1

            //Buffers
            PARAMS_BUFFER(RMVolumeData)
            PARAMS_BUFFER(RMOperationData)
            PARAMS_BUFFER(int)
            PARAMS_BUFFER(float)
            PARAMS_BUFFER(float2)
            PARAMS_BUFFER(float3)
            
            //UNITY_DECLARE_TEX2DARRAY(_Materials);

            class MySurfaceDistanceProvider : SurfaceDistanceProvider
            {
                float GetSurfaceDistanceToObject(float3 p, RMVolumeData objectData, float4x4 transform)
                {
                    int paramsId = objectData.paramsId;

                    transform = mul(objectData.worldToObjectMatrix, transform);
                    //iTransform = objectData.objectToWorldMatrix * iTransform;

                    float3 lp = mul(transform, float4(p, 1.0));
                    float lpl = length(lp);
                    float3 scale = objectData.scale;
                    float3 slp = lp * scale;
                    float lds = MAX_DISTANCE;

                    bool ldsIsWorldSpace = true;

                    switch (objectData.volumeType)
                    {
                        case RMVolumeTypeNone: break;
                        case RMVolumeTypeSphere: lds = RMVSphere(slp, scale); break;
                        case RMVolumeTypePlane: lds = RMVPlane(slp); break;
                        case RMVolumeTypeBox: lds = RMVBox(slp, scale); break;
                        case RMVolumeTypeBoxFrame: lds = RMVBoxFrame(slp, scale, BoxFrameParameters(paramsId)); break;
                        case RMVolumeTypeCylinder: lds = RMVCylinder(slp, scale); break;
                        case RMVolumeTypeTorus: lds = RMVTorus(slp, scale, TorusParameters(paramsId)); break;
                        case RMVolumeTypeCappedTorus: lds = RMVCappedTorus(slp, scale, CappedTorusParameters(paramsId)); break;
                        case RMVolumeTypeLink: lds = RMVLink(slp, scale, TorusParameters(paramsId));  break;
                        case RMVolumeTypeCone: lds = RMVCone(slp, scale); break;
                        case RMVolumeTypeCapsule: lds = RMVCapsule(slp, scale); break;
                        case RMVolumeTypeSDF: break;
                        default: break;
                    }
                    return lds;
                    //return (ldsIsWorldSpace) ? lds : length(mul(iTransform, float4(lp / lpl, 0.0f))) * lds;
                }

                float Combine(float previousDs, float currentDs, RMOperationData operationData)
                {
                    switch (operationData.type)
                    {
                        case RMOperationTypeSub:
                            return lerp(previousDs, RMSub(previousDs, currentDs, operationData.softness), operationData.blend);
                        
                        case RMOperationTypeInt:
                            return lerp(previousDs, RMInt(previousDs, currentDs, operationData.softness), operationData.blend);
                        
                        case RMOperationTypeAdd:
                        default:
                            //HOLLOW EFFECT
                            //currentDs = abs(currentDs) - 0.01f;

                            return lerp(previousDs, RMAdd(previousDs, currentDs, operationData.softness), operationData.blend);
                    }
                }

                SurfaceDistanceOutput GetSurfaceDistance(float3 p)
                {
                    SurfaceDistanceOutput o;
                    o.ds = MAX_DISTANCE;
                    o.m = 0;
                    o.uv = -1;

                    STACK(branchElements, int);
                    STACK(branchResults, float);
                    STACK(firstOperationIndex, int);
                    STACK(referenceIterator, int);
                    STACK(referenceElements, int);
                    STACK(referenceMatrix, float4x4);
                    
                    STACK_PUSH(referenceMatrix, IdentityMatrix);
                    
                    int surfaceObject = 0;
                    float closerDistanceToSurface = MAX_DISTANCE;
                    float branchResult = MAX_DISTANCE;
                    bool hasToPushFirstOperation = false;
                    
                    RMOperationData defaultAddOperation;
                    defaultAddOperation.type = RMOperationTypeAdd;
                    defaultAddOperation.info = 0;
                    defaultAddOperation.blend = 1;
                    defaultAddOperation.softness = 0;

                    for (int i = _SceneStartObject; i < SBCount(RMOperationData); i++)
                    {
                        if (!STACK_IS_EMPTY(branchElements)) STACK_TOP(branchElements)--;
                        if (!STACK_IS_EMPTY(referenceElements)) STACK_TOP(referenceElements)--;

                        RMOperationData operationData = SB(RMOperationData)[i];
                        if (operationData.type == RMOperationTypeGroup)
                        {
                            if (operationData.info > 0)
                            {
                                STACK_PUSH(branchElements, operationData.info);
                                STACK_PUSH(branchResults, branchResult);
                                branchResult = MAX_DISTANCE;
                                hasToPushFirstOperation = true;
                                if (!STACK_IS_EMPTY(referenceElements)) STACK_TOP(referenceElements) += operationData.info;
                            }
                        }
                        else
                        {
                            RMVolumeData objectData = SB(RMVolumeData)[operationData.info];

                            if (objectData.volumeType == RMVolumeTypeReference)
                            {
                                STACK_PUSH(referenceIterator, i);
                                STACK_PUSH(referenceElements, 1);
                                float4x4 previousTopMatrix = STACK_TOP(referenceMatrix);
                                STACK_PUSH(referenceMatrix, mul(objectData.worldToObjectMatrix, previousTopMatrix));
                                i = ReferenceParameters(objectData.paramsId) - 1;
                            }
                            else
                            {
                                float currentResult = GetSurfaceDistanceToObject(p, objectData, STACK_TOP(referenceMatrix));

                                if (hasToPushFirstOperation)
                                {
                                    hasToPushFirstOperation = false;
                                    STACK_PUSH(firstOperationIndex, i);
                                    branchResult = currentResult;
                                }
                                else
                                {
                                    branchResult = Combine(branchResult, currentResult, operationData);
                                }

                                [loop] while (!STACK_IS_EMPTY(branchElements) && STACK_TOP(branchElements) <= 0)
                                {
                                    float previousBranchResult = STACK_POP(branchResults);
                                    if (STACK_IS_EMPTY(referenceElements) || STACK_TOP(referenceElements) > 0)
                                    {
                                        if (!STACK_IS_EMPTY(firstOperationIndex))
                                        {
                                            branchResult = Combine(previousBranchResult, branchResult, SB(RMOperationData)[STACK_POP(firstOperationIndex)]);
                                        }
                                        else
                                        {
                                            branchResult = Combine(previousBranchResult, branchResult, defaultAddOperation);
                                        }
                                    }
                                    else
                                    {
                                        i = STACK_POP(referenceIterator);
                                        branchResult = Combine(previousBranchResult, branchResult, SB(RMOperationData)[i]);
                                        STACK_POP(referenceElements);
                                        STACK_POP(referenceMatrix);
                                    }
                                    STACK_POP(branchElements);
                                }
                            }
                        }
                    }
                    
                    RMVolumeData surfaceObjectData = SB(RMVolumeData)[surfaceObject];
                    o.ns = surfaceObjectData.normalSharpness;
                    o.ds = branchResult;

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
                float3 worldNormal = SimpleGetNormal(rayHit, surfaceDistanceProvider, sdro.surface.ns); // 5 is normal Sharpness

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
