using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class PCSSLight : MonoBehaviour
{
    public int resolution = 4096;
    public bool customShadowResolution = false;

    [Space(20f)]
    [Range(1, 128)]
    public int Blocker_SampleCount = 16;
    [Range(1, 128)]
    public int PCF_SampleCount = 16;
    public bool RotateSamples = true;

    [Space(20f)]
    [Range(1f, 10f)]
    public float Softness = 1f;

    [Space(20f)]
    [Range(0f, 0.15f)]
    public float MaxStaticGradientBias = .05f;
    [Range(0f, 1f)]
    public float Blocker_GradientBias = 0f;
    [Range(0f, 1f)]
    public float PCF_GradientBias = 1f;

    /* Unity's Cascade Blending doesn't work quite right - Might be able to do something about it when I have time to research it more */
    [Space(20f)]
    [Range(0f, 1f)]
    public float CascadeBlendDistance = .5f;


    [Space(20f)]
    public RenderTexture shadowRenderTexture;
    public RenderTextureFormat format = RenderTextureFormat.RFloat;
    public FilterMode filterMode = FilterMode.Trilinear;
    public LightEvent lightEvent = LightEvent.AfterShadowMap;
    public string shaderName = "Hidden/PCSS";
    private Shader shader;

    private CommandBuffer copyShadowBuffer;
    private Light _light;
    private Camera _cam { get { return Camera.main; } }

    public void OnEnable ()
    {
        Setup();
    }

    [ContextMenu("Reinitialize")]
    public void Setup ()
    {
        _light = GetComponent<Light>();
        if (!_light)
            return;

        resolution = Mathf.ClosestPowerOfTwo(resolution);
        if (customShadowResolution)
            _light.shadowCustomResolution = resolution;
        else
            _light.shadowCustomResolution = 0;

        shadowRenderTexture = new RenderTexture(resolution, resolution, 0, format);
        shadowRenderTexture.filterMode = filterMode;
        shadowRenderTexture.useMipMap = false;

        shader = Shader.Find(shaderName);

        copyShadowBuffer = new CommandBuffer();
        copyShadowBuffer.name = "PCSS Shadows";
        var buffers = _light.GetCommandBuffers(lightEvent);
        for (int i = 0; i < buffers.Length; i++)
        {
            if(buffers[i].name == "PCSS Shadows")
            {
                _light.RemoveCommandBuffer(lightEvent, buffers[i]);
            }
        }

        _light.AddCommandBuffer(lightEvent, copyShadowBuffer);
        GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, shader);
        GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseCustom);

        //_cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        _cam.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals | DepthTextureMode.MotionVectors;

        ToggleLink(true);
    }

    public void ToggleLink (bool target)
    {
        if (target)
        {
            Camera.onPreRender -= UpdateCommandBuffer;
            Camera.onPreRender += UpdateCommandBuffer;
        }
        else
            Camera.onPreRender -= UpdateCommandBuffer;
    }

    public void OnDisable ()
    {
        ResetShadowMode();
    }

    [ContextMenu("Reset Shadows To Default")]
    public void ResetShadowMode ()
    {
        ToggleLink(false);

        GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, Shader.Find("Hidden/Internal-ScreenSpaceShadows"));
        GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.Disabled);
        _light.shadowCustomResolution = 0;
        DestroyImmediate(shadowRenderTexture);
        GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseBuiltin);

        if (!_light)
            return;

        _light.RemoveCommandBuffer(LightEvent.AfterShadowMap, copyShadowBuffer);
    }

    void UpdateCommandBuffer (Camera cam)
    {
        if (!_light)
            return;

        Shader.SetGlobalInt("Blocker_Samples", Blocker_SampleCount);
        Shader.SetGlobalInt("PCF_Samples", PCF_SampleCount);

        //Shader.SetGlobalFloat("Blocker_Rotation", Blocker_Rotation);
        //Shader.SetGlobalFloat("PCF_Rotation", PCF_Rotation);

        Shader.SetGlobalFloat("Softness", (Softness * Softness / 1024f));

        Shader.SetGlobalFloat("RECEIVER_PLANE_MIN_FRACTIONAL_ERROR", MaxStaticGradientBias);
        Shader.SetGlobalFloat("Blocker_GradientBias", Blocker_GradientBias);
        Shader.SetGlobalFloat("PCF_GradientBias", PCF_GradientBias);

        SetFlag("USE_CASCADE_BLENDING", CascadeBlendDistance > 0);
        Shader.SetGlobalFloat("CascadeBlendDistance", CascadeBlendDistance);

        SetFlag("USE_STATIC_BIAS", MaxStaticGradientBias > 0);
        SetFlag("USE_BLOCKER_BIAS", Blocker_GradientBias > 0);
        SetFlag("USE_PCF_BIAS", PCF_GradientBias > 0);
        SetFlag("ROTATE_SAMPLES", RotateSamples);

        int maxSamples = Mathf.Max(Blocker_SampleCount, PCF_SampleCount);

        SetFlag("POISSON_32", maxSamples < 33);
        SetFlag("POISSON_64", maxSamples > 32 && maxSamples < 65);
        SetFlag("POISSON_128", maxSamples > 64);

        copyShadowBuffer.Clear();

        copyShadowBuffer.SetShadowSamplingMode(BuiltinRenderTextureType.CurrentActive, ShadowSamplingMode.RawDepth);
        //copyShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, shadowRenderTexture2);
        //copyShadowBuffer.Blit(shadowRenderTexture2, shadowRenderTexture, blitMaterial);

        copyShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, shadowRenderTexture);
        //copyShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, shadowRenderTexture, blitMaterial);

        copyShadowBuffer.SetGlobalTexture("_ShadowMap", shadowRenderTexture);
    }

    public void SetFlag (string shaderKeyword, bool value)
    {
        if (value)
            Shader.EnableKeyword(shaderKeyword);
        else
            Shader.DisableKeyword(shaderKeyword);
    }
}