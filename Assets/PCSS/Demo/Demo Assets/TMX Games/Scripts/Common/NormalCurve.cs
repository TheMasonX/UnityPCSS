using System.Collections;
using System.Collections.Generic;
using TMX.Utils;
using UnityEngine;

namespace TMX.Common
{
    [System.Serializable]
    public class NormalCurve
    {
        #region Public Variables

        public AnimationCurve curve;
        public Vector2 inputRange;
        public Vector2 outputRange;

        #endregion

        #region Constructors

        public NormalCurve ()
        {
            inputRange = outputRange = new Vector2(0f, 1f);
            curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        }

        #endregion

        #region Public Methods

        public float Evaluate (float t)
        {
            return Interpolation.CurveClamped(curve, outputRange, MathUtils.PercentBetween(t, inputRange));
        }

        public float RandomValue ()
        {
            return Interpolation.CurveClamped(curve, outputRange, Random.value);
        }

        #endregion
    }
}