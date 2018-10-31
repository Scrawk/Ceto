// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Ceto/UnderWaterPostEffect" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "black" {}
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma multi_compile __ CETO_UNDERWATER_ON
	#pragma multi_compile __ CETO_USE_OCEAN_DEPTHS_BUFFER
	#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS
	#pragma multi_compile __ CETO_STERO_CAMERA

	//#define CETO_UNDERWATER_ON
	//#define CETO_USE_OCEAN_DEPTHS_BUFFER
	//#define CETO_USE_4_SPECTRUM_GRIDS

	//#define CETO_DISABLE_CAUSTICS

	#include "./OceanShaderHeader.cginc"
	#include "./OceanDisplacement.cginc"
	#include "./OceanUnderWater.cginc"
	#include "./OceanMasking.cginc"
	
	sampler2D  _MainTex, _BelowTex;
	float4x4 _FrustumCorners;
	float4 _MainTex_TexelSize;
	float3 _MultiplyCol;

	sampler2D_float _CameraDepthTexture;
	
	struct v2f 
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv_depth : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
	};
	
	v2f vertApplyEffect( appdata_img v )
	{
		v2f o;
		half index = v.vertex.z;
		v.vertex.z = 0.1;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif				
		
		o.interpolatedRay = _FrustumCorners[(int)index];
		o.interpolatedRay.w = index;
		
		return o;
	}		
	
	v2f vertApplyMask( appdata_img v )
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;
		o.interpolatedRay = 0;
		
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif	

		return o;
	}
	
	float4 fragApplyEffect(v2f IN) : SV_Target 
	{
	
		float depth = 0;
	
		#ifdef CETO_USE_OCEAN_DEPTHS_BUFFER
			depth = SampleOceanDepthTexture(IN.uv_depth).y;
		#else
			depth = Linear01Depth(tex2D(Ceto_DepthBuffer, IN.uv_depth));
			//depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, IN.uv_depth));
		#endif

	    float4 mask = SampleMaskTexture(IN.uv_depth);

		//If the ocean is not in the depth buffer you can 
		//add its depth to the buffer like this.
		//The mask also stores the ocean depth value
		//and is return in the y value.
		float oceanDepth = mask.y;
		depth = min(depth, oceanDepth);
		
		float3 worldPos = (_WorldSpaceCameraPos + depth * IN.interpolatedRay);
		
		float dist = length(worldPos - _WorldSpaceCameraPos);
		
		fixed3 col = tex2D(_MainTex, IN.uv).rgb;

		#ifndef CETO_DISABLE_CAUSTICS
		#ifndef CETO_USE_OCEAN_DEPTHS_BUFFER
		if (depth < oceanDepth)
		{
			half2 slope1, slope2, slope3;
			SampleSlope(worldPos.xz, slope1, slope2, slope3);
			half3 normal = normalize(SlopeToWorldNormal(slope2));

			col += CausticsFromBelow(IN.uv_depth, normal, worldPos, dist);
		}
		#endif
		#endif

		col = UnderWaterColor(col, dist);
		
		col *= _MultiplyCol;

		return float4(col, 1);
	}
	
	float4 fragApplyMask(v2f IN) : SV_Target 
	{

		float4 mask = SampleMaskTexture(IN.uv);

		float a = IsUnderWater(mask);

		fixed3 aboveColor = tex2D(_MainTex, IN.uv_depth).rgb;
		fixed3 belowColor = tex2D(_BelowTex, IN.uv).rgb;
		
		fixed3 col = lerp(aboveColor, belowColor, a);

		return float4(col, 1);
	}

	ENDCG
			
	Subshader 
	{
  
	 	Pass 
	 	{
			ZTest Always Cull Off ZWrite Off
			
			CGPROGRAM
			#pragma vertex vertApplyEffect
			#pragma fragment fragApplyEffect
			#pragma target 3.0
			
			ENDCG
		}
		
		Pass 
	 	{
			ZTest Always Cull Off ZWrite Off
			
			CGPROGRAM
			#pragma vertex vertApplyMask
			#pragma fragment fragApplyMask
			#pragma target 3.0
			
			ENDCG
		}
  	}

	Fallback off
	
} 






