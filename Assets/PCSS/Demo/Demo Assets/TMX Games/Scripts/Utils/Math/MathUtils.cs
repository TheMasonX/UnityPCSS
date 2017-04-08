using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMX.Utils
{
    public static class MathUtils
    {
        #region PercentBetween

        public static float PercentBetween (float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        public static float PercentBetween (float value, Vector2 range)
        {
            return PercentBetween(value, range.x, range.y);
        }

        public static float PercentBetweenClamped (float value, float min, float max)
        {
            return Mathf.Clamp((value - min) / (max - min), min, max);
        }

        public static float PercentBetweenClamped (float value, Vector2 range)
        {
            return PercentBetweenClamped(value, range.x, range.y);
        }

        #endregion

        #region Remap

        public static float Remap (float value, float inMin, float inMax, float outMin, float outMax)
        {
            value = PercentBetween(value, inMin, inMax);
            return Mathf.LerpUnclamped(outMin, outMax, value);
        }

        public static float Remap (float value, Vector2 inRange, Vector2 outRange)
        {
            return Remap(value, inRange.x, inRange.y, outRange.x, outRange.y);
        }

        public static float RemapClamped (float value, float inMin, float inMax, float outMin, float outMax)
        {
            value = PercentBetweenClamped(value, inMin, inMax);
            return Mathf.LerpUnclamped(outMin, outMax, value);
        }

        public static float RemapClamped (float value, Vector2 inRange, Vector2 outRange)
        {
            return RemapClamped(value, inRange.x, inRange.y, outRange.x, outRange.y);
        }

        #endregion

    }
}
