using UnityEngine;
using System.Collections;

#pragma warning disable 219

namespace Ceto
{

    /// <summary>
    /// Provides a generic script to add any of the 
    /// overlay types to the ocean. Check to see if 
    /// there is a add overlay script that is specific
    /// to your needs.
    /// </summary>
	[AddComponentMenu("Ceto/Overlays/AddWaveOverlay")]
    public class AddWaveOverlay : AddWaveOverlayBase 
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
        /// Allows the wave height to be modified
        /// at this locations.
        /// </summary>
		public OverlayHeightTexture heightTexture = new OverlayHeightTexture();

        /// <summary>
        /// Allows the normal to be modified
        /// at this locations.
        /// </summary>
		public OverlayNormalTexture normalTexture = new OverlayNormalTexture();

        /// <summary>
        /// Allows the wave foam to be modified
        /// at this locations.
        /// </summary>
		public OverlayFoamTexture foamTexture = new OverlayFoamTexture();

        /// <summary>
        /// Allows the ocean mesh to be clipped
        /// at this locations.
        /// </summary>
		public OverlayClipTexture clipTexture = new OverlayClipTexture();

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
		protected override void Start () 
		{

			if(checkTextures)
			{
				if(!heightTexture.ignoreQuerys)
					CheckCanSampleTex(heightTexture.tex, "height texture");

				if(!heightTexture.ignoreQuerys)
					CheckCanSampleTex(heightTexture.mask, "height mask");

				if(!clipTexture.ignoreQuerys)
					CheckCanSampleTex(clipTexture.tex, "clip texture");
			}

			Vector2 halfSize = new Vector2(width * 0.5f, height * 0.5f);

			m_overlays.Add( new WaveOverlay(transform.position, Rotation(), halfSize, 0.0f) );

            m_overlays[0].HeightTex = heightTexture;
            m_overlays[0].NormalTex = normalTexture;
            m_overlays[0].FoamTex = foamTexture;
            m_overlays[0].ClipTex = clipTexture;

            if (!m_registered && Ocean.Instance != null)
            {
                Ocean.Instance.OverlayManager.Add(m_overlays[0]);
                m_registered = true;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Update() 
		{

            if (m_overlays == null || m_overlays.Count != 1) return;

            if (!m_registered && Ocean.Instance != null)
            {
                Ocean.Instance.OverlayManager.Add(m_overlays[0]);
                m_registered = true;
            }

            //TODO - only update if changed
            m_overlays[0].Position = transform.position;
            m_overlays[0].HalfSize = new Vector2(width * 0.5f, height * 0.5f);
			m_overlays[0].Rotation = Rotation();

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
			if(!enabled) return;

			Vector3 hs = new Vector3(width * 0.5f, 1.0f, height * 0.5f);
			Vector3 pos = transform.position;

			Matrix4x4 ltw = Matrix4x4.TRS(new Vector3(pos.x, 0.0f, pos.z), Quaternion.Euler(0, Rotation(), 0), hs);

			Gizmos.color = Color.yellow;
			Gizmos.matrix = ltw;
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 10.0f, 2));
		}

	}

}







