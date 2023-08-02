using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BasePassComponent : PipelineComponent
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
    struct CreateRTData
    {
        public System.IntPtr ptr;
        public System.IntPtr depthPtr;
        public LCPixelStorage storage;
        public Matrix4x4 invvp;
        public Vector3 camera_pos;
        public bool resetFrame;
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
        DrawingSettings darwSettings = new DrawingSettings(new ShaderTagId("CustomPass"),
                new SortingSettings(cam)
                {
                    criteria = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.QuantizedFrontToBack
                });
        FilteringSettings opaqueFilter = new FilteringSettings
        {
            layerMask = cam.cullingMask,
            renderingLayerMask = 1,
            renderQueueRange = new RenderQueueRange(2000, 2449)
        };
        context.DrawRenderers(cullResult, ref darwSettings, ref opaqueFilter);
        context.DrawSkybox(cam);
        if (asset.useNativeRenderer)
        {

            CreateRTData createRTData = new CreateRTData
            {
                ptr = arg.targetTexture.GetNativeTexturePtr(),
                depthPtr = arg.targetTexture.GetNativeDepthBufferPtr(),
                storage = LCPixelStorage.HALF4,
                invvp = (GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix).inverse,
                camera_pos = cam.transform.position
            };
            switch (asset.forceReset)
            {
                case CustomRenderPipelineAsset.ResetMode.ForceReset:
                    createRTData.resetFrame = true;
                    break;
                case CustomRenderPipelineAsset.ResetMode.ForceContinue:
                    createRTData.resetFrame = false;
                    break;
                default:
                    createRTData.resetFrame = CustomRenderPipelineAsset.resetFrame;
                    break;
            }
            SaberPlugin.IssuePluginEvent(cb, RenderEvents.PathTracing, ref createRTData);
        }
        context.ExecuteCommandBuffer(cb);
        cb.Clear();
    }
    public void Dispose()
    {
        SaberPlugin.Disable();
    }
}
