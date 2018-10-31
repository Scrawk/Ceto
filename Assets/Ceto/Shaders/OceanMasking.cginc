#ifndef CETO_MASKING_INCLUDED
#define CETO_MASKING_INCLUDED

/*
* Sample texture taking stero eye into account (for VR)
* if enabled and supported.
*/
float4 SampleMaskTexture(float2 uv)
{

#if UNITY_VERSION >= 540 && defined(CETO_STERO_CAMERA)
	if (unity_StereoEyeIndex > 0)
		return tex2Dlod(Ceto_OceanMask1, float4(uv, 0, 0));
	else
		return tex2Dlod(Ceto_OceanMask0, float4(uv, 0, 0));
#else
	return tex2Dlod(Ceto_OceanMask0, float4(uv, 0, 0));
#endif

}

float IsUnderWater(float4 mask)
{

	float error = 0.01;
	float isUnderwater = 0.0;

	if (mask.x <= TOP_MASK + error)
		isUnderwater = 0.0;
	else
		isUnderwater = 1.0;
	
	return isUnderwater;
	
}

float IsOceanSurface(float4 mask)
{

	float isOceanSurface = 0.0;

	if (mask.x > EMPTY_MASK && mask.x < BOTTOM_MASK)
		isOceanSurface = 1.0;
	else
		isOceanSurface = 0.0;

	return isOceanSurface;

}


#endif