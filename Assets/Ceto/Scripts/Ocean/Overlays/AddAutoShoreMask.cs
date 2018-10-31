using UnityEngine;
using System.Collections;

using Ceto.Common.Containers.Interpolation;

#pragma warning disable 219

namespace Ceto
{

    /// <summary>
    /// Allows a area of waves to be masked.
    /// Ideal for shoreline effects.
    /// Add this script to your terrain and the masks will be created 
    /// automatically at runtime. Maybe slow for large terrains.
    /// </summary>
    [AddComponentMenu("Ceto/Overlays/AddAutoShoreMask")]
    [DisallowMultipleComponent]
    public class AddAutoShoreMask : AddWaveOverlayBase
    {

        /// <summary>
        /// If true the masks will not be sampled by wave query's.
        /// </summary>
        public bool ignoreQuerys = false;

        /// <summary>
        /// Should the global foam texture be applied to
        /// the foam overlays.
        /// </summary>
        public bool textureFoam = true;

        /// <summary>
        /// The higher the value the larger the mask radius around the terrain.
        /// </summary>
        [Range(0.1f, 100.0f)]
        public float heightSpread = 10.0f;

        /// <summary>
        /// The higher the value the larger the mask radius around the terrain.
        /// </summary>
        [Range(0.1f, 10.0f)]
        public float foamSpread = 2.0f;

        /// <summary>
        /// The higher the value the further inshore the clipped area will be
        /// </summary>
        [Range(0.1f, 10.0f)]
        public float clipOffset = 4.0f;

        /// <summary>
        /// The actual resolution of the masks.
        /// </summary>
        public int resolution = 1024;

        /// <summary>
        /// Masks the wave heights.
        /// </summary>
        public bool useHeightMask = true;

        [Range(0, 1)]
        public float heightAlpha = 0.9f;

        /// <summary>
        /// Masks the wave normals.
        /// </summary>
        public bool useNormalMask = true;

        [Range(0, 1)]
        public float normalAlpha = 0.8f;

        /// <summary>
        /// Adds foam where tex value alpha value is > 0.
        /// </summary>
        public bool useEdgeFoam = true;

        [Range(0, 10)]
        public float edgeFoamAlpha = 1.0f;

        /// <summary>
        /// Masks the wave foam.
        /// </summary>
        public bool useFoamMask = true;

        [Range(0, 1)]
        public float foamMaskAlpha = 1.0f;

        /// <summary>
        /// Clips the ocean mesh where tex value alpha value is > 0.5.
        /// </summary>
        public bool useClipMask = true;

        /// <summary>
        /// Has overlay been added to ocean
        /// </summary>
        bool m_registered;

        /// <summary>
        /// 
        /// </summary>
        float m_width, m_height;

        /// <summary>
        /// 
        /// </summary>
        Texture2D m_heightMask, m_edgeFoam, m_clipMask;

        /// <summary>
        /// 
        /// </summary>
        float m_heightSpread, m_foamSpread, m_clipOffset, m_resolution;

        /// <summary>
        /// 
        /// </summary>
        protected override void Start()
        {

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

        protected override void OnDestroy()
        {

            base.OnDestroy();

            Release();

        }

		void UpdateOverlay()
		{

			if (Ocean.Instance != null && (!m_registered || SettingsChanged()))
			{
				CreateShoreMasks();
			}
			
			Vector2 halfSize = new Vector2(m_width * 0.5f, m_height * 0.5f);
			
			Vector3 pos = transform.position;
			pos.x += halfSize.x;
			pos.z += halfSize.y;
			
			m_overlays[0].Position = pos;
			m_overlays[0].HalfSize = halfSize;
			//m_overlays[0].Rotation = transform.rotation.y;

            m_overlays[0].HeightTex.maskAlpha = (useHeightMask) ? heightAlpha : 0.0f;
            m_overlays[0].HeightTex.ignoreQuerys = ignoreQuerys;

            m_overlays[0].NormalTex.maskAlpha = (useNormalMask) ? normalAlpha : 0.0f;
            m_overlays[0].NormalTex.maskMode = OVERLAY_MASK_MODE.WAVES_AND_OVERLAY_BLEND;

            m_overlays[0].FoamTex.alpha = (useEdgeFoam) ? edgeFoamAlpha : 0.0f;
			m_overlays[0].FoamTex.textureFoam = textureFoam;
            m_overlays[0].FoamTex.maskAlpha = (useFoamMask) ? foamMaskAlpha : 0.0f;

            m_overlays[0].ClipTex.alpha = (useClipMask) ? 1.0f : 0.0f;
            m_overlays[0].ClipTex.ignoreQuerys = ignoreQuerys;
			
			m_overlays[0].UpdateOverlay();

		}

        void CreateShoreMasks()
        {

            //float t = Time.realtimeSinceStartup;

            Release();

            Terrain terrain = GetComponent<Terrain>();

            if (terrain == null)
            {
                //If there gameobject has not terrain print a warning and return.
                //Do this rather than have a terrain as a required component as it would be
                //rather annoying for the script to create a terrain if added to wrong gameobject.
                Ocean.LogWarning("The AddAutoShoreMask script must be attached to a component with a Terrain. The shore mask will not be created.");
				enabled = false;
                return;
            }

			if (terrain.terrainData == null)
			{
				//This can happen if the terrain data in asset folder is deleted
				Ocean.LogWarning("The terrain data is null. The shore mask will not be created.");
				enabled = false;
				return;
			}

			Vector3 size = terrain.terrainData.size;

            resolution = Mathf.Clamp(resolution, 32, 4096);

            m_width = size.x;
			m_height = size.z;

            float level = Ocean.Instance.level;

            float[] data = ShoreMaskGenerator.CreateHeightMap(terrain);

            int actualResolution = terrain.terrainData.heightmapResolution;
            InterpolatedArray2f heightMap = new InterpolatedArray2f(data, actualResolution, actualResolution, 1, false);

            if(useHeightMask || useNormalMask || useFoamMask)
                m_heightMask = ShoreMaskGenerator.CreateMask(heightMap, resolution, resolution, level, heightSpread, TextureFormat.ARGB32);

            if(useEdgeFoam)
                m_edgeFoam = ShoreMaskGenerator.CreateMask(heightMap, resolution, resolution, level, foamSpread, TextureFormat.ARGB32);

            if (useClipMask)
                m_clipMask = ShoreMaskGenerator.CreateClipMask(heightMap, resolution, resolution, level + clipOffset, TextureFormat.ARGB32);

            if(useHeightMask)
                m_overlays[0].HeightTex.mask = m_heightMask;

            if(useNormalMask)
                m_overlays[0].NormalTex.mask = m_heightMask;

            if(useFoamMask)
                m_overlays[0].FoamTex.mask = m_heightMask;

            if(useEdgeFoam)
                m_overlays[0].FoamTex.tex = m_edgeFoam;

            if(useClipMask)
                m_overlays[0].ClipTex.tex = m_clipMask;

            if (!m_registered)
            {
                Ocean.Instance.OverlayManager.Add(m_overlays[0]);
                m_registered = true;
            }

            m_heightSpread = heightSpread;
            m_foamSpread = foamSpread;
            m_clipOffset = clipOffset;
            m_resolution = resolution;

            //Debug.Log("Shore mask creation time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

        bool SettingsChanged()
        {

            if (m_heightSpread != heightSpread) return true;
            if (m_foamSpread != foamSpread) return true;
            if (m_clipOffset != clipOffset) return true;
            if (m_resolution != resolution) return true;

            return false;

        }

        void Release()
        {

            if (m_heightMask != null)
            {
                Object.Destroy(m_heightMask);
                m_heightMask = null;
            }

            if (m_edgeFoam != null)
            {
                Object.Destroy(m_edgeFoam);
                m_edgeFoam = null;
            }

            if (m_clipMask != null)
            {
                Object.Destroy(m_clipMask);
                m_clipMask = null;
            }

            if(m_overlays != null && m_overlays.Count == 1)
            {
                m_overlays[0].HeightTex.mask = null;
                m_overlays[0].NormalTex.mask = null;
                m_overlays[0].FoamTex.mask = null;
                m_overlays[0].FoamTex.tex = null;
                m_overlays[0].ClipTex.tex = null;
            }

        }

    }

}







