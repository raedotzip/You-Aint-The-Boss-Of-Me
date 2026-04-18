// Animated hexagonal grid shader for the inside of a menu sphere.
// Usage: create a Unity sphere, invert it (scale Y to -1 or use Cull Front below),
// assign a material using this shader. Cull Front makes it visible from inside.
Shader "Custom/MenuSphereWall"
{
    Properties
    {
        _GridColor      ("Grid Line Color",  Color)          = (0.0, 0.85, 0.9, 1.0)
        _BgColor        ("Background Color", Color)          = (0.01, 0.01, 0.07, 1.0)
        _GlowColor      ("Pulse Glow Color", Color)          = (0.0, 0.4, 1.0, 1.0)
        _GridScale      ("Grid Scale",       Float)          = 22.0
        _LineWidth      ("Line Width",       Range(0.002, 0.12)) = 0.028
        _GlowStrength   ("Glow Strength",    Range(0.0, 4.0))   = 1.6
        _PulseSpeed     ("Pulse Speed",      Float)          = 0.45
        _PulseWidth     ("Pulse Band Width", Range(0.01, 0.5))  = 0.07
        _FlickerSpeed   ("Cell Flicker Speed", Float)        = 1.2
        _FlickerAmount  ("Cell Flicker Amount", Range(0.0, 0.15)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Front   // Renders the inside face of the sphere

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "UnityCG.cginc"

            // ---- Properties ----
            fixed4 _GridColor;
            fixed4 _BgColor;
            fixed4 _GlowColor;
            float  _GridScale;
            float  _LineWidth;
            float  _GlowStrength;
            float  _PulseSpeed;
            float  _PulseWidth;
            float  _FlickerSpeed;
            float  _FlickerAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- Hex grid ----
            // Returns local position inside the nearest hexagon.
            // The valid range fills a hex of "radius" ~0.5 in this space.
            float2 NearestHex(float2 p, float scale)
            {
                p *= scale;
                // Two offset lattices that together tile the plane with hexagons
                float2 s = float2(1.0, 1.73205); // 1, sqrt(3)
                float2 a = fmod(p,           s) - s * 0.5;
                float2 b = fmod(p + s * 0.5, s) - s * 0.5;
                return (dot(a, a) < dot(b, b)) ? a : b;
            }

            // Pseudo-distance to hex edge: 0 = center, 0.5 = at edge corner
            float HexEdgeDist(float2 p)
            {
                p = abs(p);
                // max of two projections covers all six edges
                return max(dot(p, float2(0.86603, 0.5)), p.x);
            }

            // Fast hash for per-cell variation
            float CellHash(float2 cellIndex)
            {
                return frac(sin(dot(cellIndex, float2(127.1, 311.7))) * 43758.5453);
            }

            // ---- Vertex ----
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            // ---- Fragment ----
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float  t  = _Time.y;

                // --- Hex grid ---
                float2 hexLocal = NearestHex(uv, _GridScale);
                float  d        = HexEdgeDist(hexLocal); // 0 = center, ~0.5 = edge

                // Antialiased line mask (1 = on edge line, 0 = inside cell)
                float lineThresh = 0.5 - _LineWidth;
                float aa         = fwidth(d) * 1.5;
                float onLine     = smoothstep(lineThresh - aa, lineThresh + aa, d);

                // --- Per-cell hash for flicker ---
                // Reconstruct approximate cell index from UV
                float2 scaledUV  = uv * _GridScale;
                float2 s         = float2(1.0, 1.73205);
                float2 cellA     = floor(scaledUV           / s);
                float2 cellB     = floor((scaledUV + s*0.5) / s);
                float2 hexA      = fmod(scaledUV,           s) - s * 0.5;
                float2 hexB      = fmod(scaledUV + s * 0.5, s) - s * 0.5;
                float2 cellIndex = (dot(hexA,hexA) < dot(hexB,hexB)) ? cellA : cellB;

                float  hash      = CellHash(cellIndex);
                float  flicker   = sin(t * _FlickerSpeed + hash * 6.2832);
                flicker          = 1.0 + flicker * _FlickerAmount; // subtle ±FlickerAmount

                // --- Concentric pulse rings radiating from UV center ---
                float2 toCenter  = uv - float2(0.5, 0.5);
                float  radDist   = length(toCenter) * 2.0; // 0 at center, ~1.4 at corners
                float  phase     = frac(radDist - t * _PulseSpeed);
                float  pulse     = smoothstep(0.0,         _PulseWidth,       phase)
                                 * smoothstep(_PulseWidth * 2.0, _PulseWidth, phase);
                pulse           *= 1.0 - saturate(radDist * 0.75); // fade at edges

                // --- Glow halo around lines ---
                float glowThresh = 0.5 - _LineWidth * 3.5;
                float glow       = smoothstep(glowThresh, 0.5 - _LineWidth, d) * (1.0 - onLine);
                glow            *= _GlowStrength * 0.25;

                // --- Compose ---
                // Background with subtle cell-fill brightening
                float cellFill = (1.0 - onLine) * (flicker - 1.0);
                fixed4 col     = _BgColor + cellFill * _BgColor;

                // Grid lines
                col = lerp(col, _GridColor * flicker, onLine);

                // Glow layer around lines
                col += _GlowColor * glow;

                // Pulse brightens lines it passes over
                col += _GridColor * pulse * onLine * 1.2;
                col += _GlowColor * pulse * 0.12;   // faint pulse over background

                return col;
            }
            ENDCG
        }
    }
}
