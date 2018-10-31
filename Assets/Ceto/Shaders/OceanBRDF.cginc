#ifndef CETO_OCEAN_BRDF_INCLUDED
#define CETO_OCEAN_BRDF_INCLUDED

struct SurfaceOutputOcean 
{
    fixed3 Albedo;
    half3 Normal; //Normal for specular
    half3 DNormal; //Normal for diffuse
    fixed3 Emission;
    fixed Fresnel;
	fixed Foam;
    fixed Alpha;
	fixed LightMask;
};

fixed FresnelAirWater(fixed3 V, fixed3 N) 
{

	float str = Ceto_FresnelPower;

	#ifdef CETO_BRDF_FRESNEL
	    fixed2 v = V.xz; // view direction in wind space
	    fixed2 t = v * v / (1.0 - V.y * V.y); // cos^2 and sin^2 of view direction
	    fixed sigmaV2 = dot(t, 0.004); // slope variance in view direction
	    
	    fixed sigmaV = 0.063;
	    fixed cosThetaV = dot(V, N);
	    
	    return saturate(Ceto_MinFresnel + (1.0-Ceto_MinFresnel) * pow(1.0 - cosThetaV, str * exp(-2.69 * sigmaV)) / (1.0 + 22.7 * pow(sigmaV, 1.5)));
    #else
    	return saturate(Ceto_MinFresnel + (1.0-Ceto_MinFresnel) * pow(1.0 - dot(V, N), str));
    #endif
}

fixed FresnelWaterAir(fixed3 V, fixed3 N) 
{

    return saturate(pow(1.0 - dot(V, N), Ceto_FresnelPower));
    
}

fixed3 SampleReflectionTexture(half2 reflectUV)
{
#if UNITY_VERSION >= 540 && defined(CETO_STERO_CAMERA)
	if (unity_StereoEyeIndex > 0)
		return tex2Dlod(Ceto_Reflections1, half4(reflectUV.xy, 0, 0)).xyz;
	else
		return tex2Dlod(Ceto_Reflections0, half4(reflectUV.xy, 0, 0)).xyz;
#else
	return tex2Dlod(Ceto_Reflections0, half4(reflectUV.xy, 0, 0)).xyz;
#endif
}

fixed3 ReflectionColor(half3 N, half2 reflectUV)
{

	fixed3 col = Ceto_DefaultSkyColor;

#ifdef CETO_REFLECTION_ON

	reflectUV += N.xz * Ceto_ReflectionDistortion;

	col = SampleReflectionTexture(reflectUV);

	col *= Ceto_ReflectionTint;

#endif

	return col;
}

half Lambda(half cosTheta, half sigma) 
{
	half v = cosTheta / sqrt((1.0 - cosTheta * cosTheta) * (2.0 * sigma));
	return (exp(-v * v)) / (2.0 * v * M_SQRT_PI);
}

half3 ReflectedSunRadianceNice(half3 V, half3 N, half3 L, fixed fresnel) 
{

	half3 Ty = half3(0.0, N.z, -N.y);
	half3 Tx = cross(Ty, N);
    
    half3 H = normalize(L + V);
	half dhn = dot(H, N);
	half idhn = 1.0 / dhn;
    half zetax = dot(H, Tx) * idhn;
    half zetay = dot(H, Ty) * idhn;

	half p = exp(-0.5 * (zetax * zetax / Ceto_SpecularRoughness + zetay * zetay / Ceto_SpecularRoughness)) / (2.0 * M_PI * Ceto_SpecularRoughness);

    half zL = dot(L, N); // cos of source zenith angle
    half zV = dot(V, N); // cos of receiver zenith angle
    half zH = dhn; // cos of facet normal zenith angle
    half zH2 = zH * zH;

    half tanV = atan2(dot(V, Ty), dot(V, Tx));
    half cosV2 = 1.0 / (1.0 + tanV * tanV);
    half sigmaV2 = Ceto_SpecularRoughness * cosV2 + Ceto_SpecularRoughness * (1.0 - cosV2);

    half tanL = atan2(dot(L, Ty), dot(L, Tx));
    half cosL2 = 1.0 / (1.0 + tanL * tanL);
    half sigmaL2 = Ceto_SpecularRoughness * cosL2 + Ceto_SpecularRoughness * (1.0 - cosL2);

    zL = max(zL, 0.01);
    zV = max(zV, 0.01);
    
    return (L.y < 0) ? 0.0 : Ceto_SpecularIntensity * p / ((1.0 + Lambda(zL, sigmaL2) + Lambda(zV, sigmaV2)) * zV * zH2 * zH2 * 4.0);

}


half ReflectedSunRadianceFast(half3 V, half3 N, half3 L, fixed fresnel) 
{

    half3 H = normalize(L + V);

    half hn = dot(H, N);
    half p = exp(-2.0 * ((1.0 - hn * hn) / Ceto_SpecularRoughness) / (1.0 + hn)) / (4.0 * M_PI * Ceto_SpecularRoughness);

    half zL = dot(L, N);
    half zV = dot(V, N);
    zL = max(zL,0.01);
    zV = max(zV,0.01);

    return (L.y < 0 || zL <= 0.0) ? 0.0 : max(Ceto_SpecularIntensity * p * sqrt(abs(zL / zV)), 0.0);
}

inline fixed4 OceanBRDFLight(SurfaceOutputOcean s, half3 viewDir, UnityLight light)
{

	fixed4 c = fixed4(0,0,0,1);
	
	half3 V = viewDir;
	half3 N = s.Normal;
	half3 DN = s.DNormal;

#ifdef CETO_OCEAN_UNDERSIDE
	N.y *= -1.0;
	DN.y *= -1.0;
	V.y *= -1.0;
#endif

#if defined (DIRECTIONAL)

	#ifdef CETO_NICE_BRDF
		half3 spec = ReflectedSunRadianceNice(V, N, light.dir, s.Fresnel);
	#else
		half3 spec = ReflectedSunRadianceFast(V, N, light.dir, s.Fresnel);
	#endif
	
	fixed diff = max (0, dot (DN, light.dir));

	#ifndef CETO_DISABLE_NO_DIFFUSE_IN_REFLECTIONS

		fixed a = s.Fresnel * (1.0 - s.Foam);

		fixed3 SpecAndDiffuse = s.Albedo * light.color * diff + light.color * spec;
		fixed3 SpecNoDiffuse = s.Albedo + light.color * spec;

		c.rgb = SpecNoDiffuse * a + SpecAndDiffuse * (1.0 - a);
	#else
		c.rgb = s.Albedo * light.color * diff + light.color * spec;
	#endif
	
#else

	half3 h = normalize (light.dir + V);
	
	fixed diff = max (0, dot (DN, light.dir));
	
	half nh = max (0, dot (N, h));
	half spec = pow (nh, 128.0);

	c.rgb = s.Albedo * light.color * diff + light.color * spec;

#endif

	return c;

}

inline fixed4 LightingOceanBRDF(SurfaceOutputOcean s, half3 viewDir, UnityGI gi)
{

	//return fixed4(s.Albedo,1);

	fixed4 c = OceanBRDFLight (s, viewDir, gi.light);


	#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
		#ifndef CETO_DISABLE_NO_DIFFUSE_IN_REFLECTION
			c.rgb += s.Albedo * gi.indirect.diffuse * (1.0 - s.Fresnel);
		#else
			c.rgb += s.Albedo * gi.indirect.diffuse;
		#endif
	#endif

	c.a = s.Alpha;

	c.rgb = lerp(c.rgb, s.Albedo, s.LightMask);

	return c;
}

inline void LightingOceanBRDF_GI (SurfaceOutputOcean s, UnityGIInput data, inout UnityGI gi)
{

	//UnityGlobalIllumination (UnityGIInput data, half occlusion, half oneMinusRoughness, half3 normalWorld, bool reflections)

	gi = UnityGlobalIllumination(data, 1.0, 1.0, s.DNormal, false);

	//gi.indirect.diffuse *= 0.0;
	//gi.indirect.specular *= 0.0;
}

#endif



