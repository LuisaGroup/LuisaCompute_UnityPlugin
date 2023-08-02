using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Collections.Generic;
public unsafe class CustomRenderPipeline : RenderPipeline
{
    CustomRenderPipelineAsset asset;
    Material finalBlitMat;
    CommandBuffer cb;
    bool isRenderingEditor;
    List<PipelineComponent> components = new List<PipelineComponent>();
    System.Func<System.Type, PipelineComponent> getComponent;
    public CustomRenderPipeline(CustomRenderPipelineAsset asset)
    {
        cb = new CommandBuffer();
        getComponent = (type) =>
        {
            foreach (var i in components)
            {
                if (i.GetType() == type) return i;
            }
            return null;
        };
        this.asset = asset;
        components.Add(new BasePassComponent());
        foreach (var i in components)
        {
            i.Start();
        }
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        cb.Dispose();
        Object.DestroyImmediate(finalBlitMat);
        foreach (var i in components)
        {
            i.Dispose();
        }
    }
    RenderTexture rt;
    override protected void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var cam in cameras)
        {
            context.SetupCameraProperties(cam);

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView)
            {
                isRenderingEditor = true;
            }
            else
            {
                isRenderingEditor = false;
            }
#else
            isRenderingEditor = false;
#endif
            if (isRenderingEditor)
            {
                CustomRenderPipelineAsset.resetFrame = true;
            }
            RenderTextureFormat format;
            switch (asset.hdrType)
            {
                case CustomRenderPipelineAsset.HDRType.LDR:
                    format = RenderTextureFormat.ARGB32;
                    break;
                case CustomRenderPipelineAsset.HDRType.HDR_LowQuality:
                    if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                    {
                        format = RenderTextureFormat.RGB111110Float;
                    }
                    else
                    {
                        // Maybe use RGBM in low-end machine
                        format = RenderTextureFormat.ARGB32;
                    }
                    break;
                default:
                    format = RenderTextureFormat.ARGBHalf;
                    break;
            }
            rt = RenderTexture.GetTemporary(new RenderTextureDescriptor
            {
                width = cam.pixelWidth,
                height = cam.pixelHeight,
                volumeDepth = 1,
                colorFormat = format,
                msaaSamples = 1,
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                depthBufferBits = 32
            });
            rt.Create();
            var arg = new PipeComponentArg
            {
                context = context,
                asset = asset,
                camera = cam,
                isRenderingEditor = isRenderingEditor,
                GetComponent = getComponent,
                targetTexture = rt,
                cb = cb
            };
            cb.Clear();
            cb.SetRenderTarget(rt);
            cb.ClearRenderTarget(true, false, Color.black);
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
            foreach (var i in components)
            {
                i.PreProcess(ref arg);
            }
            foreach (var i in components)
            {
                i.PostProcess(ref arg);
            }
            // cb.Clear();
            // cb.SetRenderTarget(rt);
            // context.ExecuteCommandBuffer(cb);
            cb.Clear();
            if (!finalBlitMat)
                finalBlitMat = new Material(asset.finalBlitShader);
            cb.Blit(rt, BuiltinRenderTextureType.CameraTarget, finalBlitMat, 0);
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
            context.DrawUIOverlay(cam);
            if (isRenderingEditor)
            {
                context.DrawGizmos(cam, GizmoSubset.PostImageEffects);
                context.DrawWireOverlay(cam);
            }
            // DrawVisibleGeometry();
            RenderTexture.ReleaseTemporary(rt);
        }
        context.Submit();
    }
}