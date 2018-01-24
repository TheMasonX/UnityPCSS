// NVIDIA's PCSS (Percentage Closer Soft Shadows) implemented by TheMasonX by modifiying Unity's "Internal-ScreenSpaceShadows" shader.
// Copyright (c) 2016 Unity Technologies. MIT license applies to both the underlying shader and the PCSS modifications.

Shader "Hidden/PCSS"
{
Properties
{
	_ShadowMapTexture ("", any) = "" {}
}

CGINCLUDE
#include "UnityCG.cginc"
#include "UnityShadowLibrary.cginc"

// Configuration


// Should receiver plane bias be used? This estimates receiver slope using derivatives,
// and tries to tilt the PCF kernel along it. However, since we're doing it in screenspace
// from the depth texture, the derivatives are wrong on edges or intersections of objects,
// leading to possible shadow artifacts. So it's disabled by default.
uniform float RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.025;


struct appdata
{
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
#if (UNITY_VERSION < 560)
	float3 ray : NORMAL;
#elif defined(UNITY_STEREO_INSTANCING_ENABLED)
	float3 ray[2] : TEXCOORD1;
#else
	float3 ray : TEXCOORD1;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{

	float4 pos : SV_POSITION;

	// xy uv / zw screenpos
	float4 uv : TEXCOORD0;
	// View space ray, for perspective case
	float3 ray : TEXCOORD1;

#if defined(ORTHOGRAPHIC_SUPPORTED)
	// ORTHOGRAPHIC_SUPPORTED view space positions (need xy as well for oblique matrices)
	float3 orthoPosNear : TEXCOORD2;
	float3 orthoPosFar  : TEXCOORD3;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata v)
{
	v2f o;
#if UNITY_VERSION >= 560
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
#endif
	float4 clipPos = UnityObjectToClipPos(v.vertex);
	o.pos = clipPos;
	o.uv.xy = v.texcoord;

	// unity_CameraInvProjection at the PS level.
	o.uv.zw = ComputeNonStereoScreenPos(clipPos);

	// Perspective case
	//Only do stero instancing in 5.6+
#if (UNITY_VERSION >= 560) && defined(UNITY_STEREO_INSTANCING_ENABLED)
//	o.ray = v.ray[unity_StereoEyeIndex];
	o.ray = unity_StereoEyeIndex == 0 ? v.ray0 : v.ray1;
#else
	o.ray = v.ray;
#endif

#if defined(ORTHOGRAPHIC_SUPPORTED)
	// To compute view space position from Z buffer for ORTHOGRAPHIC_SUPPORTED case,
	// we need different code than for perspective case. We want to avoid
	// doing matrix multiply in the pixel shader: less operations, and less
	// constant registers used. Particularly with constant registers, having
	// unity_CameraInvProjection in the pixel shader would push the PS over SM2.0
	// limits.
	clipPos.y *= _ProjectionParams.x;
	float3 orthoPosNear = mul(unity_CameraInvProjection, float4(clipPos.x,clipPos.y,-1,1)).xyz;
	float3 orthoPosFar  = mul(unity_CameraInvProjection, float4(clipPos.x,clipPos.y, 1,1)).xyz;
	orthoPosNear.z *= -1;
	orthoPosFar.z *= -1;
	o.orthoPosNear = orthoPosNear;
	o.orthoPosFar = orthoPosFar;
#endif

	return o;
}

//changed in 5.6
#if UNITY_VERSION >= 560
	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#else
	sampler2D_float _CameraDepthTexture;
#endif

// sizes of cascade projections, relative to first one
float4 unity_ShadowCascadeScales;

UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
float4 _ShadowMapTexture_TexelSize;

//
// Keywords based defines
//
#if defined (SHADOWS_SPLIT_SPHERES)
	#define GET_CASCADE_WEIGHTS(wpos, z)    getCascadeWeights_splitSpheres(wpos)
#else
	#define GET_CASCADE_WEIGHTS(wpos, z)	getCascadeWeights( wpos, z )
#endif

#if defined (SHADOWS_SINGLE_CASCADE)
	#define GET_SHADOW_COORDINATES(wpos,cascadeWeights)	getShadowCoord_SingleCascade(wpos)
#else
	#define GET_SHADOW_COORDINATES(wpos,cascadeWeights)	getShadowCoord(wpos,cascadeWeights)
#endif

// prototypes 
inline float3 computeCameraSpacePosFromDepth(v2f i);
inline fixed4 getCascadeWeights(float3 wpos, float z);		// calculates the cascade weights based on the world position of the fragment and plane positions
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos);	// calculates the cascade weights based on world pos and split spheres positions
inline float4 getShadowCoord_SingleCascade( float4 wpos );	// converts the shadow coordinates for shadow map using the world position of fragment (optimized for single fragment)
inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights );// converts the shadow coordinates for shadow map using the world position of fragment

/**
 * Gets the cascade weights based on the world position of the fragment.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights(float3 wpos, float z)
{
	fixed4 zNear = float4( z >= _LightSplitsNear );
	fixed4 zFar = float4( z < _LightSplitsFar );
	fixed4 weights = zNear * zFar;
	return weights;
}

/**
 * Gets the cascade weights based on the world position of the fragment and the poisitions of the split spheres for each cascade.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos)
{
	float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
	float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
	float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
	float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
	float4 distances2 = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
	fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
	weights.yzw = saturate(weights.yzw - weights.xyz);
	return weights;
}

/**
 * Returns the shadowmap coordinates for the given fragment based on the world position and z-depth.
 * These coordinates belong to the shadowmap atlas that contains the maps for all cascades.
 */
inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights )
{
	float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
	float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
	float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
	float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
	float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
#if defined(UNITY_REVERSED_Z)
	float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
	shadowMapCoordinate.z += noCascadeWeights;
#endif
	return shadowMapCoordinate;
}

/**
 * Same as the getShadowCoord; but optimized for single cascade
 */
inline float4 getShadowCoord_SingleCascade( float4 wpos )
{
	return float4( mul (unity_WorldToShadow[0], wpos).xyz, 0);
}

/**
 * Computes the receiver plane depth bias for the given shadow coord in screen space.
 * Inspirations: 
 *		http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/ 
 *		http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Isidoro-ShadowMapping.pdf
 */
float2 getReceiverPlaneDepthBias (float3 shadowCoord)
{
	float2 biasUV;
	float3 dx = ddx (shadowCoord);
	float3 dy = ddy (shadowCoord);

	biasUV.x = dy.y * dx.z - dx.y * dy.z;
    biasUV.y = dx.x * dy.z - dy.x * dx.z;
    biasUV *= 1.0f / ((dx.x * dy.y) - (dx.y * dy.x));
    return biasUV;
}

/**
* Get camera space coord from depth and inv projection matrices
*/
inline float3 computeCameraSpacePosFromDepthAndInvProjMat(v2f i)
{
	float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

	#if defined(UNITY_REVERSED_Z)
		zdepth = 1 - zdepth;
	#endif

	// View position calculation for oblique clipped projection case.
	// this will not be as precise nor as fast as the other method
	// (which computes it from interpolated ray & depth) but will work
	// with funky projections.
	float4 clipPos = float4(i.uv.zw, zdepth, 1.0);
	clipPos.xyz = 2.0f * clipPos.xyz - 1.0f;
	float4 camPos = mul(unity_CameraInvProjection, clipPos);
	camPos.xyz /= camPos.w;
	camPos.z *= -1;
	return camPos.xyz;
}

/**
* Get camera space coord from depth and info from VS
*/
inline float3 computeCameraSpacePosFromDepthAndVSInfo(v2f i)
{
	float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
	float3 vposPersp = (i.ray * Linear01Depth(zdepth)).xyz;

#if defined(UNITY_REVERSED_Z)
	zdepth = 1.0 - zdepth;
#endif

#if defined(ORTHOGRAPHIC_SUPPORTED)
	float3 vposOrtho = lerp(i.orthoPosNear, i.orthoPosFar, zdepth);
	return lerp(vposPersp, vposOrtho, unity_OrthoParams.w);
#else
	return vposPersp;
#endif
}


//PCSS --------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------

uniform float Blocker_Samples = 32;
uniform float PCF_Samples = 32;

uniform float Blocker_Rotation = .5;
uniform float PCF_Rotation = .5;

uniform float Softness = 1.0;
uniform float SoftnessFalloff = 1.0;
//uniform float NearPlane = .1;

uniform float Blocker_GradientBias = 0.0;
uniform float PCF_GradientBias = 1.0;
uniform float CascadeBlendDistance = .5;

uniform float PenumbraWithMaxSamples = .15;

uniform sampler2D_float _ShadowMap;
float4 _ShadowMap_TexelSize;

uniform sampler2D _NoiseTexture;
uniform float4 NoiseCoords;

#if defined(POISSON_32)
static const float2 PoissonOffsets[32] = {
	float2(0.06407013, 0.05409927),
	float2(0.7366577, 0.5789394),
	float2(-0.6270542, -0.5320278),
	float2(-0.4096107, 0.8411095),
	float2(0.6849564, -0.4990818),
	float2(-0.874181, -0.04579735),
	float2(0.9989998, 0.0009880066),
	float2(-0.004920578, -0.9151649),
	float2(0.1805763, 0.9747483),
	float2(-0.2138451, 0.2635818),
	float2(0.109845, 0.3884785),
	float2(0.06876755, -0.3581074),
	float2(0.374073, -0.7661266),
	float2(0.3079132, -0.1216763),
	float2(-0.3794335, -0.8271583),
	float2(-0.203878, -0.07715034),
	float2(0.5912697, 0.1469799),
	float2(-0.88069, 0.3031784),
	float2(0.5040108, 0.8283722),
	float2(-0.5844124, 0.5494877),
	float2(0.6017799, -0.1726654),
	float2(-0.5554981, 0.1559997),
	float2(-0.3016369, -0.3900928),
	float2(-0.5550632, -0.1723762),
	float2(0.925029, 0.2995041),
	float2(-0.2473137, 0.5538505),
	float2(0.9183037, -0.2862392),
	float2(0.2469421, 0.6718712),
	float2(0.3916397, -0.4328209),
	float2(-0.03576927, -0.6220032),
	float2(-0.04661255, 0.7995201),
	float2(0.4402924, 0.3640312),
};

#else
static const float2 PoissonOffsets[64] = {
	float2(0.0617981, 0.07294159),
	float2(0.6470215, 0.7474022),
	float2(-0.5987766, -0.7512833),
	float2(-0.693034, 0.6913887),
	float2(0.6987045, -0.6843052),
	float2(-0.9402866, 0.04474335),
	float2(0.8934509, 0.07369385),
	float2(0.1592735, -0.9686295),
	float2(-0.05664673, 0.995282),
	float2(-0.1203411, -0.1301079),
	float2(0.1741608, -0.1682285),
	float2(-0.09369049, 0.3196758),
	float2(0.185363, 0.3213367),
	float2(-0.1493771, -0.3147511),
	float2(0.4452095, 0.2580113),
	float2(-0.1080467, -0.5329178),
	float2(0.1604507, 0.5460774),
	float2(-0.4037193, -0.2611179),
	float2(0.5947998, -0.2146744),
	float2(0.3276062, 0.9244621),
	float2(-0.6518704, -0.2503952),
	float2(-0.3580975, 0.2806469),
	float2(0.8587891, 0.4838005),
	float2(-0.1596546, -0.8791054),
	float2(-0.3096867, 0.5588146),
	float2(-0.5128918, 0.1448544),
	float2(0.8581337, -0.424046),
	float2(0.1562584, -0.5610626),
	float2(-0.7647934, 0.2709858),
	float2(-0.3090832, 0.9020988),
	float2(0.3935608, 0.4609676),
	float2(0.3929337, -0.5010948),
	float2(-0.8682281, -0.1990303),
	float2(-0.01973724, 0.6478714),
	float2(-0.3897587, -0.4665619),
	float2(-0.7416366, -0.4377831),
	float2(-0.5523247, 0.4272514),
	float2(-0.5325066, 0.8410385),
	float2(0.3085465, -0.7842533),
	float2(0.8400612, -0.200119),
	float2(0.6632416, 0.3067062),
	float2(-0.4462856, -0.04265022),
	float2(0.06892014, 0.812484),
	float2(0.5149567, -0.7502338),
	float2(0.6464897, -0.4666451),
	float2(-0.159861, 0.1038342),
	float2(0.6455986, 0.04419327),
	float2(-0.7445076, 0.5035095),
	float2(0.9430245, 0.3139912),
	float2(0.0349884, -0.7968109),
	float2(-0.9517487, 0.2963554),
	float2(-0.7304786, -0.01006928),
	float2(-0.5862702, -0.5531025),
	float2(0.3029106, 0.09497032),
	float2(0.09025345, -0.3503742),
	float2(0.4356628, -0.0710125),
	float2(0.4112572, 0.7500054),
	float2(0.3401214, -0.3047142),
	float2(-0.2192158, -0.6911137),
	float2(-0.4676369, 0.6570358),
	float2(0.6295372, 0.5629555),
	float2(0.1253822, 0.9892166),
	float2(-0.1154335, 0.8248222),
	float2(-0.4230408, -0.7129914),
};
#endif

/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++    Helper Methods    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

inline float ValueNoise(float3 pos)
{
	float3 Noise_skew = pos + 0.2127 + pos.x * pos.y * pos.z * 0.3713;
	float3 Noise_rnd = 4.789 * sin(489.123 * (Noise_skew));
	return frac(Noise_rnd.x * Noise_rnd.y * Noise_rnd.z * (1.0 + Noise_skew.x));
}

inline float2 Rotate(float2 pos, float2 rotationTrig)
{
	return float2(pos.x * rotationTrig.x - pos.y * rotationTrig.y, pos.y * rotationTrig.x + pos.x * rotationTrig.y);
}

inline float SampleShadowmapDepth(float2 uv)
{
	return tex2Dlod(_ShadowMap, float4(uv, 0.0, 0.0)).r;
}

inline float SampleShadowmap_Soft(float4 coord)
{
	return UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord);
}

inline float SampleShadowmap(float4 coord)
{
	float depth = SampleShadowmapDepth(coord.xy);
	return step(depth, coord.z);
}

/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++    Find Blocker    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

float2 FindBlocker(float2 uv, float depth, float searchUV, float2 receiverPlaneDepthBias, float2 rotationTrig)
{
	float avgBlockerDepth = 0.0;
	float numBlockers = 0.0;
	float blockerSum = 0.0;

	for (int i = 0; i < Blocker_Samples; i++)
	{
		float2 offset = PoissonOffsets[i] * searchUV;

//#if defined(ROTATE_SAMPLES)
		offset = Rotate(offset, rotationTrig);
//#endif

		float shadowMapDepth = SampleShadowmapDepth(uv + offset);

		float biasedDepth = depth;

#if defined(USE_BLOCKER_BIAS)
		biasedDepth += dot(offset, receiverPlaneDepthBias) * Blocker_GradientBias;
#endif

#if defined(UNITY_REVERSED_Z)
		if (shadowMapDepth > biasedDepth)
#else
		if (shadowMapDepth < biasedDepth)
#endif
		{
			blockerSum += shadowMapDepth;
			numBlockers += 1.0;
		}
	}

	avgBlockerDepth = blockerSum / numBlockers;

#if defined(UNITY_REVERSED_Z)
	avgBlockerDepth = 1.0 - avgBlockerDepth;
#endif

	return float2(avgBlockerDepth, numBlockers);
}

/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++    PCF Sampling    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

float PCF_Filter(float2 uv, float depth, float filterRadiusUV, float2 receiverPlaneDepthBias, float penumbra, float2 rotationTrig)
{
	float sum = 0.0f;
#if defined(UNITY_REVERSED_Z)
	receiverPlaneDepthBias *= -1.0;
#endif

	//float penumbraPercent = saturate(penumbra / PenumbraWithMaxSamples);
	//int samples = ceil(penumbraPercent * PCF_Samples);
	////int samples = ceil((1.0 - (penumbraPercent * penumbraPercent)) * PCF_Samples);
	//samples = PCF_Samples;


	//for (int i = 0; i < samples; i++)
	for (int i = 0; i < PCF_Samples; i++)
	{
		float2 offset = PoissonOffsets[i] * filterRadiusUV;

//#if defined(ROTATE_SAMPLES)
		offset = Rotate(offset, rotationTrig);
//#endif

		float biasedDepth = depth;

#if defined(USE_PCF_BIAS)
		biasedDepth += dot(offset, receiverPlaneDepthBias) * PCF_GradientBias;
#endif

		float value = SampleShadowmap_Soft(float4(uv.xy + offset, biasedDepth, 0));

		sum += value;
	}

	//sum /= samples;
	sum /= PCF_Samples;

	return sum;
}


/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++++    PCSS Main    ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

float PCSS_Main(float4 coords, float2 receiverPlaneDepthBias, float random)
{
	float2 uv = coords.xy;
	float depth = coords.z;
	float zAwareDepth = depth;

#if defined(UNITY_REVERSED_Z)
	zAwareDepth = 1.0 - depth;
#endif

	//float rotationAngle = random * 6.283185307179586476925286766559;
	float rotationAngle = random * 3.1415926;
	float2 rotationTrig = float2(cos(rotationAngle), sin(rotationAngle));

#if defined(UNITY_REVERSED_Z)
	receiverPlaneDepthBias *= -1.0;
#endif

	// STEP 1: blocker search
	//float searchSize = Softness * (depth - _LightShadowData.w) / depth;
	float searchSize = Softness * saturate(zAwareDepth - .02) / zAwareDepth;
	float2 blockerInfo = FindBlocker(uv, depth, searchSize, receiverPlaneDepthBias, rotationTrig);

	if (blockerInfo.y < 1)
	{
		//There are no occluders so early out (this saves filtering)
		return 1.0;
	}

	// STEP 2: penumbra size
	//float penumbra = zAwareDepth * zAwareDepth - blockerInfo.x * blockerInfo.x;
	float penumbra = zAwareDepth - blockerInfo.x;

#if defined(USE_FALLOFF)
	penumbra = 1.0 - pow(1.0 - penumbra, SoftnessFalloff);
#endif

	float filterRadiusUV = penumbra * Softness;
	//filterRadiusUV *= filterRadiusUV;

	// STEP 3: filtering
	float shadow = PCF_Filter(uv, depth, filterRadiusUV, receiverPlaneDepthBias, penumbra, rotationTrig);
	return lerp(_LightShadowData.r, 1.0f, shadow);
}




//END PCSS ----------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------


/**
 *	Hard shadow 
 */
fixed4 frag_hard (v2f i) : SV_Target
{
	//only works in 5.6+
#if UNITY_VERSION >= 560
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // required for sampling the correct slice of the shadow map render texture array
#endif

	float3 vpos = computeCameraSpacePosFromDepth(i);

	float4 wpos = mul (unity_CameraToWorld, float4(vpos,1));

	fixed4 cascadeWeights = GET_CASCADE_WEIGHTS (wpos, vpos.z);
	half shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, GET_SHADOW_COORDINATES(wpos, cascadeWeights));

	return lerp(_LightShadowData.r, 1.0, shadow);
}

/**
 *	Soft Shadow Frag
 */
fixed4 frag_pcss (v2f i) : SV_Target
{
	//only works in 5.6+
#if UNITY_VERSION >= 560
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // required for sampling the correct slice of the shadow map render texture array
#endif

	float3 vpos = computeCameraSpacePosFromDepth(i);

	// sample the cascade the pixel belongs to
	float4 wpos = mul(unity_CameraToWorld, float4(vpos,1));
	fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z);
	float4 coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);

//#if defined(USE_NOISE_TEX)
	float random = tex2D(_NoiseTexture, i.uv.xy * NoiseCoords.xy * _ScreenParams.xy).a;
	random = mad(random, 2.0, -1.0);
//#else
//	float random = ValueNoise(wpos.xyz);
//#endif

	float2 receiverPlaneDepthBiasCascade0 = 0.0;
	float2 receiverPlaneDepthBias = 0.0;

#if defined(USE_STATIC_BIAS) || defined(USE_BLOCKER_BIAS) || defined(USE_PCF_BIAS)
	// Reveiver plane depth bias: need to calculate it based on shadow coordinate
	// as it would be in first cascade; otherwise derivatives
	// at cascade boundaries will be all wrong. So compute
	// it from cascade 0 UV, and scale based on which cascade we're in.
	// 
	float3 coordCascade0 = getShadowCoord_SingleCascade(wpos);
	receiverPlaneDepthBiasCascade0 = getReceiverPlaneDepthBias(coordCascade0.xyz);
	float biasMultiply = dot(cascadeWeights, unity_ShadowCascadeScales);
	receiverPlaneDepthBias = receiverPlaneDepthBiasCascade0 * biasMultiply;

#if defined(USE_STATIC_BIAS)
	// Static depth biasing to make up for incorrect fractional
	// sampling on the shadow map grid; from "A Sampling of Shadow Techniques"
	// (http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/)
	float fractionalSamplingError = 2.0 * dot(_ShadowMap_TexelSize.xy, abs(receiverPlaneDepthBias));
	fractionalSamplingError = min(fractionalSamplingError, RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);

#if defined(UNITY_REVERSED_Z)
	fractionalSamplingError *= -1.0;
#endif

	coord.z -= fractionalSamplingError;
#endif

#endif


	float shadow = PCSS_Main(coord, receiverPlaneDepthBias, random);


	// Blend between shadow cascades if enabled
	// Not working yet with split spheres, and no need when 1 cascade

//#if USE_CASCADE_BLENDING && !defined(SHADOWS_SPLIT_SPHERES) && !defined(SHADOWS_SINGLE_CASCADE)
#if defined(USE_CASCADE_BLENDING) && !defined(SHADOWS_SINGLE_CASCADE)
	half4 z4 = (float4(vpos.z,vpos.z,vpos.z,vpos.z) - _LightSplitsNear) / (_LightSplitsFar - _LightSplitsNear);
	half alpha = dot(z4 * cascadeWeights, half4(1,1,1,1));

	UNITY_BRANCH
	if (alpha > 1.0 - CascadeBlendDistance)
	{
		// get alpha to 0..1 range over the blend distance
		alpha = (alpha - (1.0 - CascadeBlendDistance)) / CascadeBlendDistance;

		// sample next cascade
		cascadeWeights = fixed4(0, cascadeWeights.xyz);
		coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);

#if defined(USE_STATIC_BIAS) || defined(USE_BLOCKER_BIAS) || defined(USE_PCF_BIAS)
		biasMultiply = dot(cascadeWeights, unity_ShadowCascadeScales);
		receiverPlaneDepthBias = receiverPlaneDepthBiasCascade0 * biasMultiply;

#if defined(USE_STATIC_BIAS)
		fractionalSamplingError = 2.0 * dot(_ShadowMap_TexelSize.xy, abs(receiverPlaneDepthBias));
		fractionalSamplingError = min(fractionalSamplingError, RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);

#if defined(UNITY_REVERSED_Z)
		fractionalSamplingError *= -1.0;
#endif
		
		coord.z -= fractionalSamplingError;
#endif

#endif

		float shadowNextCascade = PCSS_Main(coord, receiverPlaneDepthBias, random);

		shadow = lerp(shadow, shadowNextCascade, saturate(alpha));
	}
#endif

	return shadow;
}
ENDCG


// ----------------------------------------------------------------------------------------
// Subshader for hard shadows:
// Just collect shadows into the buffer. Used on pre-SM3 GPUs and when hard shadows are picked.

SubShader
{
	Tags { "ShadowmapFilter" = "HardShadow" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_hard
		#pragma multi_compile_shadowcollector

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}

// ----------------------------------------------------------------------------------------
// Subshader for hard shadows:
// Just collect shadows into the buffer. Used on pre-SM3 GPUs and when hard shadows are picked.
// This version does inv projection at the PS level, slower and less precise however more general.

SubShader
{
	Tags { "ShadowmapFilter" = "HardShadow_FORCE_INV_PROJECTION_IN_PS" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_hard
		#pragma multi_compile_shadowcollector

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}

// ----------------------------------------------------------------------------------------
//	Unity 2017
// ----------------------------------------------------------------------------------------
// NOTE: Same two subshaders as the Unity 2017 versions, but they changed the "ShadowmapFilter" tag names, so I've included both sets for now (wasn't having any luck with the UNITY_VERSION checks used elsewhere for 5.5 compatibility)
// PCSS Subshader, just had to leave the "PCF" tag so that Unity can find it
// Requires SM3 GPU.

Subshader
{
	Tags{ "ShadowmapFilter" = "PCF_SOFT" }

	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcss
		#pragma multi_compile_shadowcollector
		#pragma multi_compile POISSON_32 POISSON_64

		#pragma shader_feature USE_FALLOFF
		#pragma shader_feature USE_CASCADE_BLENDING
		#pragma shader_feature USE_STATIC_BIAS
		#pragma shader_feature USE_BLOCKER_BIAS
		#pragma shader_feature USE_PCF_BIAS
		#pragma shader_feature ORTHOGRAPHIC_SUPPORTED
//		#pragma shader_feature ROTATE_SAMPLES
//		#pragma shader_feature USE_NOISE_TEX
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}

// This version does inv projection at the PS level, slower and less precise however more general.
Subshader
{
	Tags{ "ShadowmapFilter" = "PCF_SOFT_FORCE_INV_PROJECTION_IN_PS" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcss
		#pragma multi_compile_shadowcollector
		#pragma multi_compile POISSON_32 POISSON_64

		#pragma shader_feature USE_FALLOFF
		#pragma shader_feature USE_CASCADE_BLENDING
		#pragma shader_feature USE_STATIC_BIAS
		#pragma shader_feature USE_BLOCKER_BIAS
		#pragma shader_feature USE_PCF_BIAS
		#pragma shader_feature ORTHOGRAPHIC_SUPPORTED
//		#pragma shader_feature ROTATE_SAMPLES
//		#pragma shader_feature USE_NOISE_TEX
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}

// ----------------------------------------------------------------------------------------
// Unity 5.6 and below
// ----------------------------------------------------------------------------------------

Subshader
{
	Tags{ "ShadowmapFilter" = "PCF_5x5" }

	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcss
		#pragma multi_compile_shadowcollector
		#pragma multi_compile POISSON_32 POISSON_64

		#pragma shader_feature USE_FALLOFF
		#pragma shader_feature USE_CASCADE_BLENDING
		#pragma shader_feature USE_STATIC_BIAS
		#pragma shader_feature USE_BLOCKER_BIAS
		#pragma shader_feature USE_PCF_BIAS
		#pragma shader_feature ORTHOGRAPHIC_SUPPORTED
//		#pragma shader_feature ROTATE_SAMPLES
//		#pragma shader_feature USE_NOISE_TEX
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}

// This version does inv projection at the PS level, slower and less precise however more general.
Subshader
{
	Tags{ "ShadowmapFilter" = "PCF_5x5_FORCE_INV_PROJECTION_IN_PS" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcss
		#pragma multi_compile_shadowcollector
		#pragma multi_compile POISSON_32 POISSON_64

		#pragma shader_feature USE_FALLOFF
		#pragma shader_feature USE_CASCADE_BLENDING
		#pragma shader_feature USE_STATIC_BIAS
		#pragma shader_feature USE_BLOCKER_BIAS
		#pragma shader_feature USE_PCF_BIAS
		#pragma shader_feature ORTHOGRAPHIC_SUPPORTED
//		#pragma shader_feature ROTATE_SAMPLES
//		#pragma shader_feature USE_NOISE_TEX
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}

Fallback Off
}
