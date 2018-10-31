// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Ceto/DisplacementCopy" 
{
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	sampler2D Ceto_HeightBuffer, Ceto_DisplacementBuffer;
	float4 Ceto_Choppyness;
	
	struct appdata_t 
	{
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};

	v2f vert (appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = v.texcoord.xy;
		return o;
	}

	float4 CopyGrid1(v2f i) : SV_Target
	{
		float4 d = float4(0,0,0,0);
		d.y = tex2D(Ceto_HeightBuffer, i.texcoord).x;
		d.xz = tex2D(Ceto_DisplacementBuffer, i.texcoord).xy;
	
		return d;
	}
	
	float4 CopyGrid2(v2f i) : SV_Target
	{
		float4 d = float4(0,0,0,0);
		d.y = tex2D(Ceto_HeightBuffer, i.texcoord).y;
		d.xz = tex2D(Ceto_DisplacementBuffer, i.texcoord).zw;
	
		return d;
	}
	
	float4 CopyGrid3(v2f i) : SV_Target
	{
		float4 d = float4(0,0,0,0);
		d.y = tex2D(Ceto_HeightBuffer, i.texcoord).z;
		d.xz = tex2D(Ceto_DisplacementBuffer, i.texcoord).xy;
	
		return d;
	}
	
	float4 CopyGrid4(v2f i) : SV_Target
	{
		float4 d = float4(0,0,0,0);
		d.y = tex2D(Ceto_HeightBuffer, i.texcoord).w;
		d.xz = tex2D(Ceto_DisplacementBuffer, i.texcoord).zw;
	
		return d;
	}

	float4 CopyGrid1Compact(v2f i) : SV_Target
	{
		//If only 1 grid is used the heights and dispacements 
		//are compacted into 1 texture.
		float4 d = float4(0,0,0,0);
		d.xyz = tex2D(Ceto_HeightBuffer, i.texcoord).zxw;

		return d;
	}
	
	ENDCG 
	
	SubShader 
	{ 

		Pass 
		{
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid1
			#pragma target 2.0
			ENDCG
		}
		
		Pass 
		{
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid2
			#pragma target 2.0
			ENDCG
		}
		
		Pass 
		{
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid3
			#pragma target 2.0
			ENDCG
		}
		
		Pass 
		{
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid4
			#pragma target 2.0
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid1Compact
			#pragma target 2.0
			ENDCG
		}
		
	}
	Fallback Off 
}














