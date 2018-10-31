#if !( UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 )
#define UNITY_540_OR_HIGHER
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

using Ceto.Common.Unity.Utility;

namespace Ceto
{

    /// <summary>
    /// Class to provide reflections using the planar reflection method.
    /// 
    /// Note - since this method presumes that the wave plane is flat having
    /// rough wave conditions can cause reflection artefacts. You can use the 
    /// reflection roughness to blur out the reflections to hide these artefacts.
    /// You wont have sharp reflections but thats the best that can be done with this method.
    /// 
    /// </summary>
	[AddComponentMenu("Ceto/Components/PlanarReflection")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Ocean))]
	public class PlanarReflection : OceanComponent
    {

		public const float MAX_REFLECTION_INTENSITY = 2.0f;
		public const float MAX_REFLECTION_DISORTION = 4.0f;

        /// <summary>
        /// The layers that will be rendered for the reflections.
        /// </summary>
		public LayerMask reflectionMask = 1;

        /// <summary>
        /// The resolution of the reflection texture relative to the screen.
        /// </summary>
        public REFLECTION_RESOLUTION reflectionResolution = REFLECTION_RESOLUTION.HALF;

        /// <summary>
        /// The relfections cameras offset from the clip plane.
        /// </summary>
        public float clipPlaneOffset = 0.07f;
        
        /// <summary>
        /// Should the reflection camera have fog enabled 
        /// when rendering.
        /// </summary>
        public bool fogInReflection = false;

        /// <summary>
        /// Should the skybox be reflected.
        /// </summary>
        public bool skyboxInReflection = true;

		/// <summary>
		/// If true will copy the cull distances from the camera.
		/// </summary>
		public bool copyCullDistances;

        /// <summary>
        /// The blur mode. Down sampling is faster but will lose resolution.
        /// </summary>
        public ImageBlur.BLUR_MODE blurMode = ImageBlur.BLUR_MODE.OFF;
		
		/// Blur iterations - larger number means more blur.
		[Range(0, 4)]
		public int blurIterations = 1;
		
		/// Blur spread for each iteration. Lower values
		/// give better looking blur, but require more iterations to
		/// get large blurs. Value is usually between 0.5 and 1.0.
		[Range(0.5f, 1.0f)]
		/*public*/ float blurSpread = 0.6f;

        /// <summary>
        /// Tints the reflection color.
        /// </summary>
		public Color reflectionTint = Color.white;

        /// <summary>
        /// Adjusts the reflection intensity.
        /// </summary>
		[Range(0.0f, MAX_REFLECTION_INTENSITY)]
		public float reflectionIntensity = 0.6f;

        /// <summary>
        /// Distorts the reflections based on the wave normal.
        /// </summary>
		[Range(0.0f, MAX_REFLECTION_DISORTION)]
        public float reflectionDistortion = 0.5f;

        /// <summary>
        /// Assign to provide your custom reflection rendering method.
        /// The function must have the following signature...
        /// 
        /// RenderTexture YourReflectionMethod(GameObject go);
        /// 
        /// The gameobjects transform contains the position of the 
        /// object requiring the reflections.
        /// 
        /// The returned render texture is the reflections rendered for the current camera.
        /// Ceto will not modify this texture or keep a reference to it.
        /// 
        /// </summary>
        public Func<GameObject, RenderTexture> RenderReflectionCustom;

        /// <summary>
        /// If a custom reflection method is provide this 
        /// is the game object passed. Its just a empty
        /// gameobject where the transform contains the
        /// reflections plane position.
        /// </summary>
		GameObject m_dummy;

		/// <summary>
		/// The used to blur the reflections.
		/// </summary>
		ImageBlur m_imageBlur;

		/// <summary>
		/// The blur shader.
		/// </summary>
		[HideInInspector]
		public Shader blurShader;

        void Start()
        {

            try
            {
                m_imageBlur = new ImageBlur(blurShader);
            }
            catch (Exception e)
            {
                Ocean.LogError(e.ToString());
                WasError = true;
                enabled = false;
            }

        }

        protected override void OnEnable()
		{
			base.OnEnable();
			
			Shader.EnableKeyword("CETO_REFLECTION_ON");
			
		}
		
		protected override void OnDisable()
		{

			base.OnDisable();

			Shader.DisableKeyword("CETO_REFLECTION_ON");
			
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            try
            {

                if(m_dummy != null)
                    DestroyImmediate(m_dummy);

            }
			catch(Exception e)
			{
                Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

}

        void Update()
		{

			reflectionIntensity = Mathf.Max(0.0f, reflectionIntensity);

			Shader.SetGlobalVector("Ceto_ReflectionTint", reflectionTint * reflectionIntensity); 
			Shader.SetGlobalFloat("Ceto_ReflectionDistortion", reflectionDistortion * 0.05f); 

		}

		/// <summary>
		/// Gets the reflection layer mask from the camera settings 
		/// if provided else use the default mask
		/// </summary>
		LayerMask GetReflectionLayermask(OceanCameraSettings settings)
		{
			return (settings != null) ? settings.reflectionMask : reflectionMask;
		}

		/// <summary>
		/// Gets if this camera should render the reflections.
		/// </summary>
		bool GetDisableReflections(OceanCameraSettings settings)
		{
			return (settings != null) ? settings.disableReflections : false;
		}

        /// <summary>
        /// Gets the reflection resolution from the camera settings 
        /// if provided else use the default resolution
        /// </summary>
        REFLECTION_RESOLUTION GetReflectionResolution(OceanCameraSettings settings)
        {
            return (settings != null) ? settings.reflectionResolution : reflectionResolution;
        }

        /// <summary>
        /// Render the reflections for this objects position
        /// and the current camera.
        /// </summary>
		public void RenderReflection(GameObject go)
		{

            try
            {

                if (!enabled) return;

                Camera cam = Camera.current;
                if (cam == null) return;

                CameraData data = m_ocean.FindCameraData(cam);

                //Create the data needed if not already created.
                if (data.reflection == null)
				{
                    data.reflection = new ReflectionData();
                }

                if (data.reflection.IsViewUpdated(cam)) return;

				//If this camera has disable the reflection turn it off in the shader and return.
				if(GetDisableReflections(data.settings))
				{
					Shader.DisableKeyword("CETO_REFLECTION_ON");
                    data.reflection.SetViewAsUpdated(cam);
                    return;
				}
				else
				{
					Shader.EnableKeyword("CETO_REFLECTION_ON");
				}

                RenderTexture reflections0 = null;
                RenderTexture reflections1 = null;

                if (data.reflection.cam != null)
                {
                    DisableFog disableFog = data.reflection.cam.GetComponent<DisableFog>();
                    if(disableFog != null) disableFog.enabled = !fogInReflection;
                }
				
				if(RenderReflectionCustom != null)
				{
                    //If using a custom method
                    //Destroy the camera if already created as its no longer needed.
                    if (data.reflection.cam != null)
                        data.reflection.DestroyTargets();

                    CreateRenderTarget(data.reflection, cam.pixelWidth, cam.pixelHeight, cam.allowHDR, false, data.settings);

                    //Create the dummy object if null
                    if (m_dummy == null)
					{
						m_dummy = new GameObject("Ceto Reflection Dummy Gameobject");
						m_dummy.hideFlags = HideFlags.HideAndDontSave;
					}
					
                    //Set the position of the reflection plane.
					m_dummy.transform.position = new Vector3(0.0f, m_ocean.level, 0.0f);
                    //Copy returned texture in target.
                    Graphics.Blit(RenderReflectionCustom(m_dummy), data.reflection.target0);
                    reflections0 = data.reflection.target0;
                    reflections1 = null; //Custom stero not supported.
				}
				else
				{
                    //Else use normal method.
                    CreateReflectionCameraFor(cam, data.reflection);
                    CreateRenderTarget(data.reflection, cam.pixelWidth, cam.pixelHeight, cam.allowHDR, cam.stereoEnabled, data.settings);

                    if(cam.stereoEnabled)
                    {
                        RenderSteroReflection(data.reflection, cam, data.settings);
                        reflections0 = data.reflection.target0;
                        reflections1 = data.reflection.target1;
                    }
                    else
                    {
                        Shader.DisableKeyword("CETO_STERO_CAMERA");
                        RenderReflection(data.reflection.cam, data.reflection.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix, data.settings);
                        reflections0 = data.reflection.target0;
                        reflections1 = null;
                    }

                }

                //The reflections texture should now contain the rendered 
                //reflections for the current cameras view.
				if(reflections0 != null)
				{
					m_imageBlur.BlurIterations = blurIterations;
					m_imageBlur.BlurMode = blurMode;
					m_imageBlur.BlurSpread = blurSpread;
					m_imageBlur.Blur(reflections0);

					Shader.SetGlobalTexture(Ocean.REFLECTION_TEXTURE_NAME0, reflections0);
				}

                if (reflections1 != null)
                {
                    m_imageBlur.BlurIterations = blurIterations;
                    m_imageBlur.BlurMode = blurMode;
                    m_imageBlur.BlurSpread = blurSpread;
                    m_imageBlur.Blur(reflections1);

                    Shader.SetGlobalTexture(Ocean.REFLECTION_TEXTURE_NAME1, reflections1);
                }

                data.reflection.SetViewAsUpdated(cam);

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

		}

        /// <summary>
        /// Render stero reflections for VR.
        /// </summary>
        void RenderSteroReflection(ReflectionData data, Camera cam, OceanCameraSettings settings)
        {

#if UNITY_540_OR_HIGHER && CETO_USE_STEAM_VR

            if (OceanVR.OpenVRInUse)
            {
                Shader.EnableKeyword("CETO_STERO_CAMERA");
                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                    OceanVR.GetSteamVRLeftEye(cam, out eyePos, out eyeRot, out projection);
                    RenderReflection(data.cam, data.target0, eyePos, eyeRot, projection, settings);
                }

                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                    OceanVR.GetSteamVRRightEye(cam, out eyePos, out eyeRot, out projection);
                    RenderReflection(data.cam, data.target1, eyePos, eyeRot, projection, settings);
                }
            }
            else
            {
                Shader.DisableKeyword("CETO_STERO_CAMERA");
                RenderReflection(data.cam, data.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix, settings);
            }
#else
            Shader.DisableKeyword("CETO_STERO_CAMERA");
            RenderReflection(data.cam, data.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix, settings);
#endif

        }

        /// <summary>
        /// Create the reflection camera for this camera.
        /// </summary>
        void CreateReflectionCameraFor(Camera cam, ReflectionData data)
        {

            if (data.cam == null)
			{
				GameObject go = new GameObject("Ceto Reflection Camera: " + cam.name);
				//go.AddComponent<IgnoreOceanEvents>();
				go.hideFlags = HideFlags.HideAndDontSave;

				DisableFog disableFog = go.AddComponent<DisableFog>();
	            disableFog.enabled = !fogInReflection;

	            data.cam = go.AddComponent<Camera>();

	            data.cam.depthTextureMode = DepthTextureMode.None;
	            data.cam.renderingPath = RenderingPath.Forward;
	            data.cam.enabled = false;
	            data.cam.allowHDR = cam.allowHDR;
	            data.cam.targetTexture = null;
				data.cam.useOcclusionCulling = false;

                //Copy the cull distances used by the camera.
                //Since the reflection camera uses a oblique projection matrix
                //the layer culling must be spherical or the cull wont match
                //that used by the camera. There will still be some mismatch 
                //between the reflection culling and camera culling if the 
                //camera does not use spherical culling.
                if (copyCullDistances)
                {
                    data.cam.layerCullDistances = cam.layerCullDistances;
                    data.cam.layerCullSpherical = true;
                }
            }

			data.cam.fieldOfView = cam.fieldOfView;
			data.cam.nearClipPlane = cam.nearClipPlane;
			data.cam.farClipPlane = cam.farClipPlane;
			data.cam.orthographic = cam.orthographic;
			data.cam.aspect = cam.aspect;
			data.cam.orthographicSize = cam.orthographicSize;
			data.cam.rect = new Rect(0, 0, 1, 1);
			data.cam.backgroundColor = m_ocean.defaultSkyColor;
			data.cam.clearFlags = (skyboxInReflection) ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;

        }

        /// <summary>
        /// Create the render targets for the reflection camera and the reflection texture.
        /// </summary>
		void CreateRenderTarget(ReflectionData data, int width, int height, bool isHdr, bool stero, OceanCameraSettings settings)
        {

            int scale = ResolutionToNumber(GetReflectionResolution(settings));
            width /= scale;
            height /= scale;

            //If the texture has been created and settings have not changed return
            if (data.target0 != null && data.target0.width == width && data.target0.height == height) return;

            RenderTextureFormat format;
            if ((isHdr || QualitySettings.activeColorSpace == ColorSpace.Linear) && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                format = RenderTextureFormat.ARGBHalf;
            else
                format = RenderTextureFormat.ARGB32;

            data.DestroyTargets();

            data.target0 = new RenderTexture(width, height, 16, format, RenderTextureReadWrite.Default);
            data.target0.filterMode = FilterMode.Bilinear;
            data.target0.wrapMode = TextureWrapMode.Clamp;
            data.target0.useMipMap = false;
            data.target0.anisoLevel = 0;
            data.target0.hideFlags = HideFlags.HideAndDontSave;
            data.target0.name = "Ceto Reflection Render Target0";

            if(stero)
            {
                data.target1 = new RenderTexture(width, height, 16, format, RenderTextureReadWrite.Default);
                data.target1.filterMode = FilterMode.Bilinear;
                data.target1.wrapMode = TextureWrapMode.Clamp;
                data.target1.useMipMap = false;
                data.target1.anisoLevel = 0;
                data.target1.hideFlags = HideFlags.HideAndDontSave;
                data.target1.name = "Ceto Reflection Render Target1";
            }

        }

        /// <summary>
        /// Convert the setting enum to a meaning full number.
        /// </summary>
        int ResolutionToNumber(REFLECTION_RESOLUTION resolution)
        {

            switch (resolution)
            {
                case REFLECTION_RESOLUTION.FULL:
                    return 1;

                case REFLECTION_RESOLUTION.HALF:
                    return 2;

                case REFLECTION_RESOLUTION.QUARTER:
                    return 4;

                default:
                    return 2;
            }

        }

        /// <summary>
        /// Render the reflections.
        /// </summary>
        void RenderReflection(Camera reflectionCam, RenderTexture target, Vector3 position, Quaternion rotation, Matrix4x4 projection, OceanCameraSettings settings)
        {

            // Copy camera position/rotation/projection data into the reflectionCamera
            reflectionCam.ResetWorldToCameraMatrix();
            reflectionCam.transform.position = position;
            reflectionCam.transform.rotation = rotation;
            reflectionCam.projectionMatrix = projection;
            reflectionCam.targetTexture = target;

            float level = m_ocean.level;
            // find out the reflection plane: position and normal in world space
            Vector3 pos = new Vector3(0, level, 0);
            Vector3 normal = Vector3.up;

            // Reflect camera around reflection plane
            Vector4 worldSpaceClipPlane = Plane(pos, normal);
            reflectionCam.worldToCameraMatrix *= CalculateReflectionMatrix(worldSpaceClipPlane);

            // Setup oblique projection matrix so that near plane is our reflection
            // plane. This way we clip everything behind it for free.
            Vector4 cameraSpaceClipPlane = CameraSpacePlane(reflectionCam, pos, normal);
            reflectionCam.projectionMatrix = reflectionCam.CalculateObliqueMatrix(cameraSpaceClipPlane);

            reflectionCam.cullingMask = GetReflectionLayermask(settings);
            reflectionCam.cullingMask = OceanUtility.HideLayer(reflectionCam.cullingMask, Ocean.OCEAN_LAYER);

            bool oldCulling = GL.invertCulling;
            GL.invertCulling = !oldCulling;

            NotifyOnEvent.Disable = true;
            reflectionCam.Render();
            NotifyOnEvent.Disable = false;

            GL.invertCulling = oldCulling;

            reflectionCam.targetTexture = null;

        }

        Vector4 Plane(Vector3 pos, Vector3 normal)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(pos, normal));
        }

        Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal)
        {
            Vector3 offsetPos = pos + normal * clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized;
            return Plane(cpos, cnormal);
        }

        Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 reflectionMat = Matrix4x4.identity;

            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }

    }
}

















