using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMX.Utils
{
    public static class Interpolation
    {
        #region Hermite

        /// <summary>
        /// Hermite interpolation between 0 and 1
        /// </summary>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Hermite (float t)
        {
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Hermite interpolation between 'a' and 'b'
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Hermite (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, Hermite(t));
        }

        /// <summary>
        /// Hermite interpolation between 0 and 1, autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float HermiteClamped (float t)
        {
            return Hermite(Mathf.Clamp01(t));
        }

        /// <summary>
        /// Hermite interpolation between 'a' and 'b', autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float HermiteClamped (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, HermiteClamped(t));
        }

        #endregion

        //=============================================================================================================================================================================================================================================

        #region Quintic

        /// <summary>
        /// Quintic interpolation between 0 and 1
        /// </summary>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Quintic (float t)
        {
            return t * t * t * (10f + (-15f + 6f * t) * t);
        }

        /// <summary>
        /// Quintic interpolation between 'a' and 'b'
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Quintic (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, Quintic(t));
        }

        /// <summary>
        /// Quintic interpolation between 0 and 1, autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float QuinticClamped (float t)
        {
            return Quintic(Mathf.Clamp01(t));
        }

        /// <summary>
        /// Quintic interpolation between 'a' and 'b', autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float QuinticClamped (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, QuinticClamped(t));
        }

        #endregion

        //=============================================================================================================================================================================================================================================

        #region Coserp

        /// <summary>
        /// Coserp interpolation (ease in) between 0 and 1
        /// </summary>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Coserp (float t)
        {
            return 1f - Mathf.Cos(t * Mathf.PI * .5f);
        }

        /// <summary>
        /// Coserp interpolation (ease in) between 'a' and 'b'
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Coserp (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, Coserp(t));
        }

        /// <summary>
        /// Coserp interpolation (ease in) between 0 and 1, autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float CoserpClamped (float t)
        {
            return Coserp(Mathf.Clamp01(t));
        }

        /// <summary>
        /// Coserp interpolation (ease in) between 'a' and 'b', autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float CoserpClamped (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, CoserpClamped(t));
        }

        #endregion

        //=============================================================================================================================================================================================================================================

        #region Sinerp

        /// <summary>
        /// Sinerp interpolation (ease out) between 0 and 1
        /// </summary>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Sinerp (float t)
        {
            return Mathf.Sin(t * Mathf.PI * .5f);
        }

        /// <summary>
        /// Sinerp interpolation (ease out) between 'a' and 'b'
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Sinerp (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, Sinerp(t));
        }

        /// <summary>
        /// Sinerp interpolation (ease out) between 0 and 1, autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float SinerpClamped (float t)
        {
            return Sinerp(Mathf.Clamp01(t));
        }

        /// <summary>
        /// Sinerp interpolation (ease out) between 'a' and 'b', autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float SinerpClamped (float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, SinerpClamped(t));
        }

        #endregion

        //=============================================================================================================================================================================================================================================

        #region Curve

        /// <summary>
        /// Interpolate between 0 and 1 using an animation curve
        /// </summary>
        /// <param name="curve">The animation curve to use</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Curve (AnimationCurve curve, float t)
        {
            return curve.Evaluate(t);
        }

        /// <summary>
        /// Interpolate between 'a' and 'b' using an animation curve
        /// </summary>
        /// <param name="curve">The animation curve to use</param>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Curve (AnimationCurve curve, float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, Curve(curve, t));
        }

        /// <summary>
        /// Interpolate between 'a' and 'b' using an animation curve
        /// </summary>
        /// <param name="curve">The animation curve to use</param>
        /// <param name="range">The interpolation endpoints</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float Curve (AnimationCurve curve, Vector2 range, float t)
        {
            return Mathf.LerpUnclamped(range.x, range.y, Curve(curve, t));
        }

        /// <summary>
        /// Interpolate between 0 and 1 using an animation curve, autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="curve">The animation curve to use</param>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float CurveClamped (AnimationCurve curve, float t)
        {
            return Curve(curve, Mathf.Clamp01(t));
        }

        /// <summary>
        /// Interpolate between 'a' and 'b' using an animation curve, autoclamping the 't' value to [0..1]
        /// </summary>
        /// <param name="curve">The animation curve to use</param>
        /// <param name="a">The starting value</param>
        /// <param name="b">The final value</param>
        /// <param name="t">The interpolation amount. Clamped to range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float CurveClamped (AnimationCurve curve, float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, CurveClamped(curve, t));
        }

        /// <summary>
        /// Interpolate between 'a' and 'b' using an animation curve
        /// </summary>
        /// <param name="curve">The animation curve to use</param>
        /// <param name="range">The interpolation endpoints</param>
        /// <param name="t">The interpolation amount. Required range [0..1]</param>
        /// <returns>The interpolated value [0..1]</returns>
        public static float CurveClamped (AnimationCurve curve, Vector2 range, float t)
        {
            return Mathf.LerpUnclamped(range.x, range.y, CurveClamped(curve, t));
        }

        #endregion
    }
}