using UnityEngine;
using System;
using System.Collections.Generic;

using Ceto.Common.Unity.Utility;

namespace Ceto
{
	/// <summary>
	/// Manages the overlay system.
	/// Responsible for rendering the overlays, 
	/// removing killed overlays and handling 
	/// query's in the overlays.
	/// 
	/// </summary>
	public class OverlayManager : IOverlaySampler
    {

        static readonly Vector2 TEXTURE_FOAM = new Vector2(1, 0);
        static readonly Vector2 DONT_TEXTURE_FOAM = new Vector2(0, 1);

        /// <summary>
        /// Does the manger have a overlay that renders heights.
        /// </summary>
        public bool HasHeightOverlay { get; private set; }

		/// <summary>
		/// Does the manger have a overlay that renders normal.
		/// </summary>
		public bool HasNormalOverlay { get; private set; }

		/// <summary>
		/// Does the manger have a overlay that renders foam.
		/// </summary>
		public bool HasFoamOverlay { get; private set; }

		/// <summary>
		/// Does the manger have a overlay that renders clip.
		/// </summary>
		public bool HasClipOverlay { get; private set; }

        /// <summary>
        /// The maximum amount of displacement on y axis
        /// from any of the overlays. 
        /// </summary>
        public float MaxDisplacement { get; private set; }

        /// <summary>
        /// The mode used to blend the overlays when they are rendered into the buffer.
        /// </summary>
        public OVERLAY_BLEND_MODE HeightOverlayBlendMode { get; set; }

        /// <summary>
        /// The mode used to blend the overlays when they are rendered into the buffer.
        /// </summary>
        public OVERLAY_BLEND_MODE FoamOverlayBlendMode { get; set; }

        /// <summary>
        /// The overlay material.
        /// </summary>
        Material m_overlayMat;

		/// <summary>
		/// The overlays.
		/// </summary>
		LinkedList<WaveOverlay> m_waveOverlays;

		/// <summary>
		/// All the overlays that can be queried.
		/// </summary>
		List<WaveOverlay> m_queryableOverlays;

        /// <summary>
        /// Tmp list of overlays that need to be removed.
        /// </summary>
        List<WaveOverlay> m_removeOverlays;

        /// <summary>
        /// The m_containing that contain the query pos.
        /// </summary>
        List<QueryableOverlayResult> m_containingOverlays;

		/// <summary>
		/// A texture that when sampled from
		/// as a normal will give 0.
		/// </summary>
		Texture2D m_blankNormal;

		bool m_beenCleared;

        /// <summary>
        /// Temp arrays to hold overlays when they 
        /// get separated out before rendering.
        /// </summary>
        List<WaveOverlay> m_heightOverlays;
        List<WaveOverlay> m_normalOverlays;
        List<WaveOverlay> m_foamOverlays;
        List<WaveOverlay> m_clipOverlays;

        Plane[] m_planes;

        Color m_clearColor;

        public OverlayManager( Material mat)
		{

            HeightOverlayBlendMode = OVERLAY_BLEND_MODE.ADD;
            FoamOverlayBlendMode = OVERLAY_BLEND_MODE.ADD;

            MaxDisplacement = 1.0f;

			m_overlayMat = mat;
			m_waveOverlays = new LinkedList<WaveOverlay>();
			m_queryableOverlays = new List<WaveOverlay>(32);
            m_removeOverlays = new List<WaveOverlay>(32);
            m_containingOverlays = new List<QueryableOverlayResult>(32);

            m_heightOverlays = new List<WaveOverlay>(32);
            m_normalOverlays = new List<WaveOverlay>(32);
            m_foamOverlays = new List<WaveOverlay>(32);
            m_clipOverlays = new List<WaveOverlay>(32);

            //Just need a texture to represent a 'blank' normal when overlay normal tex is null.
            //Only needs to be 1 pixel but PS4 has minimum texture size of 4x4.
            m_blankNormal = new Texture2D(4, 4, TextureFormat.ARGB32, false, true);
            m_blankNormal.hideFlags = HideFlags.HideAndDontSave;
            m_blankNormal.name = "Ceto Blank Normal Texture";

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    m_blankNormal.SetPixel(x, y, new Color(0.5f, 0.5f, 1.0f, 0.5f));
                }
            }

            m_blankNormal.Apply();

            m_clearColor = new Color(0, 0, 0, 0);

            m_planes = new Plane[6];

        }

        /// <summary>
        /// Release resources.
        /// </summary>
        public void Release()
        {

            UnityEngine.Object.DestroyImmediate(m_blankNormal);

        }

		/// <summary>
		/// Updates what type of overlays manager has,
		/// removes any overlays marked as kill
		/// and sorts the overlays that can be queried.
		/// </summary>
		public void Update()
		{

			HasHeightOverlay = false;
			HasNormalOverlay = false;
			HasFoamOverlay = false;
			HasClipOverlay = false;

            MaxDisplacement = 1.0f;

            m_queryableOverlays.Clear();
            m_removeOverlays.Clear();

            var e1 = m_waveOverlays.GetEnumerator();
			while(e1.MoveNext())
			{
                WaveOverlay overlay = e1.Current;

				if(overlay.Kill)
				{
                    m_removeOverlays.Add(overlay);
				}
				else if(!overlay.Hide)
				{
					bool canQuery = false;

					if(overlay.HeightTex.IsDrawable)
					{
						HasHeightOverlay = true;
						canQuery = true;

                        if (overlay.HeightTex.alpha > MaxDisplacement)
                            MaxDisplacement = overlay.HeightTex.alpha;
                    }

					if(overlay.NormalTex.IsDrawable)
						HasNormalOverlay = true;

					if(overlay.FoamTex.IsDrawable)
						HasFoamOverlay = true;

					if(overlay.ClipTex.IsDrawable)
					{
						HasClipOverlay = true;
						canQuery = true;
					}

					//if overlay has a drawable height or clip texture
					//then this overlay can be queried.
					if(canQuery)
						m_queryableOverlays.Add(overlay);
				}
			}

            MaxDisplacement = Mathf.Min(MaxDisplacement, Ocean.MAX_OVERLAY_WAVE_HEIGHT);

            for(int i = 0; i < m_removeOverlays.Count; i++)
            {
				m_waveOverlays.Remove(m_removeOverlays[i]);
			}

		}

		/// <summary>
		/// Add the specified overlay.
		/// </summary>
		public void Add(WaveOverlay overlay)
		{
			if(overlay.Kill == true)
				return;

			m_waveOverlays.AddLast(overlay);
		}

		/// <summary>
		/// Remove the specified overlay.
		/// </summary>
		public void Remove(WaveOverlay overlay)
		{
			m_waveOverlays.Remove(overlay);
		}

		/// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear()
		{
			m_waveOverlays.Clear();
		}

        /// <summary>
        /// 
        /// </summary>
        public void ClearBuffers(WaveOverlayData data)
        {
            RTUtility.ClearColor(data.height, m_clearColor);
            RTUtility.ClearColor(data.normal, m_clearColor);
            RTUtility.ClearColor(data.foam, m_clearColor);
            RTUtility.ClearColor(data.clip, m_clearColor);
        }

        /// <summary>
        /// 
        /// </summary>
        public void DestroyBuffers(WaveOverlayData data)
        {
            RTUtility.ReleaseAndDestroy(data.normal);
            RTUtility.ReleaseAndDestroy(data.height);
            RTUtility.ReleaseAndDestroy(data.foam);
            RTUtility.ReleaseAndDestroy(data.clip);

            data.normal = null;
            data.height = null;
            data.foam = null;
            data.clip = null;
        }

		/// <summary>
		/// Returns true if any overlay in manager contains the xz pos.
		/// </summary>
		public bool QueryableContains(float x, float z, bool overrideIqnoreQuerys)
		{

            for(int i = 0; i < m_queryableOverlays.Count; i++)
			{
                WaveOverlay overlay = m_queryableOverlays[i];

				if(overlay.Hide) continue;

				bool b1 = (overrideIqnoreQuerys || !overlay.HeightTex.ignoreQuerys) && overlay.HeightTex.IsDrawable;
				bool b2 = (overrideIqnoreQuerys || !overlay.ClipTex.ignoreQuerys) && overlay.ClipTex.IsDrawable;
				
				if(!b1 && !b2) continue;

				if(overlay.Contains(x, z)) return true;
			}
			
			return false;
		}

		/// <summary>
		/// Gets all the query-able overlays that contain xz pos.
		/// </summary>
		public void GetQueryableContaining(float x, float z, bool overrideIqnoreQuerys, bool clipOnly)
		{

			m_containingOverlays.Clear();

            for (int i = 0; i < m_queryableOverlays.Count; i++)
            {
                WaveOverlay overlay = m_queryableOverlays[i];

                if (overlay.Hide) continue;

				bool b1 = !clipOnly && (overrideIqnoreQuerys || !overlay.HeightTex.ignoreQuerys) && overlay.HeightTex.IsDrawable;
				bool b2 = (overrideIqnoreQuerys || !overlay.ClipTex.ignoreQuerys) && overlay.ClipTex.IsDrawable;
				
				if(!b1 && !b2) continue;

				float u, v;
				if(overlay.Contains(x, z, out u, out v))
				{
					QueryableOverlayResult result;
					result.overlay = overlay;
					result.u = u;
					result.v = v;

					m_containingOverlays.Add(result);
				}
				
			}
		}

		/// <summary>
		/// Queries the waves.
		/// </summary>
		public void QueryWaves(WaveQuery query)
		{
			
			if(m_queryableOverlays.Count == 0) return;
			if(!query.sampleOverlay) return;

            bool clipOnly = query.mode == QUERY_MODE.CLIP_TEST;

            float x = query.posX;
            float z = query.posZ;

			//Find all the overlays that have a affect on the wave height at this position
			//This will be overlays with a height tex, a height mask or a clip texture
			GetQueryableContaining(x, z, query.overrideIgnoreQuerys, clipOnly);

			float clipSum = 0.0f;
			float heightSum = 0.0f;
			float maskSum = 0.0f;

			OverlayClipTexture clipTex = null;
			OverlayHeightTexture heightTex = null;

            for(int i = 0; i < m_containingOverlays.Count; i++)
			{

                QueryableOverlayResult result = m_containingOverlays[i];

                //If enable read/write is not enabled on tex it will throw a exception.
                //Catch and ignore.
                try
				{
					clipTex = result.overlay.ClipTex;
					heightTex = result.overlay.HeightTex;

					//If overlay has a clip tex sample it.
					if(clipTex.IsDrawable && clipTex.tex is Texture2D)
					{
						float clip = (clipTex.tex as Texture2D).GetPixelBilinear(result.u, result.v).a;
						clipSum += clip * Mathf.Max(0.0f, clipTex.alpha);
					}

					//If overlay has a height or mask tex sample it.
					if(!clipOnly && heightTex.IsDrawable)
					{

						float alpha = heightTex.alpha;
						float maskAlpha = Mathf.Max(0.0f, heightTex.maskAlpha);

						float height = 0.0f;
						float mask = 0.0f;

						if(heightTex.tex != null && heightTex.tex is Texture2D)
						{
							height = (heightTex.tex as Texture2D).GetPixelBilinear(result.u, result.v).a;
						}

						if(heightTex.mask != null && heightTex.mask is Texture2D)
						{
							mask = (heightTex.mask as Texture2D).GetPixelBilinear(result.u, result.v).a;
							mask = Mathf.Clamp01(mask * maskAlpha);
						}

						//Apply the height and mask depending on mask mode.
						if(heightTex.maskMode == OVERLAY_MASK_MODE.WAVES)
						{
							height *= alpha;
						}
						else if(heightTex.maskMode == OVERLAY_MASK_MODE.OVERLAY)
						{
							height *= alpha * mask;
							mask = 0;
						}
						else if(heightTex.maskMode == OVERLAY_MASK_MODE.WAVES_AND_OVERLAY)
						{
							height *= alpha * (1.0f-mask);
						}
						else if(heightTex.maskMode == OVERLAY_MASK_MODE.WAVES_AND_OVERLAY_BLEND)
						{
							height *= alpha * mask;
						}

                        if (HeightOverlayBlendMode == OVERLAY_BLEND_MODE.ADD)
                        {
                            heightSum += height;
                            maskSum += mask;
                        }
                        else if(HeightOverlayBlendMode == OVERLAY_BLEND_MODE.MAX)
                        {
                            heightSum = Mathf.Max(height, heightSum);
                            maskSum = Mathf.Max(mask, maskSum);
                        }

					}

				}
				catch {}
			}

			clipSum = Mathf.Clamp01(clipSum);

			if(0.5f - clipSum < 0.0f)
			{
				query.result.isClipped = true;
			}

			maskSum = 1.0f - Mathf.Clamp01(maskSum);

			query.result.height *= maskSum;
			query.result.displacementX *= maskSum;
			query.result.displacementZ *= maskSum;

			query.result.height += heightSum;

            query.result.overlayHeight = heightSum;

		}

		/// <summary>
		/// Renders the wave overlays for this camera.
		/// </summary>
		public void RenderWaveOverlays(Camera cam, WaveOverlayData data)
		{

			if(!m_beenCleared)
			{
                ClearBuffers(data);
				m_beenCleared = true;
			}

			if(m_waveOverlays.Count == 0) return;
            
            //Copy camera planes into m_planes array.
            GetFrustumPlanes(cam.projectionMatrix * cam.worldToCameraMatrix);

			m_heightOverlays.Clear();
            m_normalOverlays.Clear();
            m_foamOverlays.Clear();
            m_clipOverlays.Clear();

            //rest the bounds check for the overlay parent if set.
            var e = m_waveOverlays.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.ParentBounds != null)
                    e.Current.ParentBounds.BoundsChecked = false;
            }

            e = m_waveOverlays.GetEnumerator();
            while(e.MoveNext())
			{

                WaveOverlay overlay = e.Current;

                //If camera farther than draw distance dont draw overlay.
                float distance = (overlay.Position - cam.transform.position).magnitude;
                if (distance > overlay.DrawDistance) continue;

                //If overlay has a parent check if its bounds is visible.
                bool parentVisible = true;
                if (overlay.ParentBounds != null)
                {
                    parentVisible = false;

                    if (overlay.ParentBounds.BoundsChecked)
                        parentVisible = overlay.ParentBounds.BoundsVisible;
                    else
                    {
                        parentVisible = GeometryUtility.TestPlanesAABB(m_planes, overlay.ParentBounds.BoundingBox);
                        overlay.ParentBounds.BoundsVisible = parentVisible;
                        overlay.ParentBounds.BoundsChecked = true;
                    }
                }

                //Parent not visible so continue.
                if (!parentVisible) continue;

                //Parent is visible so update overlay bounds.
                //Use a stamp so bounds will not be updated again if second camera renders it.
                if(overlay.BoundsUpdateStamp != Time.frameCount)
                {
                    overlay.CalculateBounds();
                    overlay.BoundsUpdateStamp = Time.frameCount;
                }

                //Create a alpha based on the draw distance.
                //Used to fade in the overlay as the camera enters the draw distance.
                //Only used for the foam atm.
                float distanceAlpha = 0.0f;
                if(overlay.DrawDistance != 0.0f && !float.IsInfinity(overlay.DrawDistance))
                    distanceAlpha = distance / overlay.DrawDistance;

                overlay.DistanceAlpha = 1.0f - distanceAlpha * distanceAlpha * distanceAlpha;

                if (!overlay.Hide && GeometryUtility.TestPlanesAABB(m_planes, overlay.BoundingBox))
				{
					if(overlay.HeightTex.IsDrawable)
					{
                        m_heightOverlays.Add(overlay);
					}
					
					if(overlay.NormalTex.IsDrawable)
					{
                        m_normalOverlays.Add(overlay);
					}
					
					if(overlay.FoamTex.IsDrawable)
					{
                        m_foamOverlays.Add(overlay);
					}

					if(overlay.ClipTex.IsDrawable)
					{
                        m_clipOverlays.Add(overlay);
					}
					
				}
				
			}

			RenderHeightOverlays(data.height);
			RenderNormalOverlays(data.normal);
			RenderFoamOverlays(data.foam);
			RenderClipOverlays(data.clip);

            //If buffer has been created apply it to shaders
            if (data.normal != null)
                Shader.SetGlobalTexture("Ceto_Overlay_NormalMap", data.normal);
            else
                Shader.SetGlobalTexture("Ceto_Overlay_NormalMap", Texture2D.blackTexture);

            if (data.height != null)
                Shader.SetGlobalTexture("Ceto_Overlay_HeightMap", data.height);
            else
                Shader.SetGlobalTexture("Ceto_Overlay_HeightMap", Texture2D.blackTexture);

            if (data.foam != null)
                Shader.SetGlobalTexture("Ceto_Overlay_FoamMap", data.foam);
            else
                Shader.SetGlobalTexture("Ceto_Overlay_FoamMap", Texture2D.blackTexture);

            if (data.clip != null)
                Shader.SetGlobalTexture("Ceto_Overlay_ClipMap", data.clip);
            else
                Shader.SetGlobalTexture("Ceto_Overlay_ClipMap", Texture2D.blackTexture);

            m_beenCleared = false;

		}

        public void GetFrustumPlanes(Matrix4x4 mat)
        {
            float x, y, z;
            // Extract the LEFT plane
            x = mat.m30 + mat.m00;
            y = mat.m31 + mat.m01;
            z = mat.m32 + mat.m02;
            m_planes[0].normal = new Vector3(x, y, z);
            m_planes[0].distance = mat.m33 + mat.m03;
            // Extract the RIGHT plane
            x = mat.m30 - mat.m00;
            y = mat.m31 - mat.m01;
            z = mat.m32 - mat.m02;
            m_planes[1].normal = new Vector3(x, y, z);
            m_planes[1].distance = mat.m33 - mat.m03;
            // Extract the BOTTOM plane
            x = mat.m30 + mat.m10;
            y = mat.m31 + mat.m11;
            z = mat.m32 + mat.m12;
            m_planes[2].normal = new Vector3(x, y, z);
            m_planes[2].distance = mat.m33 + mat.m13;
            // Extract the TOP plane
            x = mat.m30 - mat.m10;
            y = mat.m31 - mat.m11;
            z = mat.m32 - mat.m12;
            m_planes[3].normal = new Vector3(x, y, z);
            m_planes[3].distance = mat.m33 - mat.m13;
            // Extract the NEAR plane
            x = mat.m30 + mat.m20;
            y = mat.m31 + mat.m21;
            z = mat.m32 + mat.m22;
            m_planes[4].normal = new Vector3(x, y, z);
            m_planes[4].distance = mat.m33 + mat.m23;
            // Extract the FAR plane
            x = mat.m30 - mat.m20;
            y = mat.m31 - mat.m21;
            z = mat.m32 - mat.m22;
            m_planes[5].normal = new Vector3(x, y, z);
            m_planes[5].distance = mat.m33 - mat.m23;

   
        }

        void RenderHeightOverlays(RenderTexture target)
		{

			if(target == null) return;

            int pass = (HeightOverlayBlendMode == OVERLAY_BLEND_MODE.ADD) ? (int)OVERLAY_PASS.HEIGHT_ADD : (int)OVERLAY_PASS.HEIGHT_MAX;

            for (int i = 0; i < m_heightOverlays.Count; i++)
            {

                WaveOverlay overlay = m_heightOverlays[i];

                m_overlayMat.SetFloat("Ceto_Overlay_Alpha", overlay.HeightTex.alpha);
				m_overlayMat.SetFloat("Ceto_Overlay_MaskAlpha", Mathf.Max(0.0f, overlay.HeightTex.maskAlpha));
				m_overlayMat.SetTexture("Ceto_Overlay_Height", (overlay.HeightTex.tex != null) ? overlay.HeightTex.tex : Texture2D.blackTexture);
				m_overlayMat.SetTexture("Ceto_Overlay_HeightMask", (overlay.HeightTex.mask != null) ? overlay.HeightTex.mask : Texture2D.blackTexture);
				m_overlayMat.SetFloat("Ceto_Overlay_MaskMode", (float)overlay.HeightTex.maskMode);

				Blit(overlay.Corners, overlay.HeightTex.scaleUV, overlay.HeightTex.offsetUV, target, m_overlayMat, pass);

			}

		}

		void RenderNormalOverlays(RenderTexture target)
		{

			if(target == null) return;

            Matrix4x4 rotation = new Matrix4x4();

            for (int i = 0; i < m_normalOverlays.Count; i++)
            {

                WaveOverlay overlay = m_normalOverlays[i];

                RotateY(rotation, overlay.Rotation);

                m_overlayMat.SetMatrix("Ceto_Overlay_Rotation", rotation);
                m_overlayMat.SetFloat("Ceto_Overlay_Alpha", Mathf.Max(0.0f, overlay.NormalTex.alpha));
				m_overlayMat.SetFloat("Ceto_Overlay_MaskAlpha", Mathf.Max(0.0f, overlay.NormalTex.maskAlpha));
				m_overlayMat.SetTexture("Ceto_Overlay_Normal", (overlay.NormalTex.tex != null) ? overlay.NormalTex.tex : m_blankNormal);
				m_overlayMat.SetTexture("Ceto_Overlay_NormalMask", (overlay.NormalTex.mask != null) ? overlay.NormalTex.mask : Texture2D.blackTexture);
				m_overlayMat.SetFloat("Ceto_Overlay_MaskMode", (float)overlay.NormalTex.maskMode);

				Blit(overlay.Corners, overlay.NormalTex.scaleUV, overlay.NormalTex.offsetUV, target, m_overlayMat, (int)OVERLAY_PASS.NORMAL_ADD);
			}
		}

        void RotateY(Matrix4x4 m, float angle)
        {
            float ca = Mathf.Cos(angle * Mathf.PI / 180.0f);
            float sa = Mathf.Sin(angle * Mathf.PI / 180.0f);

            m.m00 = ca; m.m01 = 0; m.m02 = sa; m.m03 = 0;
            m.m10 = 0; m.m11 = 1; m.m12 = 0; m.m13 = 0;
            m.m20 = -sa; m.m21 = 0; m.m22 = ca; m.m23 = 1;
            m.m30 = 0; m.m31 = 0; m.m32 = 0; m.m33 = 0;
        }

        void RenderFoamOverlays(RenderTexture target)
		{

			if(target == null) return;

            int pass = (FoamOverlayBlendMode == OVERLAY_BLEND_MODE.ADD) ? (int)OVERLAY_PASS.FOAM_ADD : (int)OVERLAY_PASS.FOAM_MAX;

            for (int i = 0; i < m_foamOverlays.Count; i++)
            {

                WaveOverlay overlay = m_foamOverlays[i];

                m_overlayMat.SetFloat("Ceto_Overlay_Alpha", Mathf.Max(0.0f, overlay.FoamTex.alpha) * overlay.DistanceAlpha);
				m_overlayMat.SetFloat("Ceto_Overlay_MaskAlpha", Mathf.Max(0.0f, overlay.FoamTex.maskAlpha));
				m_overlayMat.SetTexture("Ceto_Overlay_Foam", (overlay.FoamTex.tex != null) ? overlay.FoamTex.tex : Texture2D.blackTexture);
				m_overlayMat.SetTexture("Ceto_Overlay_FoamMask", (overlay.FoamTex.mask != null) ? overlay.FoamTex.mask : Texture2D.blackTexture);
				m_overlayMat.SetFloat("Ceto_Overlay_MaskMode", (float)overlay.FoamTex.maskMode);
                m_overlayMat.SetVector("Ceto_TextureFoam", (overlay.FoamTex.textureFoam) ? TEXTURE_FOAM : DONT_TEXTURE_FOAM);
				
				Blit(overlay.Corners, overlay.FoamTex.scaleUV, overlay.FoamTex.offsetUV, target, m_overlayMat, pass);
			}
		}

		void RenderClipOverlays(RenderTexture target)
		{
			if(target == null) return;

            for (int i = 0; i < m_clipOverlays.Count; i++)
            {

                WaveOverlay overlay = m_clipOverlays[i];

                m_overlayMat.SetFloat("Ceto_Overlay_Alpha", Mathf.Max(0.0f, overlay.ClipTex.alpha));
				m_overlayMat.SetTexture("Ceto_Overlay_Clip", (overlay.ClipTex.tex != null) ? overlay.ClipTex.tex : Texture2D.blackTexture);
				
				Blit(overlay.Corners, overlay.ClipTex.scaleUV, overlay.ClipTex.offsetUV, target, m_overlayMat, (int)OVERLAY_PASS.CLIP_ADD);
			}
		}

		void Blit(Vector4[] corners, Vector2 scale, Vector2 offset, RenderTexture des, Material mat, int pass)
		{

			Graphics.SetRenderTarget(des);
			
			GL.PushMatrix();
			GL.LoadOrtho();
			
			mat.SetPass(pass);

			GL.Begin(GL.QUADS);

			GL.MultiTexCoord2(0, offset.x, offset.y); 
			GL.MultiTexCoord2(1, 0.0f, 0.0f); 
			GL.Vertex(corners[0]);

			GL.MultiTexCoord2(0, offset.x + 1.0f * scale.x, offset.y);
			GL.MultiTexCoord2(1, 1.0f, 0.0f); 
			GL.Vertex(corners[1]);

			GL.MultiTexCoord2(0, offset.x + 1.0f * scale.x, offset.y + 1.0f * scale.y); 
			GL.MultiTexCoord2(1, 1.0f, 1.0f); 
			GL.Vertex(corners[2]);

			GL.MultiTexCoord2(0, offset.x, offset.y + 1.0f * scale.y);
			GL.MultiTexCoord2(1, 0.0f, 1.0f); 
			GL.Vertex(corners[3]);
			GL.End();
			
			GL.PopMatrix();

		}

        /// <summary>
        /// Create the overlay buffers for this camera at the required size.
        /// </summary>
        public void CreateOverlays(Camera cam, WaveOverlayData overlay, OVERLAY_MAP_SIZE normalOverlaySize, OVERLAY_MAP_SIZE heightOverlaySize, OVERLAY_MAP_SIZE foamOverlaySize, OVERLAY_MAP_SIZE clipOverlaySize)
        {
            //If the manager has a overlay of this type then create the map it renders into
            if (HasNormalOverlay)
            {
                RenderTextureFormat format = RenderTextureFormat.ARGBHalf;

                //The mask will work but adding normals will not with this format. TODO - fix this
                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32; 

                CreateBuffer("Normal", cam, normalOverlaySize, format, true, ref overlay.normal);
            }

            if (HasHeightOverlay)
            {
                RenderTextureFormat format = RenderTextureFormat.RGHalf;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGBHalf;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;

                CreateBuffer("Height", cam, heightOverlaySize, format, true, ref overlay.height);
            }

            if (HasFoamOverlay)
            {
                RenderTextureFormat format = RenderTextureFormat.ARGB32;

                CreateBuffer("Foam", cam, foamOverlaySize, format, false, ref overlay.foam);
            }

            if (HasClipOverlay)
            {
                RenderTextureFormat format = RenderTextureFormat.R8;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.RHalf;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;

                CreateBuffer("Clip", cam, clipOverlaySize, format, true, ref overlay.clip);
            }
        }

        /// <summary>
        /// Create a overlay buffer with these settings.
        /// </summary>
        public void CreateBuffer(string name, Camera cam, OVERLAY_MAP_SIZE size, RenderTextureFormat format, bool isLinear, ref RenderTexture map)
        {

            float S = SizeToValue(size);

            int w = Mathf.Min(4096, (int)(cam.pixelWidth * S));
            int h = Mathf.Min(4096, (int)(cam.pixelHeight * S));

            if (map == null || map.width != w || map.height != h)
            {
                if (map != null)
                    RTUtility.ReleaseAndDestroy(map);

                RenderTextureReadWrite rw = (isLinear) ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default;

                map = new RenderTexture(w, h, 0, format, rw);
                map.useMipMap = false;
                map.filterMode = FilterMode.Bilinear;
                map.hideFlags = HideFlags.DontSave;
                map.name = "Ceto Overlay " + name + " Buffer: " + cam.name;
            }

        }

        /// <summary>
        /// Convert enum to a size.
        /// </summary>
		float SizeToValue(OVERLAY_MAP_SIZE size)
        {

            switch ((int)size)
            {

                case (int)OVERLAY_MAP_SIZE.DOUBLE:
                    return 2.0f;

                case (int)OVERLAY_MAP_SIZE.FULL_HALF:
                    return 1.5f;

                case (int)OVERLAY_MAP_SIZE.FULL:
                    return 1.0f;

                case (int)OVERLAY_MAP_SIZE.HALF:
                    return 0.5f;

                case (int)OVERLAY_MAP_SIZE.QUARTER:
                    return 0.25f;

            }

            return 1.0f;
        }


    }

}














