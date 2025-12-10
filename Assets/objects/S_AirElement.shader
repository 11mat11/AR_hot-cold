Shader "Custom/AirScooter"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor("Sphere Inner Color", Color) = (0.6, 0.9, 1, 0.1) // Jasny błękit, bardzo przezroczysty
        _WindColor("Wind Stripe Color", Color) = (1, 1, 1, 1) // Białe pasy

        [Header(Wind Texture)]
        [MainTexture] _MainTex("Wind Pattern (Grayscale)", 2D) = "black" {}
        _Tiling("Wind Density", Float) = 3.0 // Zagęszczenie pasków

        [Header(Motion)]
        _Speed("Rotation Speed", Float) = 4.0 // Szybkość obrotu

        [Header(Anime Style Settings)]
        _Cutoff("Wind Sharpness (Cutoff)", Range(0, 1)) = 0.4 // Jak ostre są krawędzie pasków
        _Smoothness("Edge Smoothness", Range(0, 0.5)) = 0.05 // Lekkie rozmycie krawędzi paska
        
        [Header(Rim Effect)]
        _RimPower("Rim Glow Power", Range(0.5, 10)) = 2.0 // Poświata na brzegach kuli
        _RimAlpha("Rim Opacity", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _WindColor;
                float4 _MainTex_ST;
                float _Speed;
                float _Tiling;
                float _Cutoff;
                float _Smoothness;
                float _RimPower;
                float _RimAlpha;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vertexInput.positionCS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);
                OUT.normalWS = normalInput.normalWS;
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. RUCH (Flow) - Tylko w osi X (poziomo)
                float2 movingUV = IN.uv;
                movingUV.x *= _Tiling; // Zagęszczamy w poziomie
                movingUV.y *= 1.0;     // Y zostawiamy, żeby paski były dookoła
                movingUV.x += _Time.y * -_Speed; // Minus żeby kręciło się w lewo (jak na gifie)

                // 2. POBRANIE SZUMU
                half noise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, movingUV).r;

                // 3. EFEKT ANIME (Toon Cutoff)
                // To zamienia miękki szum w twarde pasy
                // smoothstep robi lekkie przejście zamiast pikselozy
                half windMask = smoothstep(_Cutoff, _Cutoff + _Smoothness, noise);

                // 4. EFEKT RIM (Brzegi kuli)
                float NdotV = saturate(dot(IN.normalWS, normalize(IN.viewDirWS)));
                float rim = pow(1.0 - NdotV, _RimPower);

                // 5. SKŁADANIE KOLORU
                // Bierzemy kolor bazowy (niebieski)
                half3 finalRGB = _BaseColor.rgb;
                
                // Tam gdzie jest pasek wiatru (windMask), nakładamy biały kolor
                finalRGB = lerp(finalRGB, _WindColor.rgb, windMask);
                
                // Dodajemy jasność na brzegach (Rim)
                finalRGB += (_BaseColor.rgb * rim);

                // 6. ALFA (Przezroczystość)
                // Przezroczystość tła + pełna widoczność pasków + widoczność krawędzi
                half finalAlpha = _BaseColor.a + windMask + (rim * _RimAlpha);
                
                return half4(finalRGB, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
}