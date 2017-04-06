using System.Collections;
using System.Collections.Generic;
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

    private float lastTime;
    private float deltaTime;

    private float avgFPS;
    private float minFPS;
    private float maxFPS;

    private float[] frameBuffer;
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
        for (int i = 0; i < frameBuffer.Length; i++)
        {
            avgFPS += frameBuffer[i];
            minFPS = Mathf.Min(minFPS, frameBuffer[i]);
            maxFPS = Mathf.Max(maxFPS, frameBuffer[i]);
        }
        avgFPS /= frameBuffer.Length;

        graphicsAPIText.text = string.Format("Graphics API: {0}", SystemInfo.graphicsDeviceType);
        vsyncStatusText.text = string.Format("VSync: {0}", QualitySettings.vSyncCount > 0 ? "Enabled" : "Disabled");
        frameRateText.text = string.Format("Frame Rate: Avg {0} | Min {1} | Max {2}", avgFPS.ToString(frameRateStringType), minFPS.ToString(frameRateStringType), maxFPS.ToString(frameRateStringType));
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
