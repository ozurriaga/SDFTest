#if false

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/SDFRayMarching"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 3D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf SimpleSpecular vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float3 worldPos;
        };

        fixed4 _Color;
        
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

            // Constants
        #define PI 3.1415925359
        #define TWO_PI 6.2831852
        #define MAX_STEPS 100
        #define MAX_DIST 100.
        #define SURFACE_DIST .01

        float GetDist(float3 p)
        {
            float3 s = float3(0, 0, 0); //Sphere. xyz is position w is radius
            float3 s2 = float3(0, 0, 0.5); //Sphere. xyz is position w is radius
            float sphereDist = length(p - s.xyz) - 0.5;

            float sphereDist2 = length(p - s2.xyz) - 0.5;
            
            
            return max(sphereDist, -sphereDist2);
        }

        float3 GetNormal(float3 p)
        {
            float d = GetDist(p); // Distance
            float2 e = float2(.01, 0); // Epsilon
            float3 n = d - float3(
                GetDist(p - e.xyy),
                GetDist(p - e.yxy),
                GetDist(p - e.yyx));

            return normalize(n);
        }

        float RayMarch(float3 ro, float3 rd)
        {
            float dO = 0.;
            for (int i = 0;i < MAX_STEPS;i++)
            {
                float3 p = ro + rd * dO;
                float ds = GetDist(mul(unity_WorldToObject, float4(p, 1)));
                dO += ds;
                if (dO > MAX_DIST || ds < SURFACE_DIST) break;
            }
            return dO;
        }

        half4 LightingSimpleSpecular(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
            float3 normal = float3(1, 0, 0);
            half3 h = normalize(lightDir + viewDir);

            half diff = max(0, dot(normal, lightDir));

            float nh = max(0, dot(normal, h));
            float spec = pow(nh, 48.0);

            half4 c;
            c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec)* atten;
            c.a = s.Alpha;
            return c;
        }

        void vert(inout appdata_full v, out Input o)
        {
            o.worldPos = mul(unity_ObjectToWorld, v.vertex.xyz);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            float3 ro = IN.worldPos;
            float3 rd = normalize(IN.worldPos.xyz - _WorldSpaceCameraPos);
            float d = RayMarch(ro, rd);
            if (d >= 10) discard;

            float3 p = ro + rd * d;
            o.Normal = GetNormal(p);
            
            float3 color = 0.5;
            o.Albedo = color;
            o.Specular = 1;
            o.Gloss = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}


#endif