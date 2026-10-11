Shader "SdfGlue/__shader_name__"
{
    Properties
    {
        // Off: the SdfGlue camera renders the scene on the mesh like on a screen (fragCoord = uv * resolution)
        // On:  rays of the Unity camera, from the surface of the mesh, in its object space (renderers using sdfg_setupCamera)
        [Toggle(SDFG_WORLD_SPACE)] _SdfgWorldSpace ("World space (ray marching from the Unity camera)", Float) = 0
        _SdfgObjectScale ("Object space units per SDF unit (world space)", Float) = 1.0
        _SdfgResolution ("Resolution (iResolution)", Vector) = (1024, 1024, 1, 0)
        _MainTex ("iChannel0", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma shader_feature_local SDFG_WORLD_SPACE

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ro : TEXCOORD1;
                float3 hitPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _SdfgObjectScale;
            float4 _SdfgResolution;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // object space - the SDF scene follows the transform of the object
                o.ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                o.hitPos = v.vertex.xyz;

                return o;
            }

//-------------------------------------------------------------------------------
// Shadertoy inputs
//-------------------------------------------------------------------------------
#define iTime           (_Time.y)
#define iTimeDelta      (unity_DeltaTime.x)
#define iFrame          ((int)(_Time.y * 60.0))
#define iMouse          (float4(0.0, 0.0, 0.0, 0.0))
#define iChannel0       _MainTex
#define iChannel1       _MainTex
#define iChannel2       _MainTex
#define iChannel3       _MainTex
static float3 iResolution = float3(1024.0, 1024.0, 1.0);

// ray of the Unity camera (world space mode, see sdfg_setupCamera)
static float3 sdfg_unityRo = float3(0.0, 0.0, 0.0);
static float3 sdfg_unityRd = float3(0.0, 0.0, 1.0);

//-------------------------------------------------------------------------------
// GLSL compatibility
//-------------------------------------------------------------------------------
#define USE_GLSL        (0)
#define mod(x, y)       ((x) - (y) * floor((x) / (y)))

// GLSL inverse() (not available in HLSL)
float2x2 sdfg_inverse(float2x2 m)
{
    return float2x2(m._22, -m._12, -m._21, m._11) / determinant(m);
}
float3x3 sdfg_inverse(float3x3 m)
{
    float3 r0 = cross(m[1], m[2]);
    float3 r1 = cross(m[2], m[0]);
    float3 r2 = cross(m[0], m[1]);
    return transpose(float3x3(r0, r1, r2)) / dot(m[0], r0);
}

//__generated_code__

//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------
            fixed4 frag (v2f i) : SV_Target
            {
                iResolution = _SdfgResolution.xyz;

            #ifdef SDFG_WORLD_SPACE
                // the ray starts on the surface of the mesh (a "window" into the SDF scene) - a distant camera
                // does not use up the ray marching distance (MAX_DIST) before it reaches the scene
                sdfg_unityRo = i.hitPos / _SdfgObjectScale;
                sdfg_unityRd = normalize(i.hitPos - i.ro);
            #endif

                float2 fragCoord = i.uv * iResolution.xy;

                float4 col = float4(0.0, 0.0, 0.0, 1.0);
                mainImage(col, fragCoord);
                col = saturate(col);
                col.a = 1.0;
                return col;
            }

            ENDCG
        }
    }
}
