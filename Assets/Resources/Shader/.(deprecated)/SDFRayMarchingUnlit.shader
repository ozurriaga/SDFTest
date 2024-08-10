#if false

// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced 'unity_World2Shadow' with 'unity_WorldToShadow'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/SDFRayMarchingUnlit"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
        _Sharpness("Sharpness", Range(1, 50)) = 1
        _ShadowSharpness("ShadowSharpness", Range(0.1, 5)) = 1
    }

    CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityLightingCommon.cginc"
    ENDCG
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "LightMode" = "ForwardBase"  }
        LOD 100
        //ZTest Always
        //Blend One OneMinusSrcAlpha

        Pass
        {
            Lighting On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            // Constants
            #define PI 3.1415925359
            #define TWO_PI 6.2831852
            #define MAX_STEPS 100
            #define MAX_DIST 100.
            #define SURFACE_DIST .01

            #include "AutoLight.cginc"
            #include "Library/RayMarchingShaderLibrary.cginc"

            uniform sampler3D _MainTex;
            uniform sampler2D _CameraDepthTexture;
            uniform float _Sharpness;
            uniform float _ShadowSharpness;
        
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                fixed4     lightDirection : TEXCOORD2;
                fixed3     viewDirection : TEXCOORD3;
                LIGHTING_COORDS(4, 6)
            };

            struct fragOutput
            {
                fixed4 color : SV_Target;
                float depth : SV_Depth;
            };

            float GetDist(float3 p)
            {
                float3 s = float3(0, 0, 0);
                float3 s2 = float3(0, 0, 0.5);
                float3 s3 = float3(0, 0.25, 0.25);
                float sphereDist = length(p - s.xyz) - 0.5;

                float sphereDist2 = length(p - s2.xyz) - 0.5;

                float sphereDist3 = length(p - s3.xyz) - 0.25;
                
                p += 0.5;

                p.y += sin(p.x * 30 + _Time.w) * 0.05;
                p.x += sin(p.y * 100 + _Time.w) * 0.05;

                return RMAdd(RMSub(sphereDist, sphereDist2), sphereDist3);
                
                /*if (p.x > 1 || p.y > 1 || p.z > 1 ||
                    p.x < 0 || p.y < 0 || p.z < 0)
                    return MAX_DIST;*/



                //return Sub((tex3Dlod(_MainTex, float4(p, 0)).x) * 0.5 - 0.002, sphereDist);
            }

            float3 GetNormal(float3 p)
            {
                float2 e = float2(0.15 / _Sharpness, 0); // Epsilon

                float3 op = mul(unity_WorldToObject, float4(p, 1));

                float3 n = float3(
                    GetDist(op + e.xyy) - GetDist(op - e.xyy),
                    GetDist(op + e.yxy) - GetDist(op - e.yxy),
                    GetDist(op + e.yyx) - GetDist(op - e.yyx));

                n = normalize(mul(unity_ObjectToWorld, n));

                return n;
            }

            float RayMarch(float3 ro, float3 rd, float maxDepth)
            {
                float dO = 0.;
                for (int i = 0;i < MAX_STEPS;i++)
                {
                    float3 p = ro + rd * dO;

                    float ds = GetDist(mul(unity_WorldToObject, float4(p, 1)));
                    ds = length(mul(unity_ObjectToWorld, fixed3(ds, 0, 0)));
                    dO += ds;
                    if (ds < SURFACE_DIST) return dO;
                    if (dO >= maxDepth) return MAX_DIST;
                }
                return MAX_DIST;
            }

            float3 WorldPosFromDepth(float4 screenPos, float3 direction)
            {
                return _WorldSpaceCameraPos + direction * LinearEyeDepth(tex2D(_CameraDepthTexture, screenPos.xy / screenPos.w).r);
            }

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz,1));
                o.screenPos = ComputeScreenPos(o.vertex);
                o.viewDirection = normalize(_WorldSpaceCameraPos.xyz - o.worldPos.xyz);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            struct ShadowDummy
            {
                float4 _LightCoord;
            };

            fragOutput frag(v2f i)
            {
                float3 ro = i.worldPos;
                float3 rd = normalize(i.worldPos.xyz - _WorldSpaceCameraPos);

                float3 wPosFromDepth = WorldPosFromDepth(i.screenPos, rd);
                float3 fromDepthToPos = wPosFromDepth - i.worldPos;

                if (dot(rd, fromDepthToPos) < 0) discard;

                float maxDistanceByDepth = min(MAX_DIST, length(fromDepthToPos));                
                float d = RayMarch(ro, rd, maxDistanceByDepth);
                if (d >= MAX_DIST) discard;
                
                float3 p = ro + rd * d;
                float3 normal = GetNormal(p);

                float3 light = ShadeSH9(float4(normal, 1));

                float3 lightToPoint = normalize(-_WorldSpaceLightPos0.xyz);
                float3 lightPosition = p - lightToPoint * MAX_DIST;

                float lightD = RayMarch(lightPosition, lightToPoint, MAX_DIST);
                
                float3 lightHitPoint = lightPosition + lightD * lightToPoint;
                
                float lightPointDiff = dot(p - lightHitPoint, lightToPoint);

                float normalDot = (pow(1.2 + dot(lightToPoint, GetNormal(lightHitPoint)), 0.1) - 1.2) * pow(1+lightPointDiff, _ShadowSharpness);

                /*ShadowDummy shadowD;
                shadowD._LightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1));*/

                float atten = LIGHT_ATTENUATION(i);

                float nl = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
                nl = max(0, nl - abs(normalDot)) * atten;
                light += nl * _LightColor0;

                float4 col;
                col.xyz = light;
                col.a = 1;//min(1, 1.5 - abs(dot(rd, normal)));//(d >= maxDistanceByDepth) ? 0.5 : 1;

                fragOutput o;
                o.color.xyz = col;
                o.depth = mul(UNITY_MATRIX_VP, float4(p, 1)).z;
                return o;
            }
        ENDCG
        }

        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            // Constants
            #define PI 3.1415925359
            #define TWO_PI 6.2831852
            #define MAX_STEPS 100
            #define MAX_DIST 100.
            #define SURFACE_DIST .01
            #define SHADOW_CASTER_PASS

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            struct fragOutput
            {
                fixed4 color : SV_Target;
                float depth : SV_Depth;
            };

            float Add(float a, float b)
            {
                return min(a, b);
            }

            float Sub(float a, float b)
            {
                return max(a, -b);
            }

            float Add(float a, float b, float l)
            {
                return min(a, lerp(a, b, l));
            }

            float Sub(float a, float b, float l)
            {
                return max(a, lerp(a, -b, l));
            }

            float Int(float a, float b)
            {
                return max(a, b);
            }

            float Int(float a, float b, float l)
            {
                return max(a, lerp(a, b, l));
            }

            float GetDist(float3 p)
            {
                float3 s = float3(0, 0, 0);
                float3 s2 = float3(0, 0, 0.5);
                float3 s3 = float3(0, 0.25, 0.25);
                float sphereDist = length(p - s.xyz) - 0.5;

                float sphereDist2 = length(p - s2.xyz) - 0.5;

                float sphereDist3 = length(p - s3.xyz) - 0.25;

                p += 0.5;

                p.y += sin(p.x * 30 + _Time.w) * 0.05;
                p.x += sin(p.y * 100 + _Time.w) * 0.05;

                return Add(Sub(sphereDist, sphereDist2), sphereDist3);

                /*if (p.x > 1 || p.y > 1 || p.z > 1 ||
                    p.x < 0 || p.y < 0 || p.z < 0)
                    return MAX_DIST;*/



                //return Sub((tex3Dlod(_MainTex, float4(p, 0)).x) * 0.5 - 0.002, sphereDist);
            }

            float RayMarch(float3 ro, float3 rd, float maxDepth)
            {
                float dO = 0.;
                for (int i = 0;i < MAX_STEPS;i++)
                {
                    float3 p = ro + rd * dO;

                    float ds = GetDist(mul(unity_WorldToObject, float4(p, 1)));
                    ds = length(mul(unity_ObjectToWorld, fixed3(ds, 0, 0)));
                    dO += ds;
                    if (ds < SURFACE_DIST) return dO;
                    if (dO >= maxDepth) return MAX_DIST;
                }
                return MAX_DIST;
            }

            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fragOutput frag(v2f i)
            {
                float3 wCameraPos = i.worldPos - normalize(-_WorldSpaceLightPos0.xyz) * MAX_DIST;

                float3 ro = i.worldPos;
                float3 rd = normalize(i.worldPos.xyz - wCameraPos);

                    
                float d = RayMarch(ro, rd, MAX_DIST);
                if (d >= MAX_DIST) discard;

                float3 p = ro + rd * d;
                    
                fragOutput o;
                o.color.xyz = 0;
                o.depth = mul(UNITY_MATRIX_VP, float4(p, 1)).z;
                return o;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}


#endif