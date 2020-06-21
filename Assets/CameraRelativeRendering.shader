Shader "Unlit/CameraRelativeRendering"
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _TestValue;
            float4x4 _TestMatrix;
            float4x4 _TestMatrix2;
            float4x4 _TestMatrix3;
            v2f vert (appdata v)
            {
                v2f o;
                // unity_ObjectToWorld
                //float4x4 test1 =

                float4x4 objectToWorld = mul(_TestMatrix, unity_ObjectToWorld);
                float4x4 worldToClip = mul(UNITY_MATRIX_VP, _TestMatrix2);
                //float4x4 mvp = mul(worldToClip, objectToWorld);
                //tm[0][3] = tm[0][3] - _WorldSpaceCameraPos.x;
                //tm[1][3] = tm[1][3] - _WorldSpaceCameraPos.y;
                //tm[2][3] = tm[2][3] - _WorldSpaceCameraPos.z;

                float4 t1 = mul(objectToWorld, v.vertex);
                float4 t2 = mul(worldToClip, t1);
                o.vertex = t2;
               //o.vertex = mul(mvp, v.vertex);
                //o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
