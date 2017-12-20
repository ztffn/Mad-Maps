﻿Shader "sRoads/Road Decal" 
{
	Properties 
	{
		_MainTex ("Albedo (RGBA)", 2D) = "white" {}
		_Spec ("Spec (RGB)", 2D) = "black" {}
		_Smoothness ("Smooth", Float) = 0.01
		[Bump] _BumpMap ("Normal map", 2D) = "bump" {}
		_Color1 ("Color1", Color) = (1,1,1,1)
		_Color2 ("Color1", Color) = (1,1,1,1)
	}
	SubShader 
	{
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Opaque" "ForceNoShadowCasting"="True"}
		LOD 300
		Offset -1, -1
		//ZWrite Off
		Cull Back

		Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

		CGPROGRAM

		#pragma surface surf StandardSpecular finalgbuffer:DecalFinalGBuffer exclude_path:forward exclude_path:prepass noshadow noforwardadd keepalpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Spec;
		sampler2D _BumpMap;

		struct Input 
		{
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		fixed3 _ColorMain;
		half _Smoothness;
		float4 _Color1;
		float4 _Color2;

		inline half ConvertToGrayscale (half3 c)
		{
			return dot (c, half3 (0.3, 0.59, 0.11));
		}

		inline half GetColorLerp(float4 color)
		{
			return color.r;
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			fixed4 main = tex2D(_MainTex, IN.uv_MainTex);
			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			fixed4 specSmooth = tex2D(_Spec, IN.uv_MainTex);
					
			float4 color = lerp(_Color1, _Color2,  GetColorLerp(IN.color));

			o.Alpha = main.w * color.a;
			o.Albedo = main.rgb * color.rgb;
			o.Normal = normal;
			o.Specular = max(0.01, specSmooth.rgb);
			o.Smoothness = _Smoothness;
		}

		void DecalFinalGBuffer (Input IN, SurfaceOutputStandardSpecular o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			float4 color = lerp(_Color1, _Color2,  GetColorLerp(IN.color));
			float alphaMultiplier = color.a;

			diffuse.a = o.Alpha * alphaMultiplier;
			specSmoothness.a = o.Alpha * alphaMultiplier;
			normal.a = o.Alpha * alphaMultiplier; 
			emission.a = o.Alpha * alphaMultiplier;
		}

		ENDCG

		Blend One One
		ColorMask A
		//ZWrite Off
		Cull Back

		CGPROGRAM

		#pragma surface surf StandardSpecular finalgbuffer:DecalFinalGBuffer exclude_path:forward exclude_path:prepass noshadow noforwardadd keepalpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Spec;
		sampler2D _BumpMap;

		struct Input 
		{
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		half _SmoothnessLow;
		half _SmoothnessHigh;
		half _ContributionDiffuseSpecular;
		half _ContributionOcclusion;

		half _MaskAlphaUsingAO;
		half _MaskAlphaPower;
		half _MaskAlphaShift;

		float4 _Color1;
		float4 _Color2;

		inline half GetColorLerp(float4 color)
		{
			return color.r;
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			fixed4 main = tex2D(_MainTex, IN.uv_MainTex);
			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			fixed4 specSmooth = tex2D(_Spec, IN.uv_MainTex);
			float4 color = lerp(_Color1, _Color2, GetColorLerp(IN.color));

			o.Albedo = main.rgb * color.rgb;
			o.Alpha = main.a * color;
			o.Normal = normal;
			o.Specular = specSmooth.rgb;
			o.Smoothness = specSmooth.a;
		}

		void DecalFinalGBuffer (Input IN, SurfaceOutputStandardSpecular o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			float4 color = lerp(_Color1, _Color2, GetColorLerp(IN.color));
			float alphaMultiplier = color.a;
			specSmoothness.a *= o.Alpha * alphaMultiplier * _ContributionDiffuseSpecular;
		}

		ENDCG
	} 
	FallBack "sRoads/RoadDecalForward"
}