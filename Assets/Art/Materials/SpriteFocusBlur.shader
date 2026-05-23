Shader "Clockwork Wasteland/Sprite Focus Blur"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurStrength ("Blur Strength", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _BlurStrength;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float strength = saturate(_BlurStrength);
                float2 offset = _MainTex_TexelSize.xy * lerp(0.75, 2.5, strength);
                fixed4 color = tex2D(_MainTex, input.texcoord) * 0.42;
                color += tex2D(_MainTex, input.texcoord + float2(offset.x, 0)) * 0.145;
                color += tex2D(_MainTex, input.texcoord - float2(offset.x, 0)) * 0.145;
                color += tex2D(_MainTex, input.texcoord + float2(0, offset.y)) * 0.145;
                color += tex2D(_MainTex, input.texcoord - float2(0, offset.y)) * 0.145;
                color.rgb = lerp(color.rgb, dot(color.rgb, fixed3(0.299, 0.587, 0.114)).xxx, 0.28 * strength);
                color.rgb *= lerp(0.82, 0.66, strength);
                return color * input.color;
            }
            ENDCG
        }
    }
}
