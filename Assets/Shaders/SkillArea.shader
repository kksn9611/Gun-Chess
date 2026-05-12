Shader "Custom/SkillArea"
{
    Properties
    {
        _MainColor  ("Main Color", Color)                        = (1, 0, 0, 0.4)
        _ShapeType  ("Shape Type (0=Circle 1=Cone 2=Laser)", Float) = 0
        _Angle      ("Cone Angle (full, degrees)", Float)        = 90
        _EdgeSoftness ("Edge Softness", Range(0.0, 0.3))        = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"
        }

        Pass
        {
            Name "SkillAreaPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest  LEqual
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Attributes //

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            // Properties //

            CBUFFER_START(UnityPerMaterial)
                half4  _MainColor;
                float  _ShapeType;
                float  _Angle;
                float  _EdgeSoftness;
            CBUFFER_END

            // Vertex //

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv         = input.uv;
                return output;
            }

            // Fragment //

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv   = input.uv * 2.0 - 1.0; // remap (0,1) -> (-1,1)
                float  dist  = length(uv);
                float  soft  = max(_EdgeSoftness, 0.001);

                // Circle — distance from center
                float circleMask = 1.0 - smoothstep(1.0 - soft, 1.0, dist);

                // Cone — distance + angle from forward (+Y in UV space)
                float halfRad = _Angle * 0.5 * PI / 180.0;
                float ang     = atan2(abs(uv.x), uv.y); // 0 at +Y, PI at -Y
                float coneMask = (1.0 - smoothstep(1.0 - soft, 1.0, dist))
                               * (1.0 - smoothstep(halfRad - soft, halfRad, ang));

                // Laser — full quad with soft border
                float laserMask = smoothstep(0.0, soft * 2.0, 1.0 - abs(uv.x))
                                * smoothstep(0.0, soft * 2.0, 1.0 - abs(uv.y));

                // Shape selection (0 = Circle, 1 = Cone, 2 = Laser)
                float s0 = step(abs(_ShapeType),       0.5);
                float s1 = step(abs(_ShapeType - 1.0), 0.5);
                float s2 = step(abs(_ShapeType - 2.0), 0.5);
                float mask = circleMask * s0 + coneMask * s1 + laserMask * s2;

                return half4(_MainColor.rgb, _MainColor.a * saturate(mask));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
