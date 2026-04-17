Shader "Custom/MenuSphereForceField"
{
    Properties
    {
        _HexColor      ("Hex Fill Color", Color) = (0.6, 0.2, 1.0, 0.35) // semi-transparent purple
        _EdgeColor     ("Edge Color", Color)     = (0.8, 0.5, 1.0, 0.9)
        _GlowColor     ("Glow Color", Color)     = (0.7, 0.3, 1.0, 1.0)

        _GridScale     ("Grid Scale", Float) = 22.0
        _LineWidth     ("Edge Width", Range(0.002, 0.12)) = 0.025

        _GlowStrength  ("Glow Strength", Range(0.0, 5.0)) = 2.0

        _PulseSpeed    ("Pulse Speed", Float) = 0.6
        _PulseWidth    ("Pulse Width", Range(0.01, 0.5)) = 0.08

        _FlickerSpeed  ("Flicker Speed", Float) = 1.5
        _FlickerAmount ("Flicker Amount", Range(0.0, 0.2)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _HexColor;
            fixed4 _EdgeColor;
            fixed4 _GlowColor;

            float _GridScale;
            float _LineWidth;
            float _GlowStrength;
            float _PulseSpeed;
            float _PulseWidth;
            float _FlickerSpeed;
            float _FlickerAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float2 NearestHex(float2 p, float scale)
            {
                p *= scale;
                float2 s = float2(1.0, 1.73205);
                float2 a = fmod(p, s) - s * 0.5;
                float2 b = fmod(p + s * 0.5, s) - s * 0.5;
                return (dot(a, a) < dot(b, b)) ? a : b;
            }

            float HexEdgeDist(float2 p)
            {
                p = abs(p);
                return max(dot(p, float2(0.86603, 0.5)), p.x);
            }

            float CellHash(float2 cellIndex)
            {
                return frac(sin(dot(cellIndex, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                float2 hexLocal = NearestHex(uv, _GridScale);
                float d = HexEdgeDist(hexLocal);

                float lineThresh = 0.5 - _LineWidth;
                float aa = fwidth(d) * 1.5;
                float edge = smoothstep(lineThresh - aa, lineThresh + aa, d);

                float fill = 1.0 - edge;

                // Flicker
                float2 scaledUV = uv * _GridScale;
                float2 s = float2(1.0, 1.73205);
                float2 cell = floor(scaledUV / s);
                float hash = CellHash(cell);

                float flicker = sin(t * _FlickerSpeed + hash * 6.28);
                flicker = 1.0 + flicker * _FlickerAmount;

                // Pulse
                float2 toCenter = uv - 0.5;
                float dist = length(toCenter) * 2.0;
                float phase = frac(dist - t * _PulseSpeed);

                float pulse = smoothstep(0.0, _PulseWidth, phase) *
                              smoothstep(_PulseWidth * 2.0, _PulseWidth, phase);

                // Glow
                float glow = smoothstep(0.45, 0.5, d) * (1.0 - edge);
                glow *= _GlowStrength;

                // --- COLOR COMPOSITION ---

                // Purple fill (semi-transparent)
                fixed4 col = _HexColor * fill * flicker;

                // Bright edges
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge);

                // Add glow
                col.rgb += _GlowColor.rgb * glow;

                // Pulse effect
                col.rgb += _GlowColor.rgb * pulse * 0.5;
                col.a += pulse * 0.2;

                return col;
            }
            ENDCG
        }
    }
}