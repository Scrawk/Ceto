// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Ceto/NormalFade"
{
	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D _CameraDepthNormalsTexture;
	sampler2D _CameraGBufferTexture2; // World space normal (RGB), unused (A)

	float4x4 Ceto_NormalFade_MV;

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	float4 fragDepthNormal(v2f i) : SV_Target
	{

		float4 dn = tex2D(_CameraDepthNormalsTexture, i.uv);
		float3 n = DecodeViewNormalStereo(dn);
		float3 worldNormal = mul((float3x3)Ceto_NormalFade_MV, n);

		float fade = max(0, dot(float3(0, 1, 0), worldNormal));

		return float4(fade, 0, 0, 1);
	}

	float4 fragGBuffer2(v2f i) : SV_Target
	{

		float3 worldNormal = tex2D(_CameraGBufferTexture2, i.uv).xyz;

		worldNormal = (worldNormal - 0.5) * 2.0;

		float fade = max(0, dot(float3(0, 1, 0), worldNormal));

		return float4(fade, 0, 0, 1);
	}

	ENDCG

	SubShader
	{

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog{ Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragDepthNormal
			#pragma target 2.0

			#include "UnityCG.cginc"

			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog{ Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragGBuffer2
			#pragma target 2.0

			#include "UnityCG.cginc"

			ENDCG
		}
	}
}
