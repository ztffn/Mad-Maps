// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/EditorCellShader" {
Properties {
}

SubShader {
	Tags {"Queue"="Overlay" "IgnoreProjector"="True" }
	LOD 100
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off
	Offset 0, -9999999
	
	Pass {  
		CGPROGRAM
				#pragma target 5.0
				#pragma exclude_renderers glcore
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main
				#include "UnityCG.cginc" 

				// **************************************************************
				// Data structures												*
				// **************************************************************
				struct GS_INPUT
				{
					float4	pos		: POSITION;
					float3	normal	: NORMAL;
					float2  tex0	: TEXCOORD0;	
					half4 color : COLOR;				
				};

				struct FS_INPUT
				{
					float4	pos		: POSITION;
					float2  tex0	: TEXCOORD0;
					half4 color : COLOR;		
				};


				// **************************************************************
				// Vars															*
				// **************************************************************
				half _Size;

				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(appdata_full v)
				{
					GS_INPUT output = (GS_INPUT)0;

					output.pos =  v.vertex;
					output.normal = v.normal;
					output.tex0 = float2(0, 0);
					output.color = v.color;

					return output;
				}
				
				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(4)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					float3 up = float3(0, 0, -1);
					float3 right =  float3(1, 0, 0);
					
					float halfS = 0.5 * max(_Size, 1);
							
					float4 v[4];
					v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
					v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
					v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
					v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);
					
					FS_INPUT pIn;
					pIn.color = p[0].color;
					pIn.pos = UnityObjectToClipPos(v[0]);
					pIn.tex0 = float2(1.0f, 0.0f);
					triStream.Append(pIn);

					pIn.pos =  UnityObjectToClipPos(v[1]);
					pIn.tex0 = float2(1.0f, 1.0f);
					triStream.Append(pIn);

					pIn.pos =  UnityObjectToClipPos(v[2]);
					pIn.tex0 = float2(0.0f, 0.0f);
					triStream.Append(pIn);

					pIn.pos =  UnityObjectToClipPos(v[3]);
					pIn.tex0 = float2(0.0f, 1.0f);
					triStream.Append(pIn);					
				}
				
				// Fragment Shader -----------------------------------------------
				float4 FS_Main(FS_INPUT input) : COLOR
				{
					half threshold = 0.1;
					half2 wrapTex = abs((input.tex0 / 2) - 0.5);
					if(wrapTex.x < threshold || wrapTex.y < threshold)
						return saturate(1 - input.color);
					return saturate(input.color);
				}

			ENDCG
			
	}
	
}
Fallback "Hidden/EditorCellShaderSimple"
}
