// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Ceto/OceanDepth" 
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		Pass
		{
			Fog { Mode Off }
			Cull Off
			Lighting off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"

			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"

			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
			};

			v2f vert (appdata_base v) 
			{
			    v2f o;

				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
			   
				COMPUTE_OCEAN_DEPTH_PARAMETERS

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="TransparentCutout" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;
			
			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
			    float2 uv : TEXCOORD1;
			};

			v2f vert (appdata_base v) 
			{
			    v2f o;

				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS
				
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				clip(tex2D(_MainTex, i.uv).a-_Cutoff);
			
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="TreeBark" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "UnityBuiltin3xTreeLibrary.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"
			
			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
			};

			v2f vert (appdata_full v) 
			{
			    v2f o;
			    TreeVertBark(v);
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="TreeLeaf" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "UnityBuiltin3xTreeLibrary.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;
			
			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			v2f vert (appdata_full v) 
			{
			    v2f o;
			    TreeVertLeaf(v);
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS

				o.uv = v.texcoord.xy;

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{

				clip(tex2D(_MainTex, i.uv).a - _Cutoff);

			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="TreeOpaque" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"
			
			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
			};
			
			struct appdata {
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			    fixed4 color : COLOR;
			};

			v2f vert (appdata v) 
			{
			    v2f o;
			    TerrainAnimateTree(v.vertex, v.color.w);
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="TreeTransparentCutout" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;

			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
			    float2 uv : TEXCOORD1;
			};
			
			struct appdata {
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			    fixed4 color : COLOR;
			    float4 texcoord : TEXCOORD0;
			};

			v2f vert (appdata v) 
			{
			    v2f o;
			    TerrainAnimateTree(v.vertex, v.color.w);
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS
				
				o.uv = v.texcoord.xy;

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				clip(tex2D(_MainTex, i.uv).a-_Cutoff);
			
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="TreeBillboard" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;

			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 depth : TEXCOORD0;
			    float2 uv : TEXCOORD1;
			};

			v2f vert (appdata_tree_billboard v) 
			{
			    v2f o;
			    TerrainBillboardTree(v.vertex, v.texcoord1.xy, v.texcoord.y);
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS
				
				o.uv.x = v.texcoord.x;
				o.uv.y = v.texcoord.y > 0;

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				clip(tex2D(_MainTex, i.uv).a - 0.001);
			
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="Grass" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;

			struct v2f {
			    float4 pos : SV_POSITION;
			    fixed4 color : COLOR;
			    float4 depth : TEXCOORD0;
			    float2 uv : TEXCOORD1;
			};
			
			v2f vert (appdata_full v) 
			{
			
			    v2f o;
			    WavingGrassVert (v);
			    o.color = v.color;
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS
				
				o.uv = v.texcoord.xy;

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				clip(tex2D(_MainTex, i.uv).a * i.color.a - _Cutoff);
			
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	SubShader 
	{
	    Tags { "RenderType"="GrassBillboard" }
	    Pass 
	    {
	        Fog { Mode Off }
			Cull Off
			Lighting off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanUnderWater.cginc"
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;

			struct v2f {
			    float4 pos : SV_POSITION;
			    fixed4 color : COLOR;
			    float4 depth : TEXCOORD0;
			    float2 uv : TEXCOORD1;
			};
			
			v2f vert (appdata_full v) 
			{
			
			    v2f o;
			    WavingGrassBillboardVert (v);
			    o.color = v.color;
			    
				float3 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				
				COMPUTE_OCEAN_DEPTH_PARAMETERS
				
				o.uv = v.texcoord.xy;

			    return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				clip(tex2D(_MainTex, i.uv).a * i.color.a -_Cutoff);
			
			    return float4(i.depth);
			}
			ENDCG
	    }
	}
	
	
}


