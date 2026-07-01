Shader "ChillZone/UI/RarityShine"
{
    // UI image shader for content-picker icons: keeps the sprite, accents it with the rarity colour, and
    // sweeps an animated diagonal gloss band across it so the icon reads as "shiny" rather than a flat tint.
    // Based on UI/Default so it still works inside a Mask (stencil) and a RectMask2D (clip rect).
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Rarity Shine)]
        _RarityColor ("Rarity Color", Color) = (1,1,1,1)
        _RarityTint ("Rarity Tint Amount", Range(0,1)) = 0.15
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineSpeed ("Shine Speed", Float) = 0.6
        _ShineWidth ("Shine Width", Range(0.02,0.5)) = 0.12
        _ShineIntensity ("Shine Intensity", Range(0,3)) = 0.55
        _ShineRarityMix ("Shine Rarity Mix", Range(0,1)) = 0.6
        _PulseIntensity ("Pulse Intensity (epic/legendary)", Range(0,1)) = 0
        _PulseSpeed ("Pulse Speed", Float) = 2.5

        // Standard UI masking / clipping plumbing (so Mask + RectMask2D keep working).
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _RarityColor;
            float  _RarityTint;
            fixed4 _ShineColor;
            float  _ShineSpeed;
            float  _ShineWidth;
            float  _ShineIntensity;
            float  _ShineRarityMix;
            float  _PulseIntensity;
            float  _PulseSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 tex = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                half4 color = tex;

                // Wash the icon toward the rarity colour (direct lerp — clearly visible, not a faint multiply).
                color.rgb = lerp(color.rgb, _RarityColor.rgb, _RarityTint);

                // Animated diagonal gloss band sweeping across the icon — a rarity-tinted "shine".
                float diag  = frac((IN.texcoord.x + IN.texcoord.y) * 0.5);
                float sweep = frac(_Time.y * _ShineSpeed);
                float dist  = abs(frac(diag - sweep + 0.5) - 0.5);
                float band  = smoothstep(_ShineWidth, 0.0, dist);

                half3 shine = lerp(_ShineColor.rgb, _RarityColor.rgb, _ShineRarityMix) * band * _ShineIntensity;
                color.rgb += shine * tex.a;   // only on the icon's opaque pixels

                // Breathing rarity glow — set per-material; only epic / legendary use _PulseIntensity > 0.
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;   // 0..1
                color.rgb += _RarityColor.rgb * pulse * _PulseIntensity * tex.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
