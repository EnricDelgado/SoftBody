Shader"Unlit/SoftBody2D"
{
    Properties
    {
        [MainTexture]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Wobble("Wobble (xy)", Vector) = (0,0,0,0)
        _Squash("Squash", Float) = 0
        _Radius("Radius (world)", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Wobble;
            float  _Squash;
            float  _Radius;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float4 col  : COLOR;
            };

            v2f vert (appdata v)
            {
                // Object-space xy (SpriteRenderer provides a small quad/tri mesh already)
                float2 p = v.vertex.xy;

                // Estimate normalized radial dir from object center
                float r = length(p) / max(_Radius, 1e-4);
                float2 dir = (r > 1e-5) ? (p / max(length(p), 1e-4)) : float2(0,1);

                float2 wob = _Wobble.xy;
                float wobLen = length(wob);
                float2 wobDir = (wobLen > 1e-5) ? wob / wobLen : float2(1,0);

                // Falloffs so the center moves less than the rim
                float falloffRim = saturate(1 - r*r);

                // Stretch along wobble axis + radial squash
                float along = dot(dir, wobDir);
                float2 delta = dir * (_Squash * falloffRim) + wobDir * (along * 0.08 * falloffRim);

                float4 posOS = float4(p + delta, v.vertex.zw);

                v2f o;
                o.pos = TransformObjectToHClip(posOS.xyz);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color * _Color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.col;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}