using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

namespace Ceto
{
    /// <summary>
    /// This is the common settings like enum and data classes used through out Ceto.
    /// </summary>
	
    /// <summary>
    /// The resolution settings for the ocean mesh.
    /// </summary>
	public enum MESH_RESOLUTION {  LOW, MEDIUM, HIGH, ULTRA, EXTREME  };

	/// <summary>
	/// The number of meshes the projected grid is split into.
	/// A higher group means more meshes and draw calls but
	/// since they are smaller they can the culled more easily.
	/// What ever the grouping the actual number of verts is the same
	/// for a given mesh resolution.
	/// </summary>
	public enum GRID_GROUPS { SINGLE = 0, LOW = 1, MEDIUM = 2, HIGH = 3, EXTREME = 4 };
	
    /// <summary>
    /// The resolution of the reflection render target texture.
    /// </summary>
	public enum REFLECTION_RESOLUTION { FULL, HALF, QUARTER };

    /// <summary>
    /// The resolution of the refraction render target texture.
    /// </summary>
    public enum REFRACTION_RESOLUTION { FULL, HALF, QUARTER };

	/// <summary>
	/// The size of the Fourier transform and if the displacement data runs on the GPU or CPU.
	/// </summary>
	public enum FOURIER_SIZE { LOW_32_CPU, LOW_32_GPU, MEDIUM_64_CPU, MEDIUM_64_GPU, HIGH_128_CPU, HIGH_128_GPU, ULTRA_256_GPU , EXTREME_512_GPU };

    /// <summary>
    /// Type of spectrum used to generate the waves.
    /// </summary>
    public enum SPECTRUM_TYPE {  UNIFIED, PHILLIPS, UNIFIED_PHILLIPS, CUSTOM };

    /// <summary>
    /// 
    /// </summary>
    public enum SPECTRUM_DISTRIBUTION {  LINEAR, GAUSSIAN };

    /// <summary>
    /// If the underwater effect is only applied when seen from above or when seen from above and below the waves.
    /// </summary>
    public enum UNDERWATER_MODE { ABOVE_ONLY, ABOVE_AND_BELOW, BELOW_ONLY };

    /// <summary>
    /// Shader pass for the overlay rendering.
    /// </summary>
	public enum OVERLAY_PASS { HEIGHT_ADD = 0, NORMAL_ADD = 1, FOAM_ADD = 2, CLIP_ADD = 3, HEIGHT_MAX = 4, FOAM_MAX = 5 };

	/// <summary>
	/// What the overlay mask is applied to.
	/// </summary>
	public enum OVERLAY_MASK_MODE { WAVES = 0, OVERLAY = 1, WAVES_AND_OVERLAY = 2, WAVES_AND_OVERLAY_BLEND = 3 };

    /// <summary>
    /// The size of the overlay map in relation to the screen size.
    /// </summary>
	public enum OVERLAY_MAP_SIZE { DOUBLE, FULL_HALF, FULL, HALF, QUARTER };

    /// <summary>
    /// The mode used to blend the overlays when they are rendered into the buffer.
    /// </summary>
    public enum OVERLAY_BLEND_MODE {  ADD, MAX };

    /// <summary>
    /// The wave query mode.
    /// </summary>
    public enum QUERY_MODE { POSITION, DISPLACEMENT, CLIP_TEST };

    /// <summary>
    /// The method for acquiring the depth data for the underwater effect.
    /// </summary>
    public enum DEPTH_MODE {  USE_OCEAN_DEPTH_PASS, USE_DEPTH_BUFFER };

    /// <summary>
    /// Affects how the inscatter is applied.
    /// </summary>
    public enum INSCATTER_MODE { LINEAR = 0, EXP = 1, EXP2 = 2 };

    /// <summary>
    /// Settings to modify the absorption.
    /// The absorption is how the light color
    /// changes as in moves through the water.
    /// </summary>
    [Serializable]
    public struct AbsorptionModifier
    {
        //Scales the distance used the absorption is applied to.
        [Range(0.0f, 50.0f)]
        public float scale;

        //Adjust the intensity of the final color.
        [Range(0.0f, 10.0f)]
        public float intensity;

        //Tint the final color.
        public Color tint;

        public AbsorptionModifier(float scale, float intensity, Color tint)
        {
            this.scale = scale;
            this.intensity = intensity;
            this.tint = tint;
        }
    }

    /// <summary>
    /// Settings to modify the inscatter.
    /// The inscatter represents the color of light that is 
    /// bounced back into the view direction of the camera.
    /// Is just applied using a basic fog style method.
    /// </summary>
    [Serializable]
    public struct InscatterModifier
    {
        //Scales the distance used the inscatter is applied to.
        [Range(0.0f, 5000.0f)]
        public float scale;

        //Adjust the intensity of the final color.
        [Range(0.0f, 2.0f)]
        public float intensity;

        //Method used to apply the inscatter.
        public INSCATTER_MODE mode;

        //The inscatter color.
        public Color color;

        public InscatterModifier(float scale, float intensity, Color color, INSCATTER_MODE mode)
        {
            this.scale = scale;
            this.intensity = intensity;
            this.color = color;
            this.mode = mode;
        }
    }

    /// <summary>
    /// Used to modify how the caustics are applied.
    /// </summary>
    [Serializable]
    public struct CausticModifier
    {

        /// <summary>
        /// The caustic distortion
        /// </summary>
        [Range(0.0f, 3.0f)]
        public float aboveDistortion;

        [Range(0.0f, 3.0f)]
        public float belowDistortion;

        /// <summary>
        /// The caustic distortion
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float depthFade;

        /// <summary>
        /// The caustic intensity
        /// </summary>
        [Range(0.0f, 3.0f)]
        public float intensity;

        /// <summary>
        /// The caustic Tint. 
        /// </summary>
        public Color tint;

        public CausticModifier(float aboveDistortion, float belowDistortion, float depthFade, float intensity, Color tint)
        {
            this.aboveDistortion = aboveDistortion;
            this.belowDistortion = belowDistortion;
            this.depthFade = depthFade;
            this.intensity = intensity;
            this.tint = tint;
        }

    }

    /// <summary>
    /// Used to apply the foam textures to the ocean script from editor.
    /// </summary>
    [Serializable]
    public class FoamTexture
    {
        public Texture tex;
        public Vector2 scale = Vector2.one;
		public float scrollSpeed = 1.0f;
    }

    /// <summary>
    /// Used to apply the foam textures to the ocean script from editor.
    /// </summary>
    [Serializable]
    public class CausticTexture
    {
        public Texture tex;
        public Vector2 scale = Vector2.one;
    }

    /// <summary>
    /// Settings to scale the grid when query sampling.
    /// </summary>
    public class QueryGridScaling
	{
		public Vector4 invGridSizes;
		public float scaleY;
		public Vector4 choppyness;
        public Vector3 offset;
        public int numGrids;
        public float[] result;
	}

	/// <summary>
	/// If a overlay contains a given point
	/// this is the result that contains the
	/// overlay and the points uv position.
	/// </summary>
	public struct QueryableOverlayResult
	{
		public WaveOverlay overlay;
		public float u;
		public float v;
	}


}







