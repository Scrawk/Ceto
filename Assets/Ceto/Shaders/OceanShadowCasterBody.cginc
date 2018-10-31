#ifndef CETO_SHADOWCASTER_BODY_INCLUDED
#define CETO_SHADOWCASTER_BODY_INCLUDED

struct v2f
{
	V2F_SHADOW_CASTER;
	float3 worldPos : TEXCOORD1;
};

v2f OceanVertShadow(appdata_base v)
{
	v2f o;

	float4 uv = float4(v.vertex.xy, v.texcoord.xy);

	float4 oceanPos;
	float3 displacement;
	OceanPositionAndDisplacement(uv, oceanPos, displacement);

	v.vertex.xyz = oceanPos.xyz + displacement;
	o.worldPos = v.vertex.xyz;

	TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
		return o;
}

float4 OceanFragShadow(v2f i) : SV_Target
{

	half4 st = WorldPosToProjectorSpace(i.worldPos);
	OceanClip(st, i.worldPos);

	SHADOW_CASTER_FRAGMENT(i)
}

#endif