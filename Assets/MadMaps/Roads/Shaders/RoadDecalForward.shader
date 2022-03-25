Shader "MadMaps/RoadDecalForward" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Spec ("Spec (RGB) Smooth (A)", 2D) = "black" {}
		[Bump] _BumpMap ("Normal map", 2D) = "bump" {}
	}
	SubShader {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
		LOD 200
		Offset -1, -1
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Spec;
		sampler2D _BumpMap;

		struct Input {
			float2 uv_MainTex;
		};

		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 s = tex2D (_Spec, IN.uv_MainTex);
			fixed3 n = UnpackNormal(tex2D (_BumpMap, IN.uv_MainTex));
			clip(c.a - 0.4);

			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = n;
			o.Specular = s.rgb;
			o.Smoothness = s.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
