using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class BasePassComponent : System.IDisposable
{
    public ScriptableCullingParameters cullParams;
    public CullingResults cullResult;
    public void Start() { 
        SaberPlugin.Enable();
    }
    public void PreProcess(ref PipeComponentArg arg)
    {
        arg.camera.TryGetCullingParameters(out cullParams);
        cullParams.reflectionProbeSortingCriteria = ReflectionProbeSortingCriteria.ImportanceThenSize;
        cullParams.cullingOptions = CullingOptions.NeedsLighting | CullingOptions.NeedsReflectionProbes;
        cullParams.cullingOptions |= CullingOptions.OcclusionCull;
        cullResult = arg.context.Cull(ref cullParams);
    }
    [StructLayout(LayoutKind.Sequential)]
    struct CreateRTData
    {
        public System.IntPtr ptr;
        public System.IntPtr depthPtr;
        public LCPixelStorage storage;
        public Matrix4x4 invvp;
        public Vector3 camera_pos;
        public int cameraId;
        public uint resetFrame;
    }
    public void PostProcess(ref PipeComponentArg arg)
    {
        CommandBuffer cb = arg.cb;
        cb.SetRenderTarget(arg.targetTexture);
        ref var context = ref arg.context;
        ref var cam = ref arg.camera;
        ref var asset = ref arg.asset;
        context.ExecuteCommandBuffer(cb);
        cb.Clear();
        if (asset.useNativeRenderer)
        {

            CreateRTData createRTData = new CreateRTData
            {
                ptr = arg.targetTexture.GetNativeTexturePtr(),
                depthPtr = arg.targetTexture.GetNativeDepthBufferPtr(),
                storage = GetPixelStorage(arg.targetTexture.format),
                invvp = (GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix).inverse,
                camera_pos = cam.transform.position,
                cameraId = cam.GetEntityId().GetHashCode()
            };
            switch (asset.forceReset)
            {
                case CustomRenderPipelineAsset.ResetMode.ForceReset:
                    createRTData.resetFrame = 1u;
                    break;
                case CustomRenderPipelineAsset.ResetMode.ForceContinue:
                    createRTData.resetFrame = 0u;
                    break;
                default:
                    createRTData.resetFrame = arg.resetFrame ? 1u : 0u;
                    break;
            }
            SaberPlugin.IssuePluginEvent(cb, RenderEvents.PathTracing, ref createRTData);
        }
        context.ExecuteCommandBuffer(cb);
        cb.Clear();
    }
    private static LCPixelStorage GetPixelStorage(RenderTextureFormat format)
    {
        switch (format)
        {
            case RenderTextureFormat.ARGB32:
                return LCPixelStorage.BYTE4;
            case RenderTextureFormat.RGB111110Float:
                return LCPixelStorage.R11G11B10;
            case RenderTextureFormat.ARGBHalf:
                return LCPixelStorage.HALF4;
            case RenderTextureFormat.ARGBFloat:
                return LCPixelStorage.FLOAT4;
            default:
                throw new System.NotSupportedException($"Unsupported native render target format: {format}");
        }
    }
    public void Dispose()
    {
        SaberPlugin.Disable();
    }
}
