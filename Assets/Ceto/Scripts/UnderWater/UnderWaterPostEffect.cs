using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

namespace Ceto
{

	[AddComponentMenu("Ceto/Camera/UnderWaterPostEffect")]
	[RequireComponent (typeof(Camera))]
	public class UnderWaterPostEffect : MonoBehaviour 
	{

        /// <summary>
        /// This will disable the post effect if over a clip overlay.
        /// </summary>
		public bool disableOnClip = true;

        /// <summary>
        /// If true this will make this script set the underwater mode to
        /// ABOVE_ONLY or ABOVE_AND_BELOW depending if the post effect runs.
        /// This means the the under side mesh and mask will not run if not needed
        /// but you have to hand over control of that to this script.
        /// </summary>
        public bool controlUnderwaterMode = false;

        /// <summary>
        /// Multiple the under water color by sun.
        ///  So underwater fog goes dark at night.
        /// 0 is no attenuation and 1 is full.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float attenuationBySun = 0.8f;

		/// <summary>
		/// The blur mode. Down sampling is faster but will lose resolution.
		/// </summary>
		public ImageBlur.BLUR_MODE blurMode = ImageBlur.BLUR_MODE.OFF;

		/// Blur iterations - larger number means more blur.
		[Range(0, 4)]
		public int blurIterations = 3;
		
		/// Blur spread for each iteration. Lower values
		/// give better looking blur, but require more iterations to
		/// get large blurs. Value is usually between 0.5 and 1.0.
		[Range(0.5f, 1.0f)]
		/*public*/ float blurSpread = 0.6f;

		public Shader underWaterPostEffectSdr;

		[HideInInspector]
		public Shader blurShader;

		Material m_material;

		ImageBlur m_imageBlur;

        WaveQuery m_query;

        bool m_underWaterIsVisible;

		void Start () 
		{

			m_material = new Material(underWaterPostEffectSdr);

			m_imageBlur = new ImageBlur(blurShader);

            m_query = new WaveQuery();

            //Dont think you need to toggle depth mode
            //if image effect not using ImageEffectOpaque tag

            /*
            Camera cam = GetComponent<Camera>();

            //If rendering mode deferred and dx9 then toggling the depth
            //mode cause some strange issue with underwater effect
            //if using the opaque ocean materials.
            if (cam.actualRenderingPath == RenderingPath.Forward)
                GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
            */


        }

        void LateUpdate()
        {

            Camera cam = GetComponent<Camera>();

            m_underWaterIsVisible = UnderWaterIsVisible(cam);

            if (controlUnderwaterMode && Ocean.Instance != null && Ocean.Instance.UnderWater is UnderWater)
            {

                UnderWater underwater = Ocean.Instance.UnderWater as UnderWater;

                if (!m_underWaterIsVisible)
                    underwater.underwaterMode = UNDERWATER_MODE.ABOVE_ONLY;
                else
                    underwater.underwaterMode = UNDERWATER_MODE.ABOVE_AND_BELOW;

            }


        }

		//[ImageEffectOpaque]
		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{

            if (!ShouldRenderEffect())
            {
                Graphics.Blit(source, destination);
                return;
            }

            Camera cam = GetComponent<Camera>();

            float CAMERA_NEAR = cam.nearClipPlane;
            float CAMERA_FAR = cam.farClipPlane;
            float CAMERA_FOV = cam.fieldOfView;
            float CAMERA_ASPECT_RATIO = cam.aspect;
			
			Matrix4x4 frustumCorners = Matrix4x4.identity;		
			
			float fovWHalf = CAMERA_FOV * 0.5f;

            Vector3 toRight = cam.transform.right * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * CAMERA_ASPECT_RATIO;
            Vector3 toTop = cam.transform.up * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            Vector3 topLeft = (cam.transform.forward * CAMERA_NEAR - toRight + toTop);
			float CAMERA_SCALE = topLeft.magnitude * CAMERA_FAR/CAMERA_NEAR;	
			
			topLeft.Normalize();
			topLeft *= CAMERA_SCALE;

            Vector3 topRight = (cam.transform.forward * CAMERA_NEAR + toRight + toTop);
			topRight.Normalize();
			topRight *= CAMERA_SCALE;

            Vector3 bottomRight = (cam.transform.forward * CAMERA_NEAR + toRight - toTop);
			bottomRight.Normalize();
			bottomRight *= CAMERA_SCALE;

            Vector3 bottomLeft = (cam.transform.forward * CAMERA_NEAR - toRight - toTop);
			bottomLeft.Normalize();
			bottomLeft *= CAMERA_SCALE;
			
			frustumCorners.SetRow(0, topLeft); 
			frustumCorners.SetRow(1, topRight);		
			frustumCorners.SetRow(2, bottomRight);
			frustumCorners.SetRow(3, bottomLeft);

			m_material.SetMatrix ("_FrustumCorners", frustumCorners);

		    Color mulCol = Ocean.Instance.SunColor() * Mathf.Max(0.0f, Vector3.Dot(Vector3.up, Ocean.Instance.SunDir()));
            mulCol = Color.Lerp(Color.white, mulCol, attenuationBySun);

			m_material.SetColor("_MultiplyCol", mulCol);

			RenderTexture belowTex = RenderTexture.GetTemporary(source.width, source.height, 0);
			CustomGraphicsBlit(source, belowTex,  m_material, 0);

			m_imageBlur.BlurIterations = blurIterations;
			m_imageBlur.BlurMode = blurMode;
			m_imageBlur.BlurSpread = blurSpread;
			m_imageBlur.Blur(belowTex);

            m_material.SetTexture("_BelowTex", belowTex);
			Graphics.Blit(source, destination, m_material, 1);

			RenderTexture.ReleaseTemporary(belowTex);

		}

        bool ShouldRenderEffect()
        {

            if (underWaterPostEffectSdr == null || m_material == null || SystemInfo.graphicsShaderLevel < 30)
            {
                return false;
            }

            if (Ocean.Instance == null || Ocean.Instance.UnderWater == null || Ocean.Instance.Grid == null)
            {
                return false;
            }

            if (!Ocean.Instance.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!Ocean.Instance.UnderWater.enabled || !Ocean.Instance.Grid.enabled)
            {
                return false;
            }

            if (Ocean.Instance.UnderWater.underwaterMode == UNDERWATER_MODE.ABOVE_ONLY)
            {
                return false;
            }

            if (!m_underWaterIsVisible)
            {
                return false;
            }

            return true;

        }

        void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material mat, int pass) 
		{
			RenderTexture.active = dest;
			
			mat.SetTexture("_MainTex", source);	        
			
			GL.PushMatrix();
			GL.LoadOrtho();
			
			mat.SetPass (pass);	
			
			GL.Begin (GL.QUADS);
			
			//This custom blit is needed as information about what corner verts relate to what frustum corners is needed
			//A index to the frustum corner is store in the z pos of vert
			
			GL.MultiTexCoord2(0, 0.0f, 0.0f); 
			GL.Vertex3(0.0f, 0.0f, 3.0f); // BL
			
			GL.MultiTexCoord2(0, 1.0f, 0.0f); 
			GL.Vertex3(1.0f, 0.0f, 2.0f); // BR
			
			GL.MultiTexCoord2(0, 1.0f, 1.0f); 
			GL.Vertex3(1.0f, 1.0f, 1.0f); // TR
			
			GL.MultiTexCoord2(0, 0.0f, 1.0f); 
			GL.Vertex3(0.0f, 1.0f, 0.0f); // TL
			
			GL.End();
			GL.PopMatrix();
			
		}

        /// <summary>
        /// The near plane points of a frustum box.
        /// </summary>
        readonly static Vector4[] m_corners =
		{
			new Vector4(-1, -1, -1, 1), 
			new Vector4( 1, -1, -1, 1), 
			new Vector4( 1,  1, -1, 1),  
			new Vector4(-1,  1, -1, 1)
        };

        bool UnderWaterIsVisible(Camera cam)
        {

            if (Ocean.Instance == null) return false;

            Vector3 pos = cam.transform.position;

            if(disableOnClip)
            {
                m_query.posX = pos.x;
                m_query.posZ = pos.z;
                m_query.mode = QUERY_MODE.CLIP_TEST;

                Ocean.Instance.QueryWaves(m_query);

                if (m_query.result.isClipped)
                    return false;
            }

            float upperRange = Ocean.Instance.FindMaxDisplacement(true) + Ocean.Instance.level;

            if (pos.y < upperRange)
				return true;

            Matrix4x4 ivp = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;

            for (int i = 0; i < 4; i++)
            {
                Vector4 p = ivp * m_corners[i];
                p.y /= p.w;

                if (p.y < upperRange)
                {
                    return true;
                }
            }

            return false;

        }

    }

}
