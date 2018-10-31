#ifndef CETO_OCEAN_DISPLACEMENT_INCLUDED
#define CETO_OCEAN_DISPLACEMENT_INCLUDED

/*
* Find the world position on the projection plane by 
* by interpolating the frustums projected corner points.
* The uv is the screen space in 0-1 range.
* Also adds a border around the mesh to prevent horizontal 
* displacement from pulling the mesh from the screen.
*/
float4 OceanPos(float4 uv)
{

	//Interpolation must use a uv in range of 0-1
	//Should be in 0-1 but saturate just in case.
	uv.xy = saturate(uv.xy);

	//Interpolate between frustums world space projection points.
	float4 p = lerp(lerp(Ceto_Interpolation[0], Ceto_Interpolation[1], uv.x), lerp(Ceto_Interpolation[3], Ceto_Interpolation[2],uv.x), uv.y);
	p = p / p.w;
	
	//Find the world position of the screens center position.
	float4 c = lerp(lerp(Ceto_Interpolation[0], Ceto_Interpolation[1], 0.5), lerp(Ceto_Interpolation[3], Ceto_Interpolation[2],0.5), 0.5);
	c = c / c.w;
	
	//Find the direction this position is relative to the meshes center.
	float3 worldDir = normalize(p.xyz-c.xyz);
	
	//if p and c are the same value the normalized
	//results in a nan on ATI cards. Clamp fixes this.
	worldDir = clamp(worldDir, -1, 1);
	
	//Apply edge border by pushing those verts in the border 
	//in the direction away from the center.
	float mask = saturate(uv.z + uv.w);
	p.xz += worldDir.xz * mask * Ceto_GridEdgeBorder;
	
	
	return p;
}

/*
* Same as OceanPos but does not add border.
*/
float4 OceanPosNoBorder(float4 uv)
{

	//Interpolation must use a uv in range of 0-1
	//Should be in 0-1 but saturate just in case.
	uv.xy = saturate(uv.xy);

	//Interpolate between frustums world space projection points.
	float4 p = lerp(lerp(Ceto_Interpolation[0], Ceto_Interpolation[1], uv.x), lerp(Ceto_Interpolation[3], Ceto_Interpolation[2], uv.x), uv.y);
	p = p / p.w;

	return p;
}

/*
* Finds the derivatives of the ocean pos at this uv in the screen
* and based on the current screen grid size. This is used when sampling 
* the wave textures to reduce aliasing. Used for the displacemnt maps.
* The uv is the world xz position.
*/
void OceanDerivatives(float4 uv, float4 oceanPos, out float2 dux, out float2 duy)
{

	float4 uv0 = uv;
	float4 uv1 = uv;
	
	uv0.xy += float2(Ceto_ScreenGridSize.x, 0);
	uv1.xy += float2(0, Ceto_ScreenGridSize.y);

	dux = OceanPosNoBorder(uv0).xz - oceanPos.xz;
	duy = OceanPosNoBorder(uv1).xz - oceanPos.xz;
}

/*
* Converts the world normal for a plane with a normal of (0,1,0)
* to tangent space.
*/
half3 TangentSpaceNormal(half3 worldNormal)
{
	worldNormal.z *= -1.0;
	return worldNormal.xzy;
}

/*
* Converts a slope to a world normal.
*/
half3 SlopeToWorldNormal(half2 slope)
{
	half3 norm;
	norm.xz = slope * -1.0;

	norm.y = 1.0;
   	//norm.y = sqrt(max(0.0, 1 - norm.x*norm.x - norm.z*norm.z));
   	
   	return norm;
}

/*
* Clips the ocean mesh if the value in the clip map is greater than 0.5.
*/
void OceanClip(float4 st, float3 worldPos)
{
	clip(0.5 - saturate(tex2Dlod(Ceto_Overlay_ClipMap, st).r));
}

/*
* Convert the worl pos to projector space.
* Used to sample the overlay maps.
*/
float4 WorldPosToProjectorSpace(float3 worldPos)
{
	float4 oceanPos = float4(worldPos, 1);
	oceanPos.y = Ceto_OceanLevel;
	
	float4 screenPos = mul(Ceto_ProjectorVP, oceanPos);

	float4 st = float4(screenPos.xy / screenPos.w, 0, 0);
	
	#ifndef UNITY_UV_STARTS_AT_TOP
		st.y = 1.0 - st.y;
	#endif
	
	return st;
				
}

/*
* Sampling a texture by derivatives is unsupported in vert shaders in SM3 but if you
* can manually calculate the derivates you can reproduce its effect using tex2Dlod 
*/
float4 tex2DGrad(sampler2D tex, float2 uv, float2 dx, float2 dy)
{

	float2 px = Ceto_MapSize.x * dx;
    float2 py = Ceto_MapSize.x * dy;
	float lod = 0.5 * log2(max(dot(px, px), dot(py, py)));
	return tex2Dlod(tex, float4(uv, 0, lod));
}

/*
* Sample the four grid displacement maps.
* The uv is the world xz position.
*/
float3 SampleDisplacement(float2 uv, float2 dux, float2 duy)
{

	float3 h = float3(0,0,0);
	float3 d = float3(0,0,0);

	#ifndef CETO_DISABLE_SPECTRUM_DISPLACEMENT
		#if SHADER_TARGET > 20

			uv += Ceto_PosOffset.xz;
	
			float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
			float4 igridSizes = 1.0f / gridSizes;

			d = tex2DGrad(Ceto_DisplacementMap0, uv * igridSizes.x, dux * igridSizes.x, duy * igridSizes.x).xyz;
			d.xz *= Ceto_Choppyness.x;
			h += d;
	
			d = tex2DGrad(Ceto_DisplacementMap1, uv * igridSizes.y, dux * igridSizes.y, duy * igridSizes.y).xyz;
			d.xz *= Ceto_Choppyness.y;
			h += d;
		
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
		d = tex2DGrad(Ceto_DisplacementMap2, uv * igridSizes.z, dux * igridSizes.z, duy * igridSizes.z).xyz;
		d.xz *= Ceto_Choppyness.z;
		h += d;
	
			#if SHADER_TARGET > 30
				d = tex2DGrad(Ceto_DisplacementMap3, uv * igridSizes.w, dux * igridSizes.w, duy * igridSizes.w).xyz;
				d.xz *= Ceto_Choppyness.w;
				h += d;
			#endif
		#endif
		
		#endif
	#endif
			
	return h * Ceto_GridScale.y;
}

/*
* Sample the four grid slope maps.
* The uv is the world xz position.
*/
half2 SampleSlope(float2 uv, half2 dux, half2 duy)
{

	half2 slope = half2(0,0);

	#ifdef CETO_DISABLE_NO_DERIVATIVE_SAMPLING
	#ifndef CETO_DISABLE_SPECTRUM_SLOPE

		uv += Ceto_PosOffset.xz;
	
		float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
		float4 igridSizes = 1.0f / gridSizes;
	
		slope += tex2D(Ceto_SlopeMap0, uv * igridSizes.x, dux * igridSizes.x, duy * igridSizes.x).xy;
		slope += tex2D(Ceto_SlopeMap0, uv * igridSizes.y, dux * igridSizes.y, duy * igridSizes.y).zw;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			slope += tex2D(Ceto_SlopeMap1, uv * igridSizes.z, dux * igridSizes.z, duy * igridSizes.z).xy;
			slope += tex2D(Ceto_SlopeMap1, uv * igridSizes.w, dux * igridSizes.w, duy * igridSizes.w).zw;
		#endif

	#endif
	#endif
	
	return slope;

}

/*
* Sample the four grid slope maps.
* The uv is the world xz position.
* Returns 3 slopes of increasing detail from 1 - 3
*/
void SampleSlope(float2 uv, half2 dux, half2 duy, out half2 slope1, out half2 slope2, out half2 slope3)
{

	half2 s = half2(0,0);
	slope1 = half2(0, 0);
	slope2 = half2(0, 0);
	slope3 = half2(0, 0);

	#ifdef CETO_DISABLE_NO_DERIVATIVE_SAMPLING
	#ifndef CETO_DISABLE_SPECTRUM_SLOPE

		uv += Ceto_PosOffset.xz;
	
		float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
		float4 igridSizes = 1.0f / gridSizes;
	
		s += tex2D(Ceto_SlopeMap0, uv * igridSizes.x, dux * igridSizes.x, duy * igridSizes.x).xy;
		s += tex2D(Ceto_SlopeMap0, uv * igridSizes.y, dux * igridSizes.y, duy * igridSizes.y).zw;
		slope1 = s;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			s += tex2D(Ceto_SlopeMap1, uv * igridSizes.z, dux * igridSizes.z, duy * igridSizes.z).xy;
		#endif
		slope2 = s;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			s += tex2D(Ceto_SlopeMap1, uv * igridSizes.w, dux * igridSizes.w, duy * igridSizes.w).zw;
		#endif
		slope3 = s;

	#endif
	#endif
	
}

/*
* Sample the four grid slope maps.
* The uv is the world xz position.
*/
half2 SampleSlope(float2 uv)
{

	half2 slope = half2(0,0);

	#ifndef CETO_DISABLE_SPECTRUM_SLOPE

		uv += Ceto_PosOffset.xz;
	
		float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
		float4 igridSizes = 1.0f / gridSizes;
	
		slope += tex2D(Ceto_SlopeMap0, uv * igridSizes.x).xy;
		slope += tex2D(Ceto_SlopeMap0, uv * igridSizes.y).zw;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			slope += tex2D(Ceto_SlopeMap1, uv * igridSizes.z).xy;
			slope += tex2D(Ceto_SlopeMap1, uv * igridSizes.w).zw;
		#endif

	#endif
	
	return slope;

}

/*
* Sample the four grid slope maps.
* The uv is the world xz position.
* Returns 3 slopes of increasing detail from 1 - 3
*/
void SampleSlope(float2 uv, out half2 slope1, out half2 slope2, out half2 slope3)
{

	half2 s = half2(0,0);
	slope1 = half2(0, 0);
	slope2 = half2(0, 0);
	slope3 = half2(0, 0);

	#ifndef CETO_DISABLE_SPECTRUM_SLOPE

		uv += Ceto_PosOffset.xz;
	
		float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
		float4 igridSizes = 1.0f / gridSizes;
	
		s += tex2D(Ceto_SlopeMap0, uv * igridSizes.x).xy;
		s += tex2D(Ceto_SlopeMap0, uv * igridSizes.y).zw;
		slope1 = s;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			s += tex2D(Ceto_SlopeMap1, uv * igridSizes.z).xy;
		#endif
		slope2 = s;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			s += tex2D(Ceto_SlopeMap1, uv * igridSizes.w).zw;
		#endif
		slope3 = s;

	#endif
	
}

/*
* Sample the four grid foam maps..
* The uv is the world xz position.
*/
fixed SampleFoam(float2 uv, half2 dux, half2 duy)
{

	uv += Ceto_PosOffset.xz;

	float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
	float4 igridSizes = 1.0f / gridSizes;

	fixed jacobiBreak = 0;
	
	#ifdef CETO_DISABLE_NO_DERIVATIVE_SAMPLING
	#ifndef CETO_DISABLE_SPECTRUM_FOAM

		jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.x, dux * igridSizes.x, duy * igridSizes.x).x;
		jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.y, dux * igridSizes.y, duy * igridSizes.y).y;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.z, dux * igridSizes.z, duy * igridSizes.z).z;
			jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.w, dux * igridSizes.w, duy * igridSizes.w).w;
		#endif
	
	#endif
	#endif
	
	fixed foam = saturate(-jacobiBreak);
	
	return foam;
}

/*
* Sample the four grid foam maps..
* The uv is the world xz position.
*/
fixed SampleFoam(float2 uv)
{

	uv += Ceto_PosOffset.xz;

	float4 gridSizes = Ceto_GridSizes * Ceto_GridScale.x;
	float4 igridSizes = 1.0f / gridSizes;

	fixed jacobiBreak = 0;
	
	#ifndef CETO_DISABLE_SPECTRUM_FOAM

		jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.x).x;
		jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.y).y;
	
		#ifdef CETO_USE_4_SPECTRUM_GRIDS
			jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.z).z;
			jacobiBreak += tex2D(Ceto_FoamMap0, uv * igridSizes.w).w;
		#endif
	
	#endif
	
	fixed foam = saturate(-jacobiBreak);
	
	return foam;
}

/*
* Find the ocean position and the displacement for the screen uv.
* The uv is in screen space in 0-1 range.
*/
void OceanPositionAndDisplacement(float4 uv, out float4 oceanPos, out float3 displacement)
{

	oceanPos = OceanPos(uv);
	
	displacement = float3(0,0,0);
	
	#if SHADER_TARGET > 20
	
		float2 dux, duy;
		OceanDerivatives(uv, oceanPos, dux, duy);
		
		displacement = SampleDisplacement(oceanPos.xz, dux * Ceto_WaveSmoothing, duy * Ceto_WaveSmoothing);

		//Clamp the vertical displacement to the max wave height.
		displacement.y = clamp(displacement.y, -Ceto_MaxWaveHeight, Ceto_MaxWaveHeight);
				
		float2 height = float2(0, 0);
		#ifndef CETO_DISABLE_HEIGHT_OVERLAYS

			float2 st = uv.xy;

			#ifndef UNITY_UV_STARTS_AT_TOP
				st.y = 1.0 - st.y;
			#endif

			height = tex2Dlod(Ceto_Overlay_HeightMap, float4(st, 0, 0)).xy;
		#endif

		float displacementMask = 1.0 - saturate(height.y);
	
		//Apply the overlay mask and then add the overlay heights.
		displacement *= displacementMask;
		displacement.y += height.x;
	
		//Fade the wave heights near the edge of the far plane. 
		//This means the wave heights should converge to a flat planee at the horizon.
		float fade = length((_WorldSpaceCameraPos-oceanPos.xyz) * float3(1,0,1)) * 0.80 * _ProjectionParams.w;
		fade = saturate(1.0 - fade*fade);
	
		displacement *= fade;		
	
	#endif
}

/*
* Find the ocean normal for the screen uv.
* The uv is in screen space in 0-1 range.
*/
half3 OceanNormal(float4 uv, float4 st, float3 worldPos)
{

	float4 oceanPos = OceanPos(uv);
	#ifndef CETO_DISABLE_NO_DERIVATIVE_SAMPLING
		half2 slope = SampleSlope(oceanPos.xz);
		
		half3 norm = SlopeToWorldNormal(slope);
	#else
		half2 dux = ddx(oceanPos.xz) * Ceto_SlopeSmoothing;
   		half2 duy = ddy(oceanPos.xz) * Ceto_SlopeSmoothing;
		half2 slope = SampleSlope(oceanPos.xz, dux, duy);
	
		half3 norm = SlopeToWorldNormal(slope);
	#endif
	
	half4 onorm = half4(0, 0, 0, 0);
	#ifndef CETO_DISABLE_HEIGHT_OVERLAYS
		onorm = tex2Dlod(Ceto_Overlay_NormalMap, st);
	#endif

	half normMask = 1.0 - saturate(onorm.w);

	//Apply the overlay mask and then add the overlay normals.
	norm.xz *= normMask;
	norm += onorm.xyz;
   	norm = normalize(norm);
   	
   	return norm;
}


/*
* Find the ocean normal and foam for the screen uv.
* The uv is in screen space in 0-1 range.
*/
void OceanNormalAndFoam(float4 uv, float4 st, float3 worldPos, out half3 norm, out half3 unmaskedNorm, out fixed4 foam)
{

	float4 oceanPos = OceanPos(uv);

	#ifndef CETO_DISABLE_NO_DERIVATIVE_SAMPLING
		half2 slope = SampleSlope(oceanPos.xz);
		norm = SlopeToWorldNormal(slope);
   	
   		foam = fixed4(0,0,0,0);
   		foam.x = SampleFoam(oceanPos.xz);
	#else
		float2 dux = ddx(oceanPos.xz);
		float2 duy = ddy(oceanPos.xz);
		half2 slope = SampleSlope(oceanPos.xz, dux * Ceto_SlopeSmoothing, duy *  Ceto_SlopeSmoothing);
		norm = SlopeToWorldNormal(slope);
   	
   		foam = fixed4(0,0,0,0);
   		foam.x = SampleFoam(oceanPos.xz, dux * Ceto_FoamSmoothing, duy * Ceto_FoamSmoothing);
   	#endif

	half4 onorm = half4(0, 0, 0, 0);
	#ifndef CETO_DISABLE_HEIGHT_OVERLAYS
		onorm = tex2Dlod(Ceto_Overlay_NormalMap, st);
	#endif

	fixed4 ofoam = fixed4(0, 0, 0, 0);
	#ifndef CETO_DISABLE_FOAM_OVERLAYS
		ofoam = tex2Dlod(Ceto_Overlay_FoamMap, st);
	#endif

	unmaskedNorm = norm;

	half normMask = 1.0 - saturate(onorm.w);
	fixed foamMask = 1.0 - saturate(ofoam.w);
	
	//Apply the overlay mask and then add the overlay normals.
	norm.xz *= normMask;
	norm += onorm.xyz;
   	norm = normalize(norm);
   	
	//Apply the overlay mask and then add the overlay foam.
	//Spectrum foam in x, overlay foam in y.
   	foam.x *= foamMask;
   	foam.y = ofoam.x;
	foam.z = ofoam.y;
}

/*
* Find the ocean normal and foam for the screen uv.
* The uv is in screen space in 0-1 range.
* Returns 3 normals of increasing detail from 1 - 3
*/
void OceanNormalAndFoam(float4 uv, float4 st, float3 worldPos, out half3 norm1, out half3 norm2, out half3 norm3, out half3 unmaskedNorm2, out fixed4 foam)
{

	float4 oceanPos = OceanPos(uv);
	
	#ifndef CETO_DISABLE_NO_DERIVATIVE_SAMPLING
	   	half2 slope1, slope2, slope3;
		SampleSlope(oceanPos.xz, slope1, slope2, slope3);
		
		norm1 = SlopeToWorldNormal(slope1);
	   	norm2 = SlopeToWorldNormal(slope2);
	   	norm3 = SlopeToWorldNormal(slope3);
	   	
	   	foam = fixed4(0,0,0,0);
	   	foam.x = SampleFoam(oceanPos.xz);
	#else
		float2 dux = ddx(oceanPos.xz);
		float2 duy = ddy(oceanPos.xz);
	   	half2 slope1, slope2, slope3;
		SampleSlope(oceanPos.xz, dux * Ceto_SlopeSmoothing, duy *  Ceto_SlopeSmoothing, slope1, slope2, slope3);
		
		norm1 = SlopeToWorldNormal(slope1);
	   	norm2 = SlopeToWorldNormal(slope2);
	   	norm3 = SlopeToWorldNormal(slope3);
	   	
	   	foam = fixed4(0,0,0,0);
	   	foam.x = SampleFoam(oceanPos.xz, dux * Ceto_FoamSmoothing, duy * Ceto_FoamSmoothing);
   	#endif

	half4 onorm = half4(0, 0, 0, 0);
	#ifndef CETO_DISABLE_HEIGHT_OVERLAYS
		onorm = tex2Dlod(Ceto_Overlay_NormalMap, st);
	#endif

	fixed4 ofoam = fixed4(0, 0, 0, 0);
	#ifndef CETO_DISABLE_FOAM_OVERLAYS
		ofoam = tex2Dlod(Ceto_Overlay_FoamMap, st);
	#endif

	unmaskedNorm2 = norm2;
	
	half normMask = 1.0 - saturate(onorm.w);
	fixed foamMask = 1.0 - saturate(ofoam.w);
	
	//Apply the overlay mask and then add the overlay normals.
	norm1.xz *= normMask;
	norm2.xz *= normMask;
	norm3.xz *= normMask;
	
	norm1 += onorm.xyz;
	norm2 += onorm.xyz;
	norm3 += onorm.xyz;
	
   	norm1 = normalize(norm1);
   	norm2 = normalize(norm2);
   	norm3 = normalize(norm3);
   	
	//Apply the overlay mask and then add the overlay foam.
	//Spectrum foam in x, overlay foam in y.
   	foam.x *= foamMask;
   	foam.y = ofoam.x;
	foam.z = ofoam.y;
}


#endif
