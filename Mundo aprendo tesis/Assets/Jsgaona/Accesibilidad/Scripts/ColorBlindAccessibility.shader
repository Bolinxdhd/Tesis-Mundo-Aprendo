Shader "Accessibility/Color Blind Accessibility"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _Intensity ("Intensity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ColorBlindAccessibilityPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // IMPORTANTE: este include va primero
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Este include aporta Vert, Varyings y el flujo de blit
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float _Intensity;
            CBUFFER_END

            float3 ApplyMatrix(float3 c, float3 r0, float3 r1, float3 r2)
            {
                return float3(
                    dot(r0, c),
                    dot(r1, c),
                    dot(r2, c)
                );
            }

            float3 SimulateProtanopia(float3 c)
            {
                return ApplyMatrix(
                    c,
                    float3(0.567, 0.433, 0.000),
                    float3(0.558, 0.442, 0.000),
                    float3(0.000, 0.242, 0.758)
                );
            }

            float3 SimulateDeuteranopia(float3 c)
            {
                return ApplyMatrix(
                    c,
                    float3(0.625, 0.375, 0.000),
                    float3(0.700, 0.300, 0.000),
                    float3(0.000, 0.300, 0.700)
                );
            }

            float3 SimulateTritanopia(float3 c)
            {
                return ApplyMatrix(
                    c,
                    float3(0.950, 0.050, 0.000),
                    float3(0.000, 0.433, 0.567),
                    float3(0.000, 0.475, 0.525)
                );
            }

            float3 ApplyHighContrast(float3 c)
            {
                float luminance = dot(c, float3(0.299, 0.587, 0.114));
                float contrast = step(0.5, luminance);
                return lerp(float3(luminance, luminance, luminance), float3(contrast, contrast, contrast), 0.65);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Para Full Screen Pass / Blitter
                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                float3 original = col.rgb;
                float3 filtered = original;

                if (_Mode > 0.5 && _Mode < 1.5)
                    filtered = SimulateProtanopia(original);
                else if (_Mode >= 1.5 && _Mode < 2.5)
                    filtered = SimulateDeuteranopia(original);
                else if (_Mode >= 2.5 && _Mode < 3.5)
                    filtered = SimulateTritanopia(original);
                else if (_Mode >= 3.5)
                    filtered = ApplyHighContrast(original);

                col.rgb = lerp(original, saturate(filtered), _Intensity);
                return col;
            }
            ENDHLSL
        }
    }
}