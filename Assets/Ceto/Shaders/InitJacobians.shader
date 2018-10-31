// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Ceto/InitJacobians" 
{

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D Ceto_Spectrum01;
	sampler2D Ceto_Spectrum23;
	sampler2D Ceto_WTable;
	float4 Ceto_Offset;
	float4 Ceto_InverseGridSizes;
	float Ceto_FoamAmount;
	float Ceto_Time;

	struct v2f 
	{
		float4  pos : SV_POSITION;
		float2  uv : TEXCOORD0;
	};

	struct f2a2
	{
		float4 col0 : SV_Target0;
		float4 col1 : SV_Target1;
	};

	struct f2a3
	{
	 	float4 col0 : SV_Target0;
	 	float4 col1 : SV_Target1;
	 	float4 col2 : SV_Target2;
	};

	v2f vert(appdata_base v)
	{
		v2f OUT;
		OUT.pos = UnityObjectToClipPos(v.vertex);
		OUT.uv = v.texcoord;
		return OUT;
	}

	float2 GetSpectrum(float w, float2 s0, float2 s0c) 
	{
	    float c = cos(w*Ceto_Time);
	    float s = sin(w*Ceto_Time);
	    return float2((s0.x + s0c.x) * c - (s0.y + s0c.y) * s, (s0.x - s0c.x) * s + (s0.y - s0c.y) * c);
	}

	float2 COMPLEX(float2 z) 
	{
	    return float2(-z.y, z.x); // returns i times z (complex number)
	}
	
	float4 InitGrids1(v2f IN) : SV_Target
	{ 
		float2 uv = IN.uv.xy;

		float2 st;
		st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
		st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;
		
		float4 s12 =  tex2Dlod(Ceto_Spectrum01, float4(uv,0,0));
		float4 s12c = tex2Dlod(Ceto_Spectrum01, float4(Ceto_Offset.xy-uv,0,0));
		
		s12 *= Ceto_FoamAmount;
		s12c *= Ceto_FoamAmount;

		float2 k1 = st * Ceto_InverseGridSizes.x;

	    float K1 = length(k1);

	    float IK1 = K1 == 0.0 ? 0.0 : 1.0 / K1;

	    float4 w = tex2D(Ceto_WTable, uv);
	    
	    float2 h1 = GetSpectrum(w.x, s12.xy, s12c.xy);

		/// Jacobians
		float2 k1Squared = k1*k1;
		
		return float4(k1Squared.x, k1Squared.y, k1.x*k1.y, 0) * IK1 * -h1.x;

	}
	
	f2a2 InitGrids2(v2f IN)
	{
		float2 uv = IN.uv.xy;

		float2 st;
		st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
		st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

		float4 s12 = tex2Dlod(Ceto_Spectrum01, float4(uv,0,0));
		float4 s12c = tex2Dlod(Ceto_Spectrum01, float4(Ceto_Offset.xy - uv,0,0));

		s12 *= Ceto_FoamAmount;
		s12c *= Ceto_FoamAmount;

		float2 k1 = st * Ceto_InverseGridSizes.x;
		float2 k2 = st * Ceto_InverseGridSizes.y;
	
		float K1 = length(k1);
		float K2 = length(k2);
	
		float IK1 = K1 == 0.0 ? 0.0 : 1.0 / K1;
		float IK2 = K2 == 0.0 ? 0.0 : 1.0 / K2;

		float4 w = tex2D(Ceto_WTable, uv);

		float2 h1 = GetSpectrum(w.x, s12.xy, s12c.xy);
		float2 h2 = GetSpectrum(w.y, s12.zw, s12c.zw);

		/// Jacobians
		float4 IK = float4(IK1,IK2,0,0);
		float2 k1Squared = k1*k1;
		float2 k2Squared = k2*k2;

		float4 tmp = float4(h1.x, h2.x, 0, 0);

		f2a2 OUT;

		OUT.col0 = float4(k1Squared.x, k2Squared.x, k1Squared.y, k2Squared.y) * IK * -tmp.xyxy;
		OUT.col1 = float4(k1.x*k1.y, k2.x*k2.y, 0, 0) * IK * -tmp;

		return OUT;
	}
	
	f2a3 InitGrids3(v2f IN)
	{
		float2 uv = IN.uv.xy;

		float2 st;
		st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
		st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

		float4 s12 = tex2Dlod(Ceto_Spectrum01, float4(uv, 0, 0));
		float4 s12c = tex2Dlod(Ceto_Spectrum01, float4(Ceto_Offset.xy - uv, 0, 0));

		float4 s34 = tex2Dlod(Ceto_Spectrum23, float4(uv, 0, 0));
		float4 s34c = tex2Dlod(Ceto_Spectrum23, float4(Ceto_Offset.xy - uv, 0, 0));

		s12 *= Ceto_FoamAmount;
		s12c *= Ceto_FoamAmount;

		s34 *= Ceto_FoamAmount;
		s34c *= Ceto_FoamAmount;

		float2 k1 = st * Ceto_InverseGridSizes.x;
		float2 k2 = st * Ceto_InverseGridSizes.y;
		float2 k3 = st * Ceto_InverseGridSizes.z;

		float K1 = length(k1);
		float K2 = length(k2);
		float K3 = length(k3);
	
		float IK1 = K1 == 0.0 ? 0.0 : 1.0 / K1;
		float IK2 = K2 == 0.0 ? 0.0 : 1.0 / K2;
		float IK3 = K3 == 0.0 ? 0.0 : 1.0 / K3;

		float4 w = tex2D(Ceto_WTable, uv);

		float2 h1 = GetSpectrum(w.x, s12.xy, s12c.xy);
		float2 h2 = GetSpectrum(w.y, s12.zw, s12c.zw);
		float2 h3 = GetSpectrum(w.z, s34.xy, s34c.xy);

		/// Jacobians
		float4 IK = float4(IK1, IK2, IK3, 0);
		float2 k1Squared = k1*k1;
		float2 k2Squared = k2*k2;
		float2 k3Squared = k3*k3;

		float4 tmp = float4(h1.x, h2.x, h3.x, 0);

		f2a3 OUT;

		OUT.col0 = float4(k1Squared.x, k2Squared.x, k3Squared.x, 0) * IK * -tmp;
		OUT.col1 = float4(k1Squared.y, k2Squared.y, k3Squared.y, 0) * IK * -tmp;
		OUT.col2 = float4(k1.x*k1.y, k2.x*k2.y, k3.x*k3.y, 0) * IK * -tmp;

		return OUT;
	}

	f2a3 InitGrids4(v2f IN)
	{ 
		float2 uv = IN.uv.xy;

		float2 st;
		st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
		st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;
		
		float4 s12 =  tex2Dlod(Ceto_Spectrum01, float4(uv,0,0));
		float4 s12c = tex2Dlod(Ceto_Spectrum01, float4(Ceto_Offset.xy-uv,0,0));
		
		float4 s34 =  tex2Dlod(Ceto_Spectrum23, float4(uv,0,0));
		float4 s34c = tex2Dlod(Ceto_Spectrum23, float4(Ceto_Offset.xy-uv,0,0));
		
		s12 *= Ceto_FoamAmount;
		s12c *= Ceto_FoamAmount;
		
		s34 *= Ceto_FoamAmount;
		s34c *= Ceto_FoamAmount;
		
		float2 k1 = st * Ceto_InverseGridSizes.x;
	    float2 k2 = st * Ceto_InverseGridSizes.y;
	    float2 k3 = st * Ceto_InverseGridSizes.z;
	    float2 k4 = st * Ceto_InverseGridSizes.w;
	    
	    float K1 = length(k1);
	    float K2 = length(k2);
	    float K3 = length(k3);
	    float K4 = length(k4);

	    float IK1 = K1 == 0.0 ? 0.0 : 1.0 / K1;
	    float IK2 = K2 == 0.0 ? 0.0 : 1.0 / K2;
	    float IK3 = K3 == 0.0 ? 0.0 : 1.0 / K3;
	    float IK4 = K4 == 0.0 ? 0.0 : 1.0 / K4;
	    
	    float4 w = tex2D(Ceto_WTable, uv);
	    
	    float2 h1 = GetSpectrum(w.x, s12.xy, s12c.xy);
	    float2 h2 = GetSpectrum(w.y, s12.zw, s12c.zw);
	   	float2 h3 = GetSpectrum(w.z, s34.xy, s34c.xy);
	    float2 h4 = GetSpectrum(w.w, s34.zw, s34c.zw);
	    
		/// Jacobians
		float4 IK = float4(IK1,IK2,IK3,IK4);
		float2 k1Squared = k1*k1;
		float2 k2Squared = k2*k2;
		float2 k3Squared = k3*k3;
		float2 k4Squared = k4*k4;

		float4 tmp = float4(h1.x, h2.x, h3.x, h4.x);
		
		f2a3 OUT;

		OUT.col0 = float4(k1Squared.x, k2Squared.x, k3Squared.x, k4Squared.x) * IK * -tmp;
		OUT.col1 = float4(k1Squared.y, k2Squared.y, k3Squared.y, k4Squared.y) * IK * -tmp;
		OUT.col2 = float4(k1.x*k1.y, k2.x*k2.y, k3.x*k3.y, k4.x*k4.y) * IK * -tmp;

		return OUT;
	}
	
	ENDCG
			
	SubShader 
	{
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
      		Fog { Mode off }
    		
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment InitGrids1
			
			ENDCG
    	}
    	
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
      		Fog { Mode off }
    		
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment InitGrids2
			
			ENDCG
    	}
    	
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
      		Fog { Mode off }
    		
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment InitGrids3
			
			ENDCG
    	}

    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
      		Fog { Mode off }
    		
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment InitGrids4
			
			ENDCG

    	}
	}
	
}



