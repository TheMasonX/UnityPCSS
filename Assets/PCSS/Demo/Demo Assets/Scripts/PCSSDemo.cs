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
    public Slider blockerSlider;
    public Text blockerText;

    [Space(10f)]
    public Slider pcfSlider;
    public Text pcfText;

    private void Awake ()
    {
        blockerSlider.value = pcssScript.Blocker_SampleCount;
        SetBlockerSamples(blockerSlider.value);

        pcfSlider.value = pcssScript.PCF_SampleCount;
        SetPCFSamples(pcfSlider.value);

        softnessSlider.value = pcssScript.Softness;
        SetSoftness(softnessSlider.value);
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
}
