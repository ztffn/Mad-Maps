Shader "Hidden/EditorCellShaderSimple" {
Properties {
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	Cull Off
	Offset 0, -9999999	
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
                half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
                half4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
                o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half threshold = 0.01;
                half2 wrapTex = abs((i.texcoord / 2) - 0.5);
                if(wrapTex.x < threshold || wrapTex.y < threshold)
                    return saturate(1 - i.color);
                return saturate(i.color);
			}
		ENDCG
	}
}

}