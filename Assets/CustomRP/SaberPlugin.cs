using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
public enum LCPixelStorage : uint
{

    BYTE1,
    BYTE2,
    BYTE4,

    SHORT1,
    SHORT2,
    SHORT4,

    INT1,
    INT2,
    INT4,

    HALF1,
    HALF2,
    HALF4,

    FLOAT1,
    FLOAT2,
    FLOAT4,

    BC4,
    BC5,
    BC6,
    BC7,
    //TODO: ASTC
};
public enum DXGI_FORMAT : uint
{
    UNKNOWN	                                = 0,
    R32G32B32A32_TYPELESS                   = 1,
    R32G32B32A32_FLOAT                      = 2,
    R32G32B32A32_UINT                       = 3,
    R32G32B32A32_SINT                       = 4,
    R32G32B32_TYPELESS                      = 5,
    R32G32B32_FLOAT                         = 6,
    R32G32B32_UINT                          = 7,
    R32G32B32_SINT                          = 8,
    R16G16B16A16_TYPELESS                   = 9,
    R16G16B16A16_FLOAT                      = 10,
    R16G16B16A16_UNORM                      = 11,
    R16G16B16A16_UINT                       = 12,
    R16G16B16A16_SNORM                      = 13,
    R16G16B16A16_SINT                       = 14,
    R32G32_TYPELESS                         = 15,
    R32G32_FLOAT                            = 16,
    R32G32_UINT                             = 17,
    R32G32_SINT                             = 18,
    R32G8X24_TYPELESS                       = 19,
    D32_FLOAT_S8X24_UINT                    = 20,
    R32_FLOAT_X8X24_TYPELESS                = 21,
    X32_TYPELESS_G8X24_UINT                 = 22,
    R10G10B10A2_TYPELESS                    = 23,
    R10G10B10A2_UNORM                       = 24,
    R10G10B10A2_UINT                        = 25,
    R11G11B10_FLOAT                         = 26,
    R8G8B8A8_TYPELESS                       = 27,
    R8G8B8A8_UNORM                          = 28,
    R8G8B8A8_UNORM_SRGB                     = 29,
    R8G8B8A8_UINT                           = 30,
    R8G8B8A8_SNORM                          = 31,
    R8G8B8A8_SINT                           = 32,
    R16G16_TYPELESS                         = 33,
    R16G16_FLOAT                            = 34,
    R16G16_UNORM                            = 35,
    R16G16_UINT                             = 36,
    R16G16_SNORM                            = 37,
    R16G16_SINT                             = 38,
    R32_TYPELESS                            = 39,
    D32_FLOAT                               = 40,
    R32_FLOAT                               = 41,
    R32_UINT                                = 42,
    R32_SINT                                = 43,
    R24G8_TYPELESS                          = 44,
    D24_UNORM_S8_UINT                       = 45,
    R24_UNORM_X8_TYPELESS                   = 46,
    X24_TYPELESS_G8_UINT                    = 47,
    R8G8_TYPELESS                           = 48,
    R8G8_UNORM                              = 49,
    R8G8_UINT                               = 50,
    R8G8_SNORM                              = 51,
    R8G8_SINT                               = 52,
    R16_TYPELESS                            = 53,
    R16_FLOAT                               = 54,
    D16_UNORM                               = 55,
    R16_UNORM                               = 56,
    R16_UINT                                = 57,
    R16_SNORM                               = 58,
    R16_SINT                                = 59,
    R8_TYPELESS                             = 60,
    R8_UNORM                                = 61,
    R8_UINT                                 = 62,
    R8_SNORM                                = 63,
    R8_SINT                                 = 64,
    A8_UNORM                                = 65,
    R1_UNORM                                = 66,
    R9G9B9E5_SHAREDEXP                      = 67,
    R8G8_B8G8_UNORM                         = 68,
    G8R8_G8B8_UNORM                         = 69,
    BC1_TYPELESS                            = 70,
    BC1_UNORM                               = 71,
    BC1_UNORM_SRGB                          = 72,
    BC2_TYPELESS                            = 73,
    BC2_UNORM                               = 74,
    BC2_UNORM_SRGB                          = 75,
    BC3_TYPELESS                            = 76,
    BC3_UNORM                               = 77,
    BC3_UNORM_SRGB                          = 78,
    BC4_TYPELESS                            = 79,
    BC4_UNORM                               = 80,
    BC4_SNORM                               = 81,
    BC5_TYPELESS                            = 82,
    BC5_UNORM                               = 83,
    BC5_SNORM                               = 84,
    B5G6R5_UNORM                            = 85,
    B5G5R5A1_UNORM                          = 86,
    B8G8R8A8_UNORM                          = 87,
    B8G8R8X8_UNORM                          = 88,
    R10G10B10_XR_BIAS_A2_UNORM              = 89,
    B8G8R8A8_TYPELESS                       = 90,
    B8G8R8A8_UNORM_SRGB                     = 91,
    B8G8R8X8_TYPELESS                       = 92,
    B8G8R8X8_UNORM_SRGB                     = 93,
    BC6H_TYPELESS                           = 94,
    BC6H_UF16                               = 95,
    BC6H_SF16                               = 96,
    BC7_TYPELESS                            = 97,
    BC7_UNORM                               = 98,
    BC7_UNORM_SRGB                          = 99,
    AYUV                                    = 100,
    Y410                                    = 101,
    Y416                                    = 102,
    NV12                                    = 103,
    P010                                    = 104,
    P016                                    = 105,
    YUY2                                    = 107,
    Y210                                    = 108,
    Y216                                    = 109,
    NV11                                    = 110,
    AI44                                    = 111,
    IA44                                    = 112,
    P8                                      = 113,
    A8P8                                    = 114,
    B4G4R4A4_UNORM                          = 115,

    P208                                    = 130,
    V208                                    = 131,
    V408                                    = 132,


    SAMPLER_FEEDBACK_MIN_MIP_OPAQUE         = 189,
    SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,


    FORCE_UINT                  = 0xffffffff
}
public enum RenderEvents{
    PathTracing
}
public unsafe class SaberPlugin
{
    [DllImport("lc-unity3d.dll")]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport("lc-unity3d.dll")]
    private static extern IntPtr LCPluginLoad();
    [DllImport("lc-unity3d.dll")]
    private static extern IntPtr LCPluginUnLoad();
    [DllImport("lc-unity3d.dll")]
    private static extern int emplace_data(int event_id, void* ptr, int size);
    static bool isEnabled = false;
    public static void Enable(){
        if(isEnabled) return;
        isEnabled = true;
        LCPluginLoad();
    }
    public static void Disable(){
        if(!isEnabled) return;
        isEnabled = false;
        LCPluginUnLoad();
    }
    private static void IssuePluginEvent(RenderEvents eventId, void* data, int dataSize)
    {
        if(!isEnabled){
            Debug.LogError("Plugin not started");
            return;
        }
        int idx = emplace_data((int)eventId, data, dataSize);
        GL.IssuePluginEvent(GetRenderEventFunc(), idx);
    }
    public static void IssuePluginEvent<T>(RenderEvents eventId, ref T t) where T : unmanaged
    {
        fixed (void* ptr = &t)
        {
            IssuePluginEvent(eventId, ptr, sizeof(T));
        }
    }
    private static void IssuePluginEvent(CommandBuffer cmdBuffer, RenderEvents eventId, void* data, int dataSize)
    {
        if(!isEnabled){
            Debug.LogError("Plugin not started");
            return;
        }
        int idx = emplace_data((int)eventId, data, dataSize);
        cmdBuffer.IssuePluginEvent(GetRenderEventFunc(), idx);
    }
    public static void IssuePluginEvent<T>(CommandBuffer cmdBuffer, RenderEvents eventId, ref T t) where T : unmanaged
    {
        fixed (void* ptr = &t)
        {
            IssuePluginEvent(cmdBuffer, eventId, ptr, sizeof(T));
        }
    }
#if UNITY_EDITOR
    public static string GetGuid(UnityEngine.Object obj)
    {
        string guid;
        long localID;
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out localID))
        {
            throw new System.ArgumentException("Invalid mesh.");
        }

        if (guid == "0000000000000000e000000000000000")
        {
            guid = obj.name;
        }
        return guid;
    }
#endif
}
