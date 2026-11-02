Shader "Noctal/Solar Light"
{
    Properties
    {
        _Mode ("0 haz, 1 destello, 2 chispa", Float) = 0
        _Strength ("Intensidad", Float) = 1
        _BeamLength ("Longitud", Float) = 1
        _Clock ("Tiempo de juego", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float _Mode, _Strength, _BeamLength, _Clock;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float alpha; float core;
                if (_Mode < .5)
                {
                    float flow = sin(i.uv.x * _BeamLength * 8 - _Clock * 13);
                    float y = p.y + sin(i.uv.x * _BeamLength * 5 - _Clock * 7) * .025;
                    core = exp(-y*y*380);
                    float glow = exp(-y*y*17) * (.30 + flow*.035);
                    float filament = exp(-pow(abs(y)-.23-.04*flow,2)*1200) * .15;
                    float edge = smoothstep(0,.018,i.uv.x) * smoothstep(0,.06,1-i.uv.x);
                    alpha = (core*.88 + glow + filament) * edge;
                }
                else
                {
                    float r = length(p);
                    core = exp(-r*r*48);
                    float halo = exp(-r*r*7) * .45;
                    float star = pow(saturate(1-abs(p.x)),35) * pow(saturate(1-abs(p.y)),3)
                        + pow(saturate(1-abs(p.y)),35) * pow(saturate(1-abs(p.x)),3);
                    float ring = exp(-pow(r-.42,2)*1200)*.16;
                    alpha = _Mode > 1.5 ? star : core + halo + star*.32 + ring;
                    alpha *= saturate(1-r*r);
                }
                float3 gold = float3(1,.66,.20);
                float3 ivory = float3(1, .98, .80);
                float3 color = lerp(gold, ivory, saturate(core*2));
                return float4(color, saturate(alpha * _Strength));
            }
            ENDCG
        }
    }
}
