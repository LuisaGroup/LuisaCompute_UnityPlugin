using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset<CustomRenderPipeline>
{
    public override string renderPipelineShaderTag => "CustomRenderPipeline";

    public enum HDRType{
        LDR,
        HDR_LowQuality,
        HDR_HighQuality
    }
    public enum ResetMode{
        None,
        ForceReset,
        ForceContinue
    }
    public HDRType hdrType = HDRType.HDR_HighQuality;
    public  ResetMode forceReset = ResetMode.None;
    public bool useNativeRenderer = false;
    public Shader finalBlitShader;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(this);
    }
}
