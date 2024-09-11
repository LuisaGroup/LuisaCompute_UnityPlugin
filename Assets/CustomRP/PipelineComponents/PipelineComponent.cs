using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System;
public struct PipeComponentArg
{
    public ScriptableRenderContext context;
    public CustomRenderPipelineAsset asset;
    public CommandBuffer cb;
    public Camera camera;
    public bool isRenderingEditor;
    public RenderTexture targetTexture;
};