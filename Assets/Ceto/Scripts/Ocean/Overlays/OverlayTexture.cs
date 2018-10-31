using UnityEngine;
using System;
using System.Collections;

namespace Ceto
{

	[Serializable]
	public class OverlayFoamTexture
	{

		public Texture tex;
		public Vector2 scaleUV = Vector2.one;
		public Vector2 offsetUV;
        public bool textureFoam = true;

		[Range(0.0f, 4.0f)]
		public float alpha = 1.0f;

		public Texture mask;
		public OVERLAY_MASK_MODE maskMode = OVERLAY_MASK_MODE.WAVES;

		[Range(0, 1)]
		public float maskAlpha = 1.0f;

		public bool IsDrawable
		{
			get { return (alpha != 0.0f && tex != null) || (maskAlpha != 0.0f && mask != null); }
		}
    }

	[Serializable]
	public class OverlayNormalTexture
	{
		
		public Texture tex;
		public Vector2 scaleUV = Vector2.one;
		public Vector2 offsetUV;

        [Range(0.0f, 4.0f)]
		public float alpha = 1.0f;
		
		public Texture mask;
		public OVERLAY_MASK_MODE maskMode = OVERLAY_MASK_MODE.WAVES_AND_OVERLAY_BLEND;
		
		[Range(0, 1)]
		public float maskAlpha = 1.0f;
		
		public bool IsDrawable
		{
			get { return (alpha != 0.0f && tex != null) || (maskAlpha != 0.0f && mask != null); }
		}
	}

	[Serializable]
	public class OverlayHeightTexture
	{
		
		public Texture tex;
		public Vector2 scaleUV = Vector2.one;
		public Vector2 offsetUV;

        [Range(-Ocean.MAX_OVERLAY_WAVE_HEIGHT, Ocean.MAX_OVERLAY_WAVE_HEIGHT)]
		public float alpha = 1.0f;
		
		public Texture mask;
		public OVERLAY_MASK_MODE maskMode = OVERLAY_MASK_MODE.WAVES;
		
		[Range(0, 1)]
		public float maskAlpha = 1.0f;

		public bool ignoreQuerys = false;
		
		public bool IsDrawable
		{
			get { return (alpha != 0.0f && tex != null) || (maskAlpha != 0.0f && mask != null); }
		}
	}

	[Serializable]
	public class OverlayClipTexture
	{
		
		public Texture tex;
		public Vector2 scaleUV = Vector2.one;
		public Vector2 offsetUV;

        [Range(0.0f, 4.0f)]
		public float alpha = 1.0f;

		public bool ignoreQuerys = false;

		public bool IsDrawable
		{
			get { return (alpha != 0.0f && tex != null); }
		}
	}

}
