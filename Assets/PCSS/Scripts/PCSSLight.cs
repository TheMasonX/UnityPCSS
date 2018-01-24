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
    [Range(1, 64)]
    public int Blocker_SampleCount = 16;
    [Range(1, 64)]
    public int PCF_SampleCount = 16;

    [Space(20f)]
//    public bool RotateSamples = true;
//    public bool UseNoiseTexture = true;
    public Texture2D noiseTexture;

    [Space(20f)]
    [Range(0f, 7.5f)]
    public float Softness = 1f;
    [Range(0f, 5f)]
    public float SoftnessFalloff = 4f;
    //[Range(0f, 1f)]
    //public float NearPlane = .1f;

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
    public bool supportOrthographicProjection;

    [Space(20f)]
    public RenderTexture shadowRenderTexture;
    public RenderTextureFormat format = RenderTextureFormat.RFloat;
	public FilterMode filterMode = FilterMode.Bilinear;
	public enum antiAliasing
	{
		None = 1,
		Two = 2,
		Four = 4,
		Eight = 8,
	}
	public antiAliasing MSAA = antiAliasing.None;
    private LightEvent lightEvent = LightEvent.AfterShadowMap;

    public string shaderName = "Hidden/PCSS";
	public Shader shader;
	public string builtinShaderName = "Hidden/Built-In-ScreenSpaceShadows";
	public Shader builtinShader;
    private int shadowmapPropID;
	public ShaderVariantCollection shaderVariants;
	private List<string> keywords = new List<string>();

    private CommandBuffer copyShadowBuffer;
	[HideInInspector]
	public Light _light;

    #region Initialization
    public void OnEnable ()
    {
        Setup();
    }

    public void OnDisable ()
    {
        ResetShadowMode();
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

        shader = Shader.Find(shaderName);
//		if (!Application.isEditor)
//		{
//			if (shader)
//				Debug.LogErrorFormat("Custom Shadow Shader Found: {0} | Supported {1}", shader.name, shader.isSupported);
//			else
//				Debug.LogError("Custom Shadow Shader Not Found!!!");
//		}
        shadowmapPropID = Shader.PropertyToID("_ShadowMap");

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

        CreateShadowRenderTexture();
        UpdateShaderValues();
        UpdateCommandBuffer();
    }

    public void CreateShadowRenderTexture ()
    {
        shadowRenderTexture = new RenderTexture(resolution, resolution, 0, format);
        shadowRenderTexture.filterMode = filterMode;
        shadowRenderTexture.useMipMap = false;
		shadowRenderTexture.antiAliasing = (int)MSAA;
    }

    [ContextMenu("Reset Shadows To Default")]
    public void ResetShadowMode ()
    {
		builtinShader = Shader.Find(builtinShaderName);
//		if (!Application.isEditor)
//		{
//			if (builtinShader)
//				Debug.LogErrorFormat("Built-In Shadow Shader Found: {0} | Supported {1}", builtinShader.name, builtinShader.isSupported);
//			else
//				Debug.LogError("Shadow Shader Not Found!!!");
//		}

		GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, builtinShader);
        GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.Disabled);
        _light.shadowCustomResolution = 0;
        DestroyImmediate(shadowRenderTexture);
        GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseBuiltin);

        if (!_light)
            return;

        _light.RemoveCommandBuffer(LightEvent.AfterShadowMap, copyShadowBuffer);
    }
    #endregion

    #region UpdateSettings
    public void UpdateShaderValues ()
    {
		keywords.Clear();
        Shader.SetGlobalInt("Blocker_Samples", Blocker_SampleCount);
        Shader.SetGlobalInt("PCF_Samples", PCF_SampleCount);

        if (shadowRenderTexture)
        {
			if (shadowRenderTexture.format != format || shadowRenderTexture.antiAliasing != (int)MSAA)
				CreateShadowRenderTexture();
			else
			{
				shadowRenderTexture.filterMode = filterMode;
			}
        }

        Shader.SetGlobalFloat("Softness", Softness / 64f / Mathf.Sqrt(QualitySettings.shadowDistance));
        Shader.SetGlobalFloat("SoftnessFalloff", Mathf.Exp(SoftnessFalloff));
        SetFlag("USE_FALLOFF", SoftnessFalloff > Mathf.Epsilon);
        //Shader.SetGlobalFloat("NearPlane", NearPlane);

        Shader.SetGlobalFloat("RECEIVER_PLANE_MIN_FRACTIONAL_ERROR", MaxStaticGradientBias);
        Shader.SetGlobalFloat("Blocker_GradientBias", Blocker_GradientBias);
        Shader.SetGlobalFloat("PCF_GradientBias", PCF_GradientBias);

        SetFlag("USE_CASCADE_BLENDING", CascadeBlendDistance > 0);
        Shader.SetGlobalFloat("CascadeBlendDistance", CascadeBlendDistance);

        SetFlag("USE_STATIC_BIAS", MaxStaticGradientBias > 0);
        SetFlag("USE_BLOCKER_BIAS", Blocker_GradientBias > 0);
        SetFlag("USE_PCF_BIAS", PCF_GradientBias > 0);

//        SetFlag("ROTATE_SAMPLES", RotateSamples);
//        SetFlag("USE_NOISE_TEX", UseNoiseTexture);

        if (noiseTexture)
        {
            Shader.SetGlobalVector("NoiseCoords", new Vector4(1f / noiseTexture.width, 1f / noiseTexture.height, 0f, 0f));
            Shader.SetGlobalTexture("_NoiseTexture", noiseTexture);
        }

        SetFlag("ORTHOGRAPHIC_SUPPORTED", supportOrthographicProjection);

        int maxSamples = Mathf.Max(Blocker_SampleCount, PCF_SampleCount);

        SetFlag("POISSON_32", maxSamples < 33);
        SetFlag("POISSON_64", maxSamples > 33);

		if (shaderVariants)
			shaderVariants.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.Normal, keywords.ToArray()));
    }

    public void UpdateCommandBuffer ()
    {
        if (!_light)
            return;

        copyShadowBuffer.Clear();
        copyShadowBuffer.SetShadowSamplingMode(BuiltinRenderTextureType.CurrentActive, ShadowSamplingMode.RawDepth);
        copyShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, shadowRenderTexture);
        copyShadowBuffer.SetGlobalTexture(shadowmapPropID, shadowRenderTexture);
    }
    #endregion

    public void SetFlag (string shaderKeyword, bool value)
    {
		if (value)
		{
			Shader.EnableKeyword(shaderKeyword);
			keywords.Add(shaderKeyword);
		}
        else
            Shader.DisableKeyword(shaderKeyword);
    }
}