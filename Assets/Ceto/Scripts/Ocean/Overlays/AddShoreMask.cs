using UnityEngine;
using System.Collections;

#pragma warning disable 219

namespace Ceto
{

    /// <summary>
    /// Allows a area of waves to be masked.
    /// Ideal for shoreline effects.
    /// </summary>
    [AddComponentMenu("Ceto/Overlays/AddShoreMask")]
    public class AddShoreMask : AddWaveOverlayBase
    {

		/// <summary>
		/// Rotation mode to apply to the overlays.
		/// </summary>
		public enum ROTATION { RELATIVE_TO_PARENT, INDEPENDANT_TO_PARENT };

        /// <summary>
        /// If true will check the height and clip 
        /// textures to see if read/write is enabled.
        /// </summary>
        public bool checkTextures = true;

        /// <summary>
        /// If true the masks will not be sampled by wave querys.
        /// </summary>
        public bool ignoreQuerys = false;

        /// <summary>
        /// Masks the wave heights.
        /// </summary>
        public Texture heightMask;

        [Range(0, 1)]
        public float heightAlpha = 1.0f;

        /// <summary>
        /// Masks the wave normals.
        /// </summary>
        public Texture normalMask;

        [Range(0, 1)]
        public float normalAlpha = 1.0f;

        /// <summary>
        /// Adds foam where tex value alpha value is > 0.
        /// </summary>
        public Texture edgeFoam;

        [Range(0, 10)]
        public float edgeFoamAlpha = 1.0f;

		/// <summary>
		/// Should the global foam texture be applied to
		/// the foam overlays.
		/// </summary>
		public bool textureFoam = true;

        /// <summary>
        /// Masks the wave foam.
        /// </summary>
        public Texture foamMask;

        [Range(0, 1)]
        public float foamMaskAlpha = 1.0f;

        /// <summary>
        /// Clips the ocean mesh where tex value alpha value is > 0.5.
        /// </summary>
        public Texture clipMask;

        /// <summary>
        /// The width of the overlay.
        /// </summary>
        public float width = 10.0f;

        /// <summary>
        /// The height of the overlay.
        /// </summary>
        public float height = 10.0f;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 offset;

		/// <summary>
		/// 
		/// </summary>
		public ROTATION m_rotationMode = ROTATION.RELATIVE_TO_PARENT;

        /// <summary>
        /// The world y rotation of the overlay.
        /// </summary>
        [Range(0.0f, 360.0f)]
        public float rotation = 0.0f;

        /// <summary>
        /// Has overlay been added to ocean
        /// </summary>
        bool m_registered;

        /// <summary>
        /// 
        /// </summary>
        protected override void Start()
        {

            if (checkTextures && !ignoreQuerys)
            {
                CheckCanSampleTex(heightMask, "height mask");
                CheckCanSampleTex(clipMask, "clip mask");
            }

			m_overlays.Add(new WaveOverlay());

			UpdateOverlay();

        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {

            if (m_overlays == null || m_overlays.Count != 1) return;

			UpdateOverlay();

        }

		void UpdateOverlay()
		{

			if (!m_registered && Ocean.Instance != null)
			{
				Ocean.Instance.OverlayManager.Add(m_overlays[0]);
				m_registered = true;
			}

			m_overlays[0].Position = transform.position + offset;
			m_overlays[0].HalfSize = new Vector2(width * 0.5f, height * 0.5f);
			m_overlays[0].Rotation = Rotation();

			m_overlays[0].HeightTex.mask = heightMask;
			m_overlays[0].HeightTex.maskAlpha = heightAlpha;
			m_overlays[0].HeightTex.ignoreQuerys = ignoreQuerys;
			
			m_overlays[0].NormalTex.mask = normalMask;
			m_overlays[0].NormalTex.maskAlpha = normalAlpha;
			m_overlays[0].NormalTex.maskMode = OVERLAY_MASK_MODE.WAVES_AND_OVERLAY_BLEND;
			
			m_overlays[0].FoamTex.tex = edgeFoam;
			m_overlays[0].FoamTex.alpha = edgeFoamAlpha;
			m_overlays[0].FoamTex.textureFoam = textureFoam;
			m_overlays[0].FoamTex.mask = foamMask;
			m_overlays[0].FoamTex.maskAlpha = foamMaskAlpha;
			
			m_overlays[0].ClipTex.tex = clipMask;
			m_overlays[0].ClipTex.ignoreQuerys = ignoreQuerys;

			m_overlays[0].UpdateOverlay();
		}

		/// <summary>
		/// Returns a rotation value depending on the rotation mode.
		/// </summary>
		float Rotation()
		{
			
			switch (m_rotationMode)
			{
			case ROTATION.RELATIVE_TO_PARENT:
				return transform.eulerAngles.y + rotation;
				
			case ROTATION.INDEPENDANT_TO_PARENT:
				return rotation;
				
			}
			
			return rotation;
			
		}

        /// <summary>
        /// 
        /// </summary>
        void OnDrawGizmos()
        {
            if (!enabled) return;

            Vector3 hs = new Vector3(width * 0.5f, 1.0f, height * 0.5f);
            Vector3 pos = transform.position + offset;

			Matrix4x4 ltw = Matrix4x4.TRS(new Vector3(pos.x, 0.0f, pos.z), Quaternion.Euler(0, Rotation(), 0), hs);

            Gizmos.color = Color.yellow;
            Gizmos.matrix = ltw;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 10.0f, 2));
        }

    }

}







