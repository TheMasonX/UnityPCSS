Shader "Custom/DoubleSidedFoliage_PBR" {
	Properties {
	    _Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		_Specular ("Specular", 2D) = "white" {}
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
		_Cutoff ("Alpha Cutoff", float) = 0.5
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}
	 
	SubShader
	{
		Tags
		{
			"Queue" = "Geometry+200"
			"IgnoreProjector"="True"
			"RenderType"="Grass"
		}
		Cull Off
		LOD 200
		ColorMask RGB
		 
		CGPROGRAM
		#pragma surface surf StandardSpecular vertex:WavingGrassVert addshadow fullforwardshadows alphatest:_Cutoff2
		#include "TerrainEngine.cginc"
		 
		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _Specular;
		float _Shininess;
		fixed _Cutoff;
		 
		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			fixed4 color : COLOR;
		};
		 
		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
			fixed4 d = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = d.a;
			clip (o.Alpha - _Cutoff);
			o.Smoothness = _Shininess;
			o.Specular = tex2D(_Specular, IN.uv_MainTex) * _SpecColor;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		}
		ENDCG
	}
 
	SubShader {
		Tags {
			"Queue" = "Geometry+200"
			"IgnoreProjector"="True"
			"RenderType"="Grass"
		}
		Cull Off
		LOD 200
		ColorMask RGB
		 
		Pass {
			Tags { "LightMode" = "Vertex" }
			Material {
				Diffuse (1,1,1,1)
				Ambient (1,1,1,1)
                Specular [_SpecColor]
			}
			Lighting On
			ColorMaterial AmbientAndDiffuse
			AlphaTest Greater [_Cutoff]
			SetTexture [_MainTex] { combine texture * primary DOUBLE, texture }
		}
		Pass {
			Tags { "LightMode" = "VertexLMRGBM" }
			AlphaTest Greater [_Cutoff]
			BindChannels {
				Bind "Vertex", vertex
				Bind "texcoord1", texcoord0 // lightmap uses 2nd uv
				Bind "texcoord", texcoord1 // main uses 1st uv
			}
			SetTexture [unity_Lightmap] {
				matrix [unity_LightmapMatrix]
				combine texture * texture alpha DOUBLE
			}
			SetTexture [_MainTex] { combine texture * previous QUAD, texture }
		}
	}
	 
	Fallback Off
}