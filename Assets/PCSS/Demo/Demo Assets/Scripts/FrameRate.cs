using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FrameRate : MonoBehaviour
{
    public Text graphicsAPIText;
    public Text vsyncStatusText;
    public Text frameRateText;

    public float updateInterval = 1f;

    public int frameBufferSize = 120;
    public string frameRateStringType = "N0";

    [Range(0f, .5f)]
    public float minMaxPercent;

    private float lastTime;
    private float deltaTime;

    private float avgFPS;
    private float minFPS;
    private float maxFPS;
    private float lowFPS;
    private float highFPS;

    private float[] frameBuffer;
    private List<float> sortedFrames;
    private int frameBufferIndex;

    private void Awake ()
    {
        frameBuffer = new float[frameBufferSize];
        Invoke("UpdateUI", updateInterval);
        Application.targetFrameRate = 1000;
    }

    private void UpdateUI ()
    {
        avgFPS = 0;
        minFPS = 100000;
        maxFPS = 0;

        //sortedFrames = frameBuffer.OrderBy(f => f).ToList();
        //int lowHighCount = Mathf.CeilToInt(sortedFrames.Count * minMaxPercent);
        //lowFPS = highFPS = 0;

        //for (int i = 0; i < lowHighCount; i++)
        //{
        //    lowFPS += sortedFrames[i];
        //}
        //for (int i = 1; i < lowHighCount + 1; i++)
        //{
        //    highFPS += sortedFrames[sortedFrames.Count - i];
        //}

        //lowFPS /= lowHighCount;
        //highFPS /= lowHighCount;


        for (int i = 0; i < frameBuffer.Length; i++)
        {
            avgFPS += frameBuffer[i];
            minFPS = Mathf.Min(minFPS, frameBuffer[i]);
            maxFPS = Mathf.Max(maxFPS, frameBuffer[i]);
        }
        avgFPS /= frameBuffer.Length;

        DebugExt.stringType = frameRateStringType;

        graphicsAPIText.text = string.Format("Graphics API: {0}", SystemInfo.graphicsDeviceType);
        string vysncText = (QualitySettings.vSyncCount > 0) ? "Enabled" : "Disabled";
        vsyncStatusText.text = string.Format("VSync: {0}", vysncText);
        frameRateText.text = string.Format("Average Frame Rate: {0} FPS", avgFPS.TS());
        //frameRateText.text = string.Format("Frame Rate: Avg {0} | Min {1} | Max {2}", avgFPS.TS(), minFPS.TS(), maxFPS.TS());
        //frameRateText.text = string.Format("Frame Rate: Avg {0} | Min {1} | Max {2} | Low {3} | High {4}", avgFPS.TS(), minFPS.TS(), maxFPS.TS(), lowFPS.TS(), highFPS.TS());
        Invoke("UpdateUI", updateInterval);
    }

    private void LateUpdate ()
    {
        frameBuffer[frameBufferIndex] = GetCurrentFrameRate();
        frameBufferIndex = (frameBufferIndex + 1) % frameBuffer.Length;
    }

    private float GetCurrentFrameRate ()
    {
        deltaTime = Time.realtimeSinceStartup - lastTime;
        lastTime = Time.realtimeSinceStartup;
        return 1f / deltaTime;
    }
}

static class DebugExt
{
    public static string stringType;

    public static string TS (this float value)
    {
        return value.ToString(stringType);
    }
}
