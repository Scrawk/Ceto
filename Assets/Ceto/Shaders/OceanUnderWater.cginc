#ifndef CETO_UNDERWATER_INCLUDED
#define CETO_UNDERWATER_INCLUDED


/*
* Applies the foam to the ocean color.
* Spectrum foam in x, overlay foam in y.
*/
fixed FoamAmount(float3 worldPos, fixed4 foam)
{

	//foam.x == the wave (spectrum) foam.
	//foam.y == the overlay foam with foam texture.
	//foam.z == the overlay foam with no foam texture.

	fixed foamTexture = 0.0;

	#ifndef CETO_DISABLE_FOAM_TEXTURE
   		foamTexture += tex2D(Ceto_FoamTexture0, (worldPos.xz + Ceto_FoamTextureScale0.z) * Ceto_FoamTextureScale0.xy).a * 0.5;
		foamTexture += tex2D(Ceto_FoamTexture1, (worldPos.xz + Ceto_FoamTextureScale1.z) * Ceto_FoamTextureScale1.xy).a * 0.5;
	#else
		foamTexture = 1.0;
	#endif

	//Apply texture to the wave foam if that option is enabled.
    foam.x = lerp(foam.x, foam.x * foamTexture, Ceto_TextureWaveFoam);
	//Apply texture to overlay foam
   	foam.y = foam.y * foamTexture;
   
   	return saturate(max(max(foam.x, foam.y), foam.z));

}

/*
* Applies the foam to the ocean color.
* Spectrum foam in x, overlay foam in y.
*/
fixed3 AddFoamColor(fixed foamAmount, fixed3 oceanCol)
{

	//apply the absorption coefficient to the foam based on the foam strength.
	//This will fade the foam add make it look like it has some depth and
	//since it uses the abs cof the color should match the water.
	fixed3 foamCol = Ceto_FoamTint * foamAmount * exp(-Ceto_AbsCof.rgb * (1.0 - foamAmount) * 1.0);

	//TODO - find better way than lerp to blend.
	return lerp(oceanCol, foamCol, foamAmount);
}

/*
* Calculate a subsurface scatter color based on the view, normal and sun dir.
* NOTE - you have to add your directional light onto the ocean component for
* the sun direction to be used. A default sun dir of up is used otherwise.
*/
fixed3 SubSurfaceScatter(fixed3 V, fixed3 N, float surfaceDepth)
{

	fixed3 col = fixed3(0,0,0);

	#ifdef CETO_UNDERWATER_ON

		//The strength based on the view and up direction.
		fixed VU = 1.0 -  max(0.0, dot(V, fixed3(0,1,0)));
		VU *= VU;
		
		//The strength based on the view and sun direction.
		fixed VS = max(0, dot(reflect(V, fixed3(0,1,0)) * -1.0, Ceto_SunDir));
		VS *= VS;
		VS *= VS;
		
		float NX =  abs(dot(N, fixed3(1,0,0)));
		
		fixed s = NX * VU * VS;

		//If sun below horizion remove sss.
		if (dot(Ceto_SunDir, float3(0, 1, 0)) < 0.0) s = 0.0;
		
		//apply a non linear fade to distance.
		fixed d = max(0.2, exp(-max(0.0, surfaceDepth)));

		//Apply the absorption coefficient base on the distance and tint final color.
		col = Ceto_SSSTint * exp(-Ceto_SSSCof.rgb * d * Ceto_SSSCof.a) * s;
		
	#endif
	
	return col;

}

/*
* Get IVP matrix.
*/
float4x4 GetIVPMatrix()
{
	return Ceto_Camera_IVP0;
/*
#if UNITY_VERSION >= 540 && defined(CETO_STERO_CAMERA)
	if (unity_StereoEyeIndex > 0)
		return Ceto_Camera_IVP1;
	else
		return Ceto_Camera_IVP0;
#else
	return Ceto_Camera_IVP0;
#endif
*/
}

/*
* Calculates the world position from the depth buffer value.
*/
float3 WorldPosFromDepth(float2 uv, float depth)
{

	#if defined(UNITY_REVERSED_Z)
		depth = 1.0 - depth;
	#endif

	float4 ndc = float4(uv.x * 2.0 - 1.0, uv.y * 2.0 - 1.0, depth * 2.0 - 1.0, 1);
	
	float4 worldPos = mul(GetIVPMatrix(), ndc);
	worldPos /= worldPos.w;

	return worldPos.xyz;
}

/*
* The world position of the first object below the water surface
* reconstructed from the depth buffer.
*/
float3 WorldDepthPos(float2 screenUV)
{

	float3 worldPos = float3(0, 0, 0);

	#ifdef CETO_UNDERWATER_ON
	#ifndef CETO_USE_OCEAN_DEPTHS_BUFFER

	float db = tex2D(Ceto_DepthBuffer, screenUV).x;
	worldPos = WorldPosFromDepth(screenUV, db);

	#endif
	#endif

	return worldPos;

}

/*
* Samples the depth buffer with a distortion to the uv.
*/
float SampleDepthBuffer(float2 screenUV)
{

	//float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV).x;	
	float depth = tex2D(Ceto_DepthBuffer, screenUV).x;

	return Linear01Depth(depth);
}

/*
* Returns the depth info needed to apply the underwater effect 
* calculated from the depth buffer. If a object does not write 
* into the depth buffer it will not show up.
*/
float4 SampleOceanDepthFromDepthBuffer(float2 screenUV)
{

	//float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV).x
	float depth = tex2D(Ceto_DepthBuffer, screenUV).x;

	float3 worldPos = WorldPosFromDepth(screenUV, depth);

	float4 oceanDepth = float4(0,0,0,0);

	float ld = Linear01Depth(depth);
	
	oceanDepth.x = (worldPos.y-Ceto_OceanLevel) * -1.0;
	oceanDepth.y = ld * _ProjectionParams.z / Ceto_MaxDepthDist;
	oceanDepth.z = ld;
	oceanDepth.w = 0;
	
	return oceanDepth;
}

/*
* Sample texture taking stero eye into account (for VR)
* if enabled and supported.
*/
float4 SampleOceanDepthTexture(float2 uv)
{

#if UNITY_VERSION >= 540 && defined(CETO_STERO_CAMERA)
	if (unity_StereoEyeIndex > 0)
		return tex2D(Ceto_OceanDepth1, uv);
	else
		return tex2D(Ceto_OceanDepth0, uv);
#else
	return tex2D(Ceto_OceanDepth0, uv);
#endif

}

/*
* Returns the depth info needed to apply the underwater effect 
* from the ocean depths buffer. This is done by rendering the objects 
* in the ocean depth layer using a replacement shader. This is needed
* when the ocean is in the opaque queue. You can not get the info from 
* the depth buffer as Unity does a depth only pass the ocean mesh will
* have overridden the objects depth values that are below the mesh.
* If a object does not have its render type in the OceanDepths shader or
* its layer is not selected it will not show up.
*/
float4 SampleOceanDepth(float2 screenUV)
{

	float4 oceanDepth = SampleOceanDepthTexture(screenUV);

	float ld = oceanDepth.y;

	//unnormalize.
	oceanDepth.x *= Ceto_MaxDepthDist;
	oceanDepth.y = ld * _ProjectionParams.z / Ceto_MaxDepthDist;
	oceanDepth.z = ld;
	oceanDepth.w = 0;

	return oceanDepth;

}

/*
* This is the ocean depth info that is written using the ocean
* depth replacement shader. X is relative to ocean level to conserve
* precision in the half format used for buffer.
* x = world y pos relative to ocean level normalized to ocean depth and up flipped.
* y = linear depth value.
*/
#define COMPUTE_OCEAN_DEPTH_PARAMETERS \
	o.depth = float4(0,0,0,0);\
	o.depth.x = (worldPos.y-Ceto_OceanLevel) * -1.0 / Ceto_MaxDepthDist;\
	o.depth.y = COMPUTE_DEPTH_01;\

/*
* Computes the depth value used to apply the underwater effect.
*/
float2 OceanDepth(float2 screenUV, float3 worldPos, float depth)
{

	float2 surfaceDepth;
	surfaceDepth.x = (worldPos.y-Ceto_OceanLevel) * -1.0;
	surfaceDepth.y = depth * _ProjectionParams.z / Ceto_MaxDepthDist;
	
	#ifdef CETO_USE_OCEAN_DEPTHS_BUFFER
		float2 oceanDepth = SampleOceanDepth(screenUV).xy;
	#else
		float2 oceanDepth = SampleOceanDepthFromDepthBuffer(screenUV).xy;
	#endif

	oceanDepth.x = max(0.0, oceanDepth.x - surfaceDepth.x) / Ceto_MaxDepthDist;
	oceanDepth.y = max(0.0, oceanDepth.y - surfaceDepth.y);
	
	return oceanDepth;
	
}

/*
* Distorts the screen uv by the wave normal. 
*/
float4 DisortScreenUV(half3 normal, float4 screenUV, float surfaceDepth, float dist, half3 view)
{

	//Fade by distance so distortion is less on far away objects.
	float distortionFade = 1.0 - clamp(dist * 0.01, 0.0001, 1.0);
	float3 distortion = normal * Ceto_RefractionDistortion * distortionFade * distortionFade;

	distortion.z *= dot(view, normal);
	float4 distortedUV = saturate(screenUV + distortion.xzxz);

#ifdef CETO_USE_OCEAN_DEPTHS_BUFFER
	float depth = SampleOceanDepth(distortedUV.xy).z;
#else
	float depth = SampleDepthBuffer(distortedUV.xy);
#endif

	//If the distorted depth is less than the ocean mesh depth
	//then the distorted uv is in front of a object. The distortion
	//cant be applied in this case as the color from the grab texture
	//will be from a object that is not under the water.
	if (depth <= surfaceDepth) distortedUV = screenUV;

	//The smaller the depth difference the smaller the distortion
	float distortionMultiplier = saturate((depth - surfaceDepth) * _ProjectionParams.z * 0.25);
	distortedUV = lerp(screenUV, distortedUV, distortionMultiplier);

	return distortedUV;
}

/*
* The refraction color when see from above the ocean mesh.
* Where depth is normalized between 0-1 based on Ceto_MaxDepthDist.
*/
fixed3 AboveRefractionColor(float2 grabUV, float3 surfacePos, float depth, fixed3 caustics)
{

	fixed3 grab = tex2D(Ceto_RefractionGrab, grabUV).rgb * Ceto_AboveRefractionIntensity;

	grab += caustics;
	
	fixed3 col = grab * Ceto_AbsTint * exp(-Ceto_AbsCof.rgb * depth * Ceto_MaxDepthDist * Ceto_AbsCof.a);
	
	return col;
}

/*
* The refraction color when see from below the ocean mesh (under water).
*/
fixed3 BelowRefractionColor(float2 grabUV)
{

	fixed3 grab = tex2D(Ceto_RefractionGrab, grabUV).rgb * Ceto_BelowRefractionIntensity;
	
	return grab;
}

/*
* The inscatter when seen from above the ocean mesh.
* Where depth is normalized between 0-1 based on Ceto_MaxDepthDist.
*/
fixed3 AddAboveInscatter(fixed3 col, float depth)
{

	//There are 3 methods used to apply the inscatter.
	half3 inscatterScale;
	inscatterScale.x = saturate(depth * Ceto_AboveInscatterScale);
	inscatterScale.y = saturate(1.0-exp(-depth * Ceto_AboveInscatterScale));
	inscatterScale.z = saturate(1.0-exp(-depth * depth * Ceto_AboveInscatterScale));
	
	//Apply mask to pick which methods result to use.
	half a = dot(inscatterScale, Ceto_AboveInscatterMode);
	
	return lerp(col, Ceto_AboveInscatterColor.rgb, a * Ceto_AboveInscatterColor.a);
}

/*
* The inscatter when seen from below the ocean mesh.
* Where depth is normalized between 0-1 based on Ceto_MaxDepthDist.
*/
fixed3 AddBelowInscatter(fixed3 col, float depth)
{
	//There are 3 methods used to apply the inscatter.
	half3 inscatterScale;
	inscatterScale.x = saturate(depth * Ceto_BelowInscatterScale);
	inscatterScale.y = saturate(1.0-exp(-depth * Ceto_BelowInscatterScale));
	inscatterScale.z = saturate(1.0-exp(-depth * depth * Ceto_BelowInscatterScale));
	
	//Apply mask to pick which methods result to use.
	half a = dot(inscatterScale, Ceto_BelowInscatterMode);
	
	return lerp(col, Ceto_BelowInscatterColor.rgb, a * Ceto_BelowInscatterColor.a);
}

/*
* The ocean color when seen from above the ocean mesh.
*/
fixed3 OceanColorFromAbove(float4 distortedUV, float3 surfacePos, float surfaceDepth, fixed3 caustics)
{

	fixed3 col = Ceto_DefaultOceanColor;

	#ifdef CETO_UNDERWATER_ON
		
		float2 oceanDepth = OceanDepth(distortedUV.xy, surfacePos, surfaceDepth);

		float depthBlend = lerp(oceanDepth.x, oceanDepth.y, Ceto_DepthBlend);
		
		fixed3 refraction = AboveRefractionColor(distortedUV.zw, surfacePos, depthBlend, caustics);
		
		col = AddAboveInscatter(refraction, depthBlend);

	#endif
	
	return col;
	
}

/*
* This is the color of the underside of the mesh.
*/
fixed3 DefaultUnderSideColor()
{
	return Ceto_BelowInscatterColor.rgb;
}

/*
* The sky color when seen from below the ocean mesh.
*/
fixed3 SkyColorFromBelow(float4 distortedUV)
{

	fixed3 col = Ceto_DefaultOceanColor;

	#ifdef CETO_UNDERWATER_ON
		
		col = BelowRefractionColor(distortedUV.zw);
		
	#endif
	
	return col;
	
}

/*
* Returns a blend value to use as the alpha to fade ocean into shoreline.
*/
float EdgeFade(float2 screenUV, float3 view, float3 surfacePos, float3 worldDepthPos)
{

	float edgeFade = 1.0;

	#ifdef CETO_UNDERWATER_ON
	#ifndef CETO_DISABLE_EDGE_FADE

	//Fade based on dist between ocean surface and bottom
	#ifdef CETO_USE_OCEAN_DEPTHS_BUFFER
		float surfaceDepth = (surfacePos.y - Ceto_OceanLevel) * -1.0;
		float oceanDepth = SampleOceanDepthTexture(screenUV).x * Ceto_MaxDepthDist;
		float dist = oceanDepth - surfaceDepth;
	#else
		float dist = surfacePos.y - worldDepthPos.y;
	#endif

		dist = max(0.0, dist);
		edgeFade = 1.0 - saturate(exp(-dist * Ceto_EdgeFade) * 2.0);

		//Restrict blending when viewing ocean from a shallow angle
		//as it will cause some artifacts at horizon.
		float viewMaskStr = 10.0;
		float viewMask = saturate(dot(view, fixed3(0, 1, 0)) * viewMaskStr);

		edgeFade = lerp(1.0, edgeFade, viewMask);

	#endif
	#endif

	return edgeFade;
}

/*
* Fades the edge of the water where it meets other objects.
* The fade is based on the objects y distance from surface.
* If in the transparent queue then the fade can be used for the alpha.
* If in the opaque there is no alpha blending so need to do it manually from the
* screen grab. In this case the faded area sould not have lighting applied as
* the grab will alread have lighting. If not done this will result in double
* the amount of lighting being applied.
*/
fixed3 ApplyEdgeFade(fixed3 col, float2 grabUV, float edgeFade, out fixed alpha, out fixed lightMask)
{

	alpha = 1.0;
	lightMask = 0.0;

	#ifdef CETO_UNDERWATER_ON
	#ifndef CETO_DISABLE_EDGE_FADE

		#ifdef CETO_OPAQUE_QUEUE
			fixed3 grab = tex2D(Ceto_RefractionGrab, grabUV).rgb;
			col = lerp(grab, col, edgeFade);
			alpha = 1.0;
			lightMask = 1.0 - edgeFade;
		#endif

		#ifdef CETO_TRANSPARENT_QUEUE
			alpha = edgeFade;
			lightMask = 0.0;
		#endif

	#endif
	#endif

	return col;

}

/*
* Calculate the caustic color when above the water.
*/
fixed3 CausticsFromAbove(float2 disortionUV, half3 unmaskedNorm, float3 surfacePos, float3 distortedWorldDepthPos, float dist)
{

	fixed3 col = fixed3(0, 0, 0);

	#ifdef CETO_UNDERWATER_ON
	#ifndef CETO_USE_OCEAN_DEPTHS_BUFFER
	#ifndef CETO_DISABLE_CAUSTICS

	float2 uv = distortedWorldDepthPos.xz * Ceto_CausticTextureScale.xy + unmaskedNorm.xz * Ceto_CausticDistortion.x;

	//Depth fade fades the caustics the deeper the object is in the ocean.
	float depthFadeScale = Ceto_CausticTextureScale.w * Ceto_CausticTextureScale.w;
	float depthFade = exp(-max(0.0, surfacePos.y - distortedWorldDepthPos.y) * depthFadeScale);

	fixed3 caustic = tex2D(Ceto_CausticTexture, uv);

	//Normal fade makes the caustics only appear on top of opjects.
	float nf = tex2D(Ceto_NormalFade, disortionUV).x;

	//The dist fade fades the caustics the higher the camera is above the ocean.
	//This reduces aliasing issues and in forward mode the DepthNormal texture
	//gets noisey when far away from a object causing noise in the normal fade texture.
	float distFade = 1.0 - saturate(dist * 0.001);

	col = caustic * Ceto_CausticTint * nf * distFade * depthFade;

	#endif
	#endif
	#endif

	return col;

}

/*
* Calculate the caustic color when above the water.
*/
fixed3 CausticsFromBelow(float2 screenUV, half3 normal, float3 worldDepthPos, float dist)
{

	fixed3 col = fixed3(0, 0, 0);

	#ifdef CETO_UNDERWATER_ON
	#ifndef CETO_USE_OCEAN_DEPTHS_BUFFER
	#ifndef CETO_DISABLE_CAUSTICS

	float2 uv = worldDepthPos.xz * Ceto_CausticTextureScale.xy + normal.xz * Ceto_CausticDistortion.y;

	//Depth fade fades the caustics the deeper the object is in the ocean.
	float depthFadeScale = Ceto_CausticTextureScale.w * Ceto_CausticTextureScale.w;
	float depthFade = exp(-max(0.0, Ceto_OceanLevel - worldDepthPos.y) * depthFadeScale);

	fixed3 caustic = tex2D(Ceto_CausticTexture, uv);

	//Normal fade makes the caustics only appear on top of objects.
	float nf = tex2D(Ceto_NormalFade, screenUV).x;

	col = caustic * Ceto_CausticTint * nf * depthFade;

	#endif
	#endif
	#endif

	return col;

}

/*
* The underwater color used in the post effect shader.
*/ 
fixed3 UnderWaterColor(fixed3 belowColor, float dist)
{
	
	fixed3 col = belowColor;
	
	#ifdef CETO_UNDERWATER_ON
		
		col = belowColor * Ceto_BelowTint * exp(-Ceto_BelowCof.rgb * dist * Ceto_BelowCof.a);
		
		//For inscatter dist should be normalized to max dist.
		dist = dist / Ceto_MaxDepthDist;
		//Need to rescale otherwise the inscatter is to strong.
		dist *= 0.1;

		col = AddBelowInscatter(col, dist);

	#endif
	
	return col;

}

#endif








