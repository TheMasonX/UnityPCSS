Shader "Custom/Vegetation"
{
	Properties
	{
        _MainTex ("Base Color", 2D) = "white" {}
		[Normal] [NoScaleOffset]
		_NormalMap ("Normal Map", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Gloss ("Gloss", Range(0, 1)) = 0.1336244
		
		[Space(10)] [NoScaleOffset]
		_WindNoise ("Wind Noise", 2D) = "gray" {}
		_WindNoiseScale ("Wind Noise Position Scale", Float ) = .1
		_WindNoiseSpeed ("Wind Noise Speed", Float ) = .1
		_WindNoiseStrength ("Wind Noise Strength", Range(0,10)) = .3

		[Space(10)] 
		_Wind_Direction ("Wind_Direction", Vector) = (1,0,1,0)
        _WindFrequency ("Wind Frequency", Float ) = 1.5
        _WindAmplitude ("Wind Amplitude", Float ) = 0.03
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader
	{
		Tags { "RenderType" = "TreeTransparentCutout" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard vertex:vert alphatest:_Cutoff addshadow fullforwardshadows

		#pragma target 3.0

		sampler2D _MainTex; uniform float4 _MainTex_ST;
		sampler2D _NormalMap;
		sampler2D _WindNoise;

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 color : COLOR;
			float2 uv : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
			float2 texcoord2 : TEXCOORD2;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Input
		{
			float4 pos : SV_POSITION;
			float2 texcoord : TEXCOORD0;

			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

		fixed4 _Wind_Direction;
		half _Metallic;
		half _Gloss;
		float _WindNoiseScale;
		float _WindNoiseSpeed;
		float _WindNoiseStrength;

		float _WindFrequency;
		float _WindAmplitude;

		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void vert(inout appdata v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_TRANSFER_INSTANCE_ID(v, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			o.texcoord = TRANSFORM_TEX(v.uv, _MainTex);

			float3 pos = mul(v.vertex, unity_ObjectToWorld);
			float noiseA = tex2Dlod(_WindNoise, float4(pos.xz * _WindNoiseScale + _Time.x * float2(_WindNoiseSpeed, _WindNoiseSpeed), 0.0, 0.0)).r * _WindNoiseStrength;
			 
			v.vertex.xyz += (mul(_Wind_Direction.xyz, unity_WorldToObject) * v.color.r * sin(v.color.b*3.141592654 + (_Time.y + noiseA) *_WindFrequency) *_WindAmplitude);
			o.pos = UnityObjectToClipPos(v.vertex);
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.texcoord);
			o.Albedo = c.rgb;

			o.Normal = UnpackNormal(tex2D(_NormalMap, IN.texcoord));
			o.Metallic = _Metallic;
			o.Smoothness = _Gloss;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
