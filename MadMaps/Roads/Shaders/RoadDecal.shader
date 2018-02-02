﻿// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "MadMaps/Road Decal" 
{
	Properties 
	{
		_MainTex ("Albedo (RGBA)", 2D) = "white" {}
		_Spec ("Spec (RGB)", 2D) = "black" {}
		_Smoothness ("Smooth", Float) = 0.01
		[Bump] _BumpMap ("Normal map", 2D) = "bump" {}
		_Color1 ("Color1", Color) = (1,1,1,1)
		_Color2 ("Color1", Color) = (1,1,1,1)

		_DropAmount("Drop Amount", Float) = 0

		_DropMin("Drop Min", Float) = 0
		_DropDistance("Drop Distance", Float) = 0
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

		#pragma surface surf StandardSpecular finalgbuffer:DecalFinalGBuffer vertex:vert exclude_path:forward exclude_path:prepass noshadow noforwardadd keepalpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Spec;
		sampler2D _BumpMap;
		
		struct Input 
		{
			float2 uv_MainTex;
			float4 color : COLOR;
			float4 auxData;
		};

		fixed3 _ColorMain;
		half _Smoothness;

		float _DropAmount;
		float _DropMin;
		float _DropDistance;

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

		void vert (inout appdata_full v, out Input o)
        {	
			UNITY_INITIALIZE_OUTPUT(Input, o);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
			float dist = length(worldPos - _WorldSpaceCameraPos) - _DropMin;
			float multiplier = 1-saturate( dist / _DropDistance);

			o.auxData.rgb = worldPos.xyz;
			o.auxData.a = multiplier;

			v.vertex.y += multiplier * _DropAmount;
        }
		
		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			fixed4 main = tex2D(_MainTex, IN.uv_MainTex);
			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			fixed4 specSmooth = tex2D(_Spec, IN.uv_MainTex);					
			float4 color = lerp(_Color1, _Color2,  GetColorLerp(IN.color));
			
			o.Alpha = main.w;
			o.Albedo = main.rgb * color.rgb;		
			o.Normal = normal;
			o.Specular = max(0.01, specSmooth.rgb);
			o.Smoothness = _Smoothness;

			//o.Alpha = IN.auxData.a;
			//o.Albedo = fixed3(frac(IN.auxData.xz), 1);
			//o.Albedo = IN.auxData.a;
		}

		void DecalFinalGBuffer (Input IN, SurfaceOutputStandardSpecular o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			//float4 color = lerp(_Color1, _Color2,  GetColorLerp(IN.color));
			//float alphaMultiplier = color.a;
			float alphaMultiplier = 1;

			diffuse.a = o.Alpha * alphaMultiplier;
			specSmoothness.a = o.Alpha * alphaMultiplier;
			normal.a = o.Alpha * alphaMultiplier; 
			emission.a = o.Alpha * alphaMultiplier;
		}

		ENDCG
	} 
	FallBack "MadMaps/RoadDecalForward"
}	