// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#ifndef CETO_SHADER_HEADER_INCLUDED
#define CETO_SHADER_HEADER_INCLUDED

///////////////////////////////////////////////////////////
//                Common Block                          //
//////////////////////////////////////////////////////////


//#define CETO_DISABLE_SPECTRUM_DISPLACEMENT
//#define CETO_DISABLE_HEIGHT_OVERLAYS
//#define CETO_DISABLE_NO_DERIVATIVE_SAMPLING
//#define CETO_DISABLE_NO_DIFFUSE_IN_REFLECTIONS


#if !defined (M_PI)
#define M_PI 3.141592657
#endif

#if !defined (M_SQRT_PI)
#define M_SQRT_PI 1.7724538
#endif

#define OBJECT_TO_WORLD unity_ObjectToWorld

//#ifndef CETO_HIDE_DEPTH_TEXTURE
//sampler2D_float _CameraDepthTexture;
//#endif

//TODO - optimize this mess

///////////////////////////////////////////////////////////
//                Ocean BDRF Block                      //
//////////////////////////////////////////////////////////

sampler2D Ceto_Reflections0;
sampler2D Ceto_Reflections1;

float Ceto_SpecularRoughness;
float Ceto_SpecularIntensity;
float Ceto_MinFresnel;
float Ceto_FresnelPower;
float3 Ceto_ReflectionTint;
float Ceto_ReflectionDistortion;
float3 Ceto_DefaultSkyColor;

///////////////////////////////////////////////////////////
//                Ocean Displacement Block              //
//////////////////////////////////////////////////////////

/* 
* These are the maps produced by the wave spectrum.
* They contain the data for each of the four grids but a each packed differnt
* depending on the format and how they are sampled. 
* 
* Foam maps (foam map 0 currently unused)
* map1 x - Grid0
* map1 y - Grid1
* map1 z - Grid2
* map1 w - Grid3
*
* Slope maps
* map0 xy - Grid0
* map0 zw - Grid1
* map1 xy - Grid2
* map1 zw - Grid3
* 
* Displacement maps
* map0 xyz - Grid0
* map1 xyz - Grid1
* map2 xyz - Grid2
* map3 xyz - Grid3
*
*/
sampler2D Ceto_FoamMap0;
sampler2D Ceto_SlopeMap0, Ceto_SlopeMap1;
sampler2D Ceto_DisplacementMap0, Ceto_DisplacementMap1, Ceto_DisplacementMap2;
sampler2D Ceto_DisplacementMap3;

float3 Ceto_PosOffset;

/*
* These are the maps for the overlays.
* The xyz channels contain the data and the w chanel is the mask value.
*/
sampler2D Ceto_Overlay_NormalMap, Ceto_Overlay_HeightMap, Ceto_Overlay_FoamMap, Ceto_Overlay_ClipMap;

float4x4 Ceto_Interpolation;
float4x4 Ceto_ProjectorVP;
float4  Ceto_GridSizes;
float4 Ceto_Choppyness;
float2 Ceto_GridScale;
float2 Ceto_ScreenGridSize;
float Ceto_SlopeSmoothing;
float Ceto_FoamSmoothing;
float Ceto_WaveSmoothing;
float Ceto_MapSize;
float Ceto_GridEdgeBorder;
float Ceto_OceanLevel;
float Ceto_MaxWaveHeight;

///////////////////////////////////////////////////////////
//                Ocean Underwater Block                //
//////////////////////////////////////////////////////////

sampler2D Ceto_OceanDepth0, Ceto_OceanDepth1;
sampler2D Ceto_DepthBuffer;
sampler2D Ceto_NormalFade;

sampler2D Ceto_RefractionGrab;

float4x4 Ceto_Camera_IVP0, Ceto_Camera_IVP1;

float3 Ceto_SunDir;
float3 Ceto_SunColor;
float3 Ceto_DefaultOceanColor;
float Ceto_MaxDepthDist;
float Ceto_AboveRefractionIntensity;
float Ceto_BelowRefractionIntensity;
float Ceto_RefractionDistortion;
float3 Ceto_FoamTint;
float Ceto_DepthBlend;
float Ceto_EdgeFade;

float4 Ceto_SSSCof;
float3 Ceto_SSSTint;

float4 Ceto_AbsCof;
float3 Ceto_AbsTint;

float4 Ceto_BelowCof;
float3 Ceto_BelowTint;

float Ceto_AboveInscatterScale;
float3 Ceto_AboveInscatterMode;
float4 Ceto_AboveInscatterColor;

float Ceto_BelowInscatterScale;
float3 Ceto_BelowInscatterMode;
float4 Ceto_BelowInscatterColor;

sampler2D Ceto_FoamTexture0;
float4 Ceto_FoamTextureScale0;
sampler2D Ceto_FoamTexture1;
float4 Ceto_FoamTextureScale1;

float Ceto_TextureWaveFoam;

sampler2D Ceto_CausticTexture;
float4 Ceto_CausticTextureScale;
float3 Ceto_CausticTint;
float2 Ceto_CausticDistortion;

///////////////////////////////////////////////////////////
//                Ocean Masking Block                   //
//////////////////////////////////////////////////////////

sampler2D Ceto_OceanMask0, Ceto_OceanMask1;

#define EMPTY_MASK 0.0

#define TOP_MASK 0.25

#define UNDER_MASK 0.5

#define BOTTOM_MASK 1.0

#endif
