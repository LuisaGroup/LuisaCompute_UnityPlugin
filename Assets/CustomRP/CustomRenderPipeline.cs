using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public unsafe class CustomRenderPipeline : RenderPipeline
{
    private sealed class CameraResources
    {
        public RenderTexture target;
        public Matrix4x4 viewMatrix;
        public Matrix4x4 projectionMatrix;
        public int width;
        public int height;
        public RenderTextureFormat format;
        public bool hasCameraState;
    }

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int SourceScaleBiasId = Shader.PropertyToID("_SourceScaleBias");
    private static readonly Vector4 IdentityScaleBias = new(1f, 1f, 0f, 0f);
    private static readonly Vector4 FlipYScaleBias = new(1f, -1f, 0f, 1f);

    private readonly CustomRenderPipelineAsset asset;
    private readonly CommandBuffer commandBuffer = new() { name = "Custom Render Pipeline" };
    private readonly MaterialPropertyBlock finalBlitProperties = new();
    private readonly Dictionary<int, CameraResources> cameraResources = new();
    private readonly BasePassComponent baseComponent = new();
    private Material finalBlitMaterial;

    public CustomRenderPipeline(CustomRenderPipelineAsset asset)
    {
        this.asset = asset;
        baseComponent.Start();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        commandBuffer.Dispose();
        Object.DestroyImmediate(finalBlitMaterial);
        foreach (var resources in cameraResources.Values)
            ReleaseTarget(resources);
        cameraResources.Clear();
        baseComponent.Dispose();
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach (var camera in cameras)
            RenderCamera(context, camera);
    }

    private void RenderCamera(ScriptableRenderContext context, Camera camera)
    {
        var width = camera.targetTexture != null ? camera.targetTexture.width : camera.pixelWidth;
        var height = camera.targetTexture != null ? camera.targetTexture.height : camera.pixelHeight;
        if (width <= 0 || height <= 0)
            return;

        BeginCameraRendering(context, camera);

#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        else if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
            ScriptableRenderContext.EmitGeometryForCamera(camera);
#endif

        context.SetupCameraProperties(camera);

        var format = GetRenderTextureFormat();
        var resources = GetCameraResources(GetCameraId(camera));
        var recreated = EnsureTarget(resources, camera, width, height, format);
        var cameraChanged = recreated || HasCameraChanged(resources, camera);
        UpdateCameraState(resources, camera);

        var argument = new PipeComponentArg
        {
            context = context,
            asset = asset,
            camera = camera,
            isRenderingEditor = camera.cameraType == CameraType.SceneView,
            resetFrame = cameraChanged,
            targetTexture = resources.target,
            cb = commandBuffer
        };

        commandBuffer.Clear();
        commandBuffer.SetRenderTarget(resources.target);
        commandBuffer.SetViewport(new Rect(0f, 0f, width, height));
        commandBuffer.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        baseComponent.PreProcess(ref argument);
        baseComponent.PostProcess(ref argument);

        DrawFinalBlit(context, camera, resources.target);
        DrawEditorOverlays(context, camera);

        context.Submit();
        EndCameraRendering(context, camera);
    }

    private void DrawFinalBlit(ScriptableRenderContext context, Camera camera, RenderTexture source)
    {
        if (finalBlitMaterial == null)
            finalBlitMaterial = new Material(asset.finalBlitShader) { hideFlags = HideFlags.HideAndDontSave };

        var destination = camera.targetTexture != null
            ? new RenderTargetIdentifier(camera.targetTexture)
            : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
        var flipY = camera.cameraType == CameraType.Game &&
                    camera.targetTexture == null &&
                    SystemInfo.graphicsUVStartsAtTop;

        finalBlitProperties.Clear();
        finalBlitProperties.SetTexture(MainTexId, source);
        finalBlitProperties.SetVector(SourceScaleBiasId, flipY ? FlipYScaleBias : IdentityScaleBias);

        commandBuffer.Clear();
        commandBuffer.SetRenderTarget(destination);
        commandBuffer.DrawProcedural(Matrix4x4.identity, finalBlitMaterial, 0,
            MeshTopology.Triangles, 3, 1, finalBlitProperties);
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        // SceneView and Preview use editor-owned CameraTarget surfaces. Their viewport
        // is not reliably represented by camera.pixelRect after switching editor tabs.
        context.SetupCameraProperties(camera);
    }

    private static void DrawEditorOverlays(ScriptableRenderContext context, Camera camera)
    {
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            context.DrawWireOverlay(camera);
        }
#endif
    }

    private CameraResources GetCameraResources(int cameraId)
    {
        if (!cameraResources.TryGetValue(cameraId, out var resources))
        {
            resources = new CameraResources();
            cameraResources.Add(cameraId, resources);
        }
        return resources;
    }

    private static bool EnsureTarget(CameraResources resources, Camera camera, int width, int height,
        RenderTextureFormat format)
    {
        if (resources.target != null && resources.width == width && resources.height == height &&
            resources.format == format)
            return false;

        ReleaseTarget(resources);
        resources.width = width;
        resources.height = height;
        resources.format = format;
        resources.hasCameraState = false;
        resources.target = new RenderTexture(new RenderTextureDescriptor
        {
            width = width,
            height = height,
            volumeDepth = 1,
            colorFormat = format,
            msaaSamples = 1,
            dimension = TextureDimension.Tex2D,
            enableRandomWrite = true,
            depthBufferBits = 32
        })
        {
            name = $"CustomRP Camera Target ({camera.name})",
            hideFlags = HideFlags.HideAndDontSave
        };
        resources.target.Create();
        return true;
    }

    private static void ReleaseTarget(CameraResources resources)
    {
        if (resources.target == null)
            return;
        resources.target.Release();
        Object.DestroyImmediate(resources.target);
        resources.target = null;
    }

    private static int GetCameraId(Camera camera)
    {
        return camera.GetEntityId().GetHashCode();
    }

    private static bool HasCameraChanged(CameraResources resources, Camera camera)
    {
        return !resources.hasCameraState ||
               !Approximately(resources.viewMatrix, camera.worldToCameraMatrix) ||
               !Approximately(resources.projectionMatrix, camera.projectionMatrix);
    }

    private static void UpdateCameraState(CameraResources resources, Camera camera)
    {
        resources.viewMatrix = camera.worldToCameraMatrix;
        resources.projectionMatrix = camera.projectionMatrix;
        resources.hasCameraState = true;
    }

    private static bool Approximately(Matrix4x4 left, Matrix4x4 right)
    {
        const float epsilon = 1e-6f;
        for (var i = 0; i < 16; i++)
        {
            if (Mathf.Abs(left[i] - right[i]) > epsilon)
                return false;
        }
        return true;
    }

    private RenderTextureFormat GetRenderTextureFormat()
    {
        switch (asset.hdrType)
        {
            case CustomRenderPipelineAsset.HDRType.LDR:
                return RenderTextureFormat.ARGB32;
            case CustomRenderPipelineAsset.HDRType.HDR_LowQuality:
                return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float)
                    ? RenderTextureFormat.RGB111110Float
                    : RenderTextureFormat.ARGB32;
            default:
                return RenderTextureFormat.ARGBHalf;
        }
    }
}
