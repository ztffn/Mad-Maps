// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/WorldStampPreview" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
		LOD 200
		ZWrite Off
		Offset 0, -50000

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = 0;
			o.Smoothness = 0;
			o.Alpha = c.a * IN.color.a;
		}
		ENDCG
	}
	FallBack "Standard"
}
