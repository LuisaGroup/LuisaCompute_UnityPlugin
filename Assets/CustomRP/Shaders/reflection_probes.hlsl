#ifndef _SRP_REFL_PROBE_INCLUDE_
    #define _SRP_REFL_PROBE_INCLUDE_
    #if REFL_PROBE_MAX>=0
        TextureCube<float4> _ReflectionProbe0;
        #if REFL_PROBE_MAX>=1
            TextureCube<float4> _ReflectionProbe1;
            #if REFL_PROBE_MAX>=2
                TextureCube<float4> _ReflectionProbe2;
                #if REFL_PROBE_MAX>=3
                    TextureCube<float4> _ReflectionProbe3;
                    #if REFL_PROBE_MAX>=4
                        TextureCube<float4> _ReflectionProbe4;
                        #if REFL_PROBE_MAX>=5
                            TextureCube<float4> _ReflectionProbe5;
                            #if REFL_PROBE_MAX>=6
                                TextureCube<float4> _ReflectionProbe6;
                                #if REFL_PROBE_MAX>=7
                                    TextureCube<float4> _ReflectionProbe7;
                                    #if REFL_PROBE_MAX>=8
                                        TextureCube<float4> _ReflectionProbe8;
                                        #if REFL_PROBE_MAX>=9
                                            TextureCube<float4> _ReflectionProbe9;
                                            #if REFL_PROBE_MAX>=10
                                                TextureCube<float4> _ReflectionProbe10;
                                                #if REFL_PROBE_MAX>=11
                                                    TextureCube<float4> _ReflectionProbe11;
                                                    #if REFL_PROBE_MAX>=12
                                                        TextureCube<float4> _ReflectionProbe12;
                                                        #if REFL_PROBE_MAX>=13
                                                            TextureCube<float4> _ReflectionProbe13;
                                                            #if REFL_PROBE_MAX>=14
                                                                TextureCube<float4> _ReflectionProbe14;
                                                                #if REFL_PROBE_MAX>=15
                                                                    TextureCube<float4> _ReflectionProbe15;
                                                                #endif
                                                            #endif
                                                        #endif
                                                    #endif
                                                #endif
                                            #endif
                                        #endif
                                    #endif
                                #endif
                            #endif
                        #endif
                    #endif
                #endif
            #endif
        #endif
    #endif
    SamplerState sampcube_linear_clamp;

    float4 SampleReflectionProbe(float3 dir, int idx){
        switch(idx){
            #if REFL_PROBE_MAX>=0
                case 0: return _ReflectionProbe0.Sample(sampcube_linear_clamp, uvw);
                #if REFL_PROBE_MAX>=1
                    case 1: return _ReflectionProbe1.Sample(sampcube_linear_clamp, uvw);
                    #if REFL_PROBE_MAX>=2
                        case 2: return _ReflectionProbe2.Sample(sampcube_linear_clamp, uvw);
                        #if REFL_PROBE_MAX>=3
                            case 3: return _ReflectionProbe3.Sample(sampcube_linear_clamp, uvw);
                            #if REFL_PROBE_MAX>=4
                                case 4: return _ReflectionProbe4.Sample(sampcube_linear_clamp, uvw);
                                #if REFL_PROBE_MAX>=5
                                    case 5: return _ReflectionProbe5.Sample(sampcube_linear_clamp, uvw);
                                    #if REFL_PROBE_MAX>=6
                                        case 6: return _ReflectionProbe6.Sample(sampcube_linear_clamp, uvw);
                                        #if REFL_PROBE_MAX>=7
                                            case 7: return _ReflectionProbe7.Sample(sampcube_linear_clamp, uvw);
                                            #if REFL_PROBE_MAX>=8
                                                case 8: return _ReflectionProbe8.Sample(sampcube_linear_clamp, uvw);
                                                #if REFL_PROBE_MAX>=9
                                                    case 9: return _ReflectionProbe9.Sample(sampcube_linear_clamp, uvw);
                                                    #if REFL_PROBE_MAX>=10
                                                        case 10: return _ReflectionProbe10.Sample(sampcube_linear_clamp, uvw);
                                                        #if REFL_PROBE_MAX>=11
                                                            case 11: return _ReflectionProbe11.Sample(sampcube_linear_clamp, uvw);
                                                            #if REFL_PROBE_MAX>=12
                                                                case 12: return _ReflectionProbe12.Sample(sampcube_linear_clamp, uvw);
                                                                #if REFL_PROBE_MAX>=13
                                                                    case 13: return _ReflectionProbe13.Sample(sampcube_linear_clamp, uvw);
                                                                    #if REFL_PROBE_MAX>=14
                                                                        case 14: return _ReflectionProbe14.Sample(sampcube_linear_clamp, uvw);
                                                                        #if REFL_PROBE_MAX>=15
                                                                            case 15: return _ReflectionProbe15.Sample(sampcube_linear_clamp, uvw);
                                                                        #endif
                                                                    #endif
                                                                #endif
                                                            #endif
                                                        #endif
                                                    #endif
                                                #endif
                                            #endif
                                        #endif
                                    #endif
                                #endif
                            #endif
                        #endif
                    #endif
                #endif
            #endif
            default: return float4(0,0,0,0);
        }
    }

    float4 SampleReflectionProbeLevel(float3 dir, int idx, float mip){
        switch(idx){
            #if REFL_PROBE_MAX>=0
                case 0: return _ReflectionProbe0.SampleLevel(sampcube_linear_clamp, uvw, mip);
                #if REFL_PROBE_MAX>=1
                    case 1: return _ReflectionProbe1.SampleLevel(sampcube_linear_clamp, uvw, mip);
                    #if REFL_PROBE_MAX>=2
                        case 2: return _ReflectionProbe2.SampleLevel(sampcube_linear_clamp, uvw, mip);
                        #if REFL_PROBE_MAX>=3
                            case 3: return _ReflectionProbe3.SampleLevel(sampcube_linear_clamp, uvw, mip);
                            #if REFL_PROBE_MAX>=4
                                case 4: return _ReflectionProbe4.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                #if REFL_PROBE_MAX>=5
                                    case 5: return _ReflectionProbe5.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                    #if REFL_PROBE_MAX>=6
                                        case 6: return _ReflectionProbe6.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                        #if REFL_PROBE_MAX>=7
                                            case 7: return _ReflectionProbe7.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                            #if REFL_PROBE_MAX>=8
                                                case 8: return _ReflectionProbe8.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                #if REFL_PROBE_MAX>=9
                                                    case 9: return _ReflectionProbe9.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                    #if REFL_PROBE_MAX>=10
                                                        case 10: return _ReflectionProbe10.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                        #if REFL_PROBE_MAX>=11
                                                            case 11: return _ReflectionProbe11.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                            #if REFL_PROBE_MAX>=12
                                                                case 12: return _ReflectionProbe12.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                                #if REFL_PROBE_MAX>=13
                                                                    case 13: return _ReflectionProbe13.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                                    #if REFL_PROBE_MAX>=14
                                                                        case 14: return _ReflectionProbe14.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                                        #if REFL_PROBE_MAX>=15
                                                                            case 15: return _ReflectionProbe15.SampleLevel(sampcube_linear_clamp, uvw, mip);
                                                                        #endif
                                                                    #endif
                                                                #endif
                                                            #endif
                                                        #endif
                                                    #endif
                                                #endif
                                            #endif
                                        #endif
                                    #endif
                                #endif
                            #endif
                        #endif
                    #endif
                #endif
            #endif
            default: return float4(0,0,0,0);
        }
    }
    Texture2D<int> _ReflectionProbeIndices;
    int2 _ReflCullMapSize;
    int _ReflProbeSize;
    float4 SampleReflCubes(float3 uvw, float2 screen_uv){
        int2 pixel_idx = int2(clamp(screen_uv * _ReflCullMapSize, float2(0), _ReflCullMapSize - float2(0.5)));
        float4 value = 0;
        int2 sample_idx = int2(pixel_idx.x * _ReflProbeSize, pixel_idx.y);
        for(int i = 0; i < _ReflProbeSize; ++i){
            if(_ReflectionProbeIndices[int2(sample_idx.x + i, sample_idx.y)]){
                value = SampleReflectionProbe(uvw, tex_id);
            }
        }
        return value;
    }

#endif