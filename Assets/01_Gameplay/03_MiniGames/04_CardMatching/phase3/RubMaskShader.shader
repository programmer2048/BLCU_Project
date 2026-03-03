Shader "Custom/RubMaskShader"
{
    Properties
    {
        _MainTex ("Old Image (Top)", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {} // 涂抹产生的遮罩图
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha // 开启透明混合

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_MaskTex, i.uv);
                // 核心：旧图的透明度 = 1 - 遮罩图的红色通道
                // 也就是说，遮罩图越白的地方，旧图越透明
                col.a = 1.0 - mask.r; 
                return col;
            }
            ENDCG
        }
    }
}