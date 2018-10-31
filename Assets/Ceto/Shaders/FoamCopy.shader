// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Ceto/FoamCopy" 
{
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	sampler2D Ceto_JacobianBuffer0, Ceto_JacobianBuffer1, Ceto_JacobianBuffer2, Ceto_HeightBuffer;
	float4 Ceto_FoamChoppyness;
	float Ceto_FoamCoverage;

	struct v2f 
	{
		float4  pos : SV_POSITION;
		float2  uv : TEXCOORD0;
	};

	v2f vert(appdata_base v)
	{
		v2f OUT;
		OUT.pos = UnityObjectToClipPos(v.vertex);
		OUT.uv = v.texcoord;
		return OUT;
	}
	
	float4 CopyGrid1(v2f IN) : SV_Target
	{ 
	
		float choppyness = Ceto_FoamChoppyness.x;

		float4 J = tex2D(Ceto_JacobianBuffer0, IN.uv);

		float Jxx = choppyness*J.x;
		float Jyy = choppyness*J.y;
		float Jxy = choppyness*choppyness*J.z;

		float j = Ceto_FoamCoverage + Jxx + Jyy + choppyness*Jxx*Jyy - Jxy*Jxy;
		
		return float4(j, 0, 0, 0);
	}

	float4 CopyGrid2(v2f IN) : SV_Target
	{

		float2 choppyness = Ceto_FoamChoppyness.xy;

		float4 J = tex2D(Ceto_JacobianBuffer0, IN.uv);

		float2 Jxx = choppyness*J.xy;
		float2 Jyy = choppyness*J.zw;

		float2 Jxy = choppyness*choppyness*tex2D(Ceto_JacobianBuffer1, IN.uv).xy;

		float2 j = Ceto_FoamCoverage.xx + Jxx + Jyy + choppyness*Jxx*Jyy - Jxy*Jxy;

		return float4(j, 0, 0);
	}

	float4 CopyGrid3(v2f IN) : SV_Target
	{

		float3 choppyness = Ceto_FoamChoppyness.xyz;

		float3 Jxx = choppyness*tex2D(Ceto_JacobianBuffer0, IN.uv).xyz;
		float3 Jyy = choppyness*tex2D(Ceto_JacobianBuffer1, IN.uv).xyz;
		float3 Jxy = choppyness*choppyness*tex2D(Ceto_JacobianBuffer2, IN.uv).xyz;

		float3 j = Ceto_FoamCoverage.xxx + Jxx + Jyy + choppyness*Jxx*Jyy - Jxy*Jxy;

		return float4(j, 0);
	}

	float4 CopyGrid4(v2f IN) : SV_Target
	{

		float4 choppyness = Ceto_FoamChoppyness;

		float4 Jxx = choppyness*tex2D(Ceto_JacobianBuffer0, IN.uv);
		float4 Jyy = choppyness*tex2D(Ceto_JacobianBuffer1, IN.uv);
		float4 Jxy = choppyness*choppyness*tex2D(Ceto_JacobianBuffer2, IN.uv);

		float4 j = Ceto_FoamCoverage.xxxx + Jxx + Jyy + choppyness*Jxx*Jyy - Jxy*Jxy;

		return j;
	}
	
	ENDCG
	
	SubShader 
	{
    	
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
      		Fog { Mode off }
    		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid1
			#pragma target 2.0
			
			ENDCG
    	}

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog{ Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid2
			#pragma target 2.0

			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog{ Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid3
			#pragma target 2.0

			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog{ Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment CopyGrid4
			#pragma target 2.0

			ENDCG
		}
    	
	}
}









