// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Ceto/SlopeCopy" 
{

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D Ceto_SlopeBuffer;

	struct appdata_t
	{
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = v.texcoord.xy;
		return o;
	}

	float4 frag(v2f i) : SV_Target
	{
		return tex2D(Ceto_SlopeBuffer, i.texcoord);
	}

	ENDCG

	SubShader 
	{ 
		Pass 
		{
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			ENDCG 
		}
		
	}

	Fallback Off 
}
