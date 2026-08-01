Shader "Hidden/FinalBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex Vert
            #pragma fragment Frag

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _SourceScaleBias;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexId : SV_VertexID)
            {
                Varyings output;
                float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.uv = uv * _SourceScaleBias.xy + _SourceScaleBias.zw;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return _MainTex.SampleLevel(sampler_MainTex, input.uv, 0);
            }
            ENDHLSL
        }
    }
}
