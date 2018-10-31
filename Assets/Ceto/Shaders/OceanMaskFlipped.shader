// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



Shader "Ceto/OceanMaskFlipped" 
{

	SubShader 
	{
		Tags { "OceanMask"="Ceto_ProjectedGrid_Top" "Queue"="Geometry+1"}
		Pass 
		{
		
			zwrite on
			Fog { Mode Off }
			Lighting off
			
			cull front //Flipped

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex OceanVertMask
			#pragma fragment OceanFragMask

			#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS
			//#define CETO_USE_4_SPECTRUM_GRIDS
			
			#define CETO_OCEAN_TOPSIDE

			#include "./OceanShaderHeader.cginc"
			#include "./OceanDisplacement.cginc"
			#include "./OceanMasking.cginc"
			#include "./OceanMaskBody.cginc"		
			
			ENDCG
		}

	}

	SubShader 
	{
		Tags { "OceanMask"="Ceto_ProjectedGrid_Under" "Queue"="Geometry+2"}
		Pass 
		{
		
			zwrite on
			Fog { Mode Off }
			Lighting off
			
			cull back //Flipped

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex OceanVertMask
			#pragma fragment OceanFragMask

			#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS
			//#define CETO_USE_4_SPECTRUM_GRIDS
			
			#define CETO_OCEAN_UNDERSIDE

			#include "./OceanShaderHeader.cginc"
			#include "./OceanDisplacement.cginc"
			#include "./OceanMasking.cginc"
			#include "./OceanMaskBody.cginc"		
			
			ENDCG
		}
	}
	
	SubShader 
	{
		
		Tags { "OceanMask"="Ceto_Ocean_Bottom" "Queue"="Background"}
		Pass 
		{
			zwrite off
			Fog { Mode Off }
			Lighting off

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanMasking.cginc"

			struct v2f 
			{
				float4  pos : SV_POSITION;
				float depth : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;

				float4 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);

				o.depth = COMPUTE_DEPTH_01;
				
				return o;
		 	} 
			
			float4 frag(v2f IN) : SV_Target
			{
			    return float4(BOTTOM_MASK, IN.depth, 0, 0);
			}	
			
			ENDCG
		}
	}

}











