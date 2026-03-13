Shader "Custom/SDFGlueShd"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                
                // world space
                o.ro = _WorldSpaceCameraPos;
                o.hitPos = mul(unity_ObjectToWorld, v.vertex);
                
                // object space
                //o.ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                //o.hitPos = v.vertex;
                
                return o;
            }


//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------
#define mod(x, y) (x-y*floor(x/y))

float frac(float x)
{
    return x - floor(x);
}
//-------------------------------------------------------------------------------


//__generated_definitions__
//__common_functions__
//__generated_camera_data__
//__generated_materials__
//__generated_distance_functions__
//__generated_mixop_functions__
//__generated_posop_functions__
//__generated_distop_functions__
//__generated_map_uniforms__
//__generated_map_function__
//__generated_materials_function__
//__generated_renderer_uniforms__
//__generated_renderer_code__


//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------


//            fixed4 frag (v2f i) : SV_Target
//            {
//                // sample the texture
//                fixed4 col = fixed4(1,1,0,1);// tex2D(_MainTex, i.uv);
//                // apply fog
//                UNITY_APPLY_FOG(i.fogCoord, col);
//                return col;
//            }


            ENDCG
        }
    }
}
