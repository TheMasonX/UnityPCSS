using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PCSSDemo : MonoBehaviour
{
    public PCSSLight pcssScript;

    [Space(10f)]
    public Slider softnessSlider;
    public Text softnessText;

    [Space(10f)]
    public Slider softnessFalloffSlider;
    public Text softnessFalloffText;

    [Space(10f)]
    public Slider blockerSlider;
    public Text blockerText;

    [Space(10f)]
    public Slider pcfSlider;
    public Text pcfText;

	[Space(10f)]
	public Dropdown shadowMode;

	[Space(10f)]
	public Dropdown msaaMode;
	private Camera _camera;

    private void Awake ()
    {
        blockerSlider.value = pcssScript.Blocker_SampleCount;
        SetBlockerSamples(blockerSlider.value);

        pcfSlider.value = pcssScript.PCF_SampleCount;
        SetPCFSamples(pcfSlider.value);

        softnessSlider.value = pcssScript.Softness;
        SetSoftness(softnessSlider.value);

        softnessFalloffSlider.value = pcssScript.SoftnessFalloff;
        SetSoftnessFalloff(softnessFalloffSlider.value);

//		SetShadowMode(shadowMode.value);
		SetMSAAMode(msaaMode.value);
    }

    public void SetBlockerSamples (float samplesFloat)
    {
        int samples = Mathf.RoundToInt(samplesFloat);
        pcssScript.Blocker_SampleCount = samples;
        blockerText.text = string.Format("Blocker Samples: {0}", samples);
        pcssScript.UpdateShaderValues();
    }

    public void SetPCFSamples (float samplesFloat)
    {
        int samples = Mathf.RoundToInt(samplesFloat);
        pcssScript.PCF_SampleCount = samples;
        pcfText.text = string.Format("PCF Samples: {0}", samples);
        pcssScript.UpdateShaderValues();
    }

    public void SetSoftness (float softness)
    {
        pcssScript.Softness = softness;
        softnessText.text = string.Format("Softness: {0}", softness.ToString("N2"));
        pcssScript.UpdateShaderValues();
    }

    public void SetSoftnessFalloff (float softnessFalloff)
    {
        pcssScript.SoftnessFalloff = softnessFalloff;
        softnessFalloffText.text = string.Format("Softness Falloff: {0}", softnessFalloff.ToString("N2"));
        pcssScript.UpdateShaderValues();
    }

	public void SetShadowMode (int mode)
	{
		switch (mode)
		{
			case(0):
				pcssScript.ResetShadowMode();
				pcssScript._light.shadows = LightShadows.Soft;
				pcssScript.Setup();
				break;
			case(1):
				pcssScript.ResetShadowMode();
				pcssScript._light.shadows = LightShadows.Soft;
				break;
			case(2):
				pcssScript.ResetShadowMode();
				pcssScript._light.shadows = LightShadows.Hard;
				break;
		}
	}

	public void SetMSAAMode (int mode)
	{
		if (!_camera)
			_camera = Camera.main;
		if (!_camera)
			return;

		_camera.allowMSAA = mode > 0;
		switch (mode)
		{
			case(0):
				QualitySettings.antiAliasing = 0;
				break;
			case(1):
				QualitySettings.antiAliasing = 2;
				break;
			case(2):
				QualitySettings.antiAliasing = 4;
				break;
			case(3):
				QualitySettings.antiAliasing = 8;
				break;
		}
	}
}
