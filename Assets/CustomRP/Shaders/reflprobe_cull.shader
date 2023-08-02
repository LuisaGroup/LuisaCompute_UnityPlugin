Shader "Hidden/reflprobe_cull"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "plane.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.vertex.xy * 0.5 + 0.5;
                return o;
            }

            struct BBox {
                float4x4 localToWorld;
                float3 center;
                float3 extent;
            };
            #define REFL_PROBE_MAX 8
            StructuredBuffer<BBox> _BBox;
            float4 _CamNearClipPlane;
            float4 _CamFarClipPlane;
            float3 _CamFrustumMinPoint;
            float3 _CamFrustumMaxPoint;
            float4 _InvVP;
            int _ReflProbeSize;

            int frag (v2f i) : SV_Target
            {
                int2 pixel_id = (int2)i.vertex.xy;
                int cluster_x = pixel_id / _ReflProbeSize;
                int probe_idx = pixel_id.x - cluster_x * _ReflProbeSize;
                pixel_id.x = cluster_x;
                BBox bbox = _BBox[probe_idx];
                const float3 offsets[8] = {
                    float3(-0.5,-0.5,-0.5),
                    float3(-0.5,-0.5,0.5),
                    float3(-0.5,0.5,-0.5),
                    float3(-0.5,0.5,0.5),
                    float3(0.5,-0.5,-0.5),
                    float3(0.5,-0.5,0.5),
                    float3(0.5,0.5,-0.5),
                    float3(0.5,0.5,0.5)
                };
                float3 min_pos = 1e8;
                float3 max_pos = -1e8;
                for(int i = 0; i < 8; ++i) {
                    float3 world_pos = mul(bbox.localToWorld, float4(bbox.center + bbox.extent * offsets[i], 1)).xyz;
                    max_pos = max(max_pos, world_pos);
                    min_pos = min(min_pos, world_pos);
                }
                if(any(min_pos > _CamFrustumMaxPoint) || any(max_pos < _CamFrustumMinPoint)){
                    return 0;
                }
                float4 planes[6];
                planes[0] = _CamNearClipPlane;
                planes[1] = _CamFarClipPlane;
                float4 left_down_point = mul(_InvVP, float4(-1, -1, 0, 1));
                float4 right_down_point = mul(_InvVP, float4(1, -1, 0, 1));
                float4 left_top_point = mul(_InvVP, float4(-1, 1, 0, 1));
                float4 right_top_point = mul(_InvVP, float4(1, 1, 0, 1));
                left_down_point /= left_down_point.w;
                right_down_point /= right_down_point.w;
                left_top_point /= left_top_point.w;
                right_top_point /= right_top_point.w;
                planes[2] = GetPlane(left_down_point.xyz, left_top_point.xyz, _WorldSpaceCameraPos);
                planes[3] = GetPlane(right_top_point.xyz, right_down_point.xyz, _WorldSpaceCameraPos);
                planes[4] = GetPlane(left_top_point.xyz, right_top_point.xyz, _WorldSpaceCameraPos);
                planes[5] = GetPlane(right_down_point.xyz, left_down_point.xyz, _WorldSpaceCameraPos);
                if(BoxIntersectBool(bbox.extent, bbox.localToWorld, bbox.center, planes)){
                    return 0;
                }
                return 1;
            }
            ENDCG
        }
    }
}
