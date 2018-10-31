// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#ifndef CETO_MASK_BODY_INCLUDED
#define CETO_MASK_BODY_INCLUDED

struct v2f
{
	float4  pos : SV_POSITION;
	float3 worldPos : TEXCOORD0;
	float depth : TEXCOORD1;
};

v2f OceanVertMask(appdata_base v)
{

	float4 uv = float4(v.vertex.xy, v.texcoord.xy);

	float4 oceanPos;
	half3 displacement;
	OceanPositionAndDisplacement(uv, oceanPos, displacement);

	v.vertex.xyz = oceanPos.xyz + displacement;

	v2f o;

	o.pos = UnityObjectToClipPos(v.vertex);
	o.worldPos = v.vertex.xyz;
	o.depth = COMPUTE_DEPTH_01;

	return o;
}

float4 OceanFragMask(v2f IN) : SV_Target
{
#ifdef CETO_OCEAN_TOPSIDE
	return float4(TOP_MASK, IN.depth, 0, 0);
#else
	return float4(UNDER_MASK, IN.depth, 0, 0);
#endif

}

#endif