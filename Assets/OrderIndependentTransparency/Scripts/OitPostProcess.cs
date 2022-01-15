#if UNITY_POST_PROCESSING_STACK_V2
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
public sealed class OitModeParameter : ParameterOverride<OitMode>
{
}

[Serializable]
[PostProcess(typeof(OitPostProcessRenderer), PostProcessEvent.BeforeStack, "OrderIndependentTransparency")]
public sealed class OitPostProcess : PostProcessEffectSettings
{
    [Tooltip("Use Multi-Layer Alpha Blending if your graphics target supports shader model 5.1 and the Rasterizer Order Views (ROV) feature." +
                "For legacy shader model 5.0 support use the linked list mode.")]
    public OitModeParameter oitMode = new OitModeParameter { value = OitMode.MLAB };
}

public sealed class OitPostProcessRenderer : PostProcessEffectRenderer<OitPostProcess>
{
    private IOrderIndependentTransparency orderIndependentTransparency;

    public override void Init()
    {
        base.Init();
        if (settings.oitMode.value == OitMode.MLAB)
        {
            orderIndependentTransparency = new OitMultiLayerAlphaBlending(true);
        }
        else
        {
            orderIndependentTransparency = new OitLinkedList(true);
        }

        Camera.onPreRender += PreRender;
    }

    private void PreRender(Camera cam)
    {
        orderIndependentTransparency.PreRender();
    }

    public override void Render(PostProcessRenderContext context)
    {
        orderIndependentTransparency.Render(context);
    }

    public override void Release()
    {
        base.Release();
        orderIndependentTransparency.Release();
        Camera.onPreRender -= PreRender;
    }
}
#endif