#if !( UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 )
#define UNITY_540_OR_HIGHER
#endif

using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

using Ceto.Common.Unity.Utility;

#pragma warning disable 649

namespace Ceto
{

    /// <summary>
    /// Handles the under water settings
    /// </summary>
	[AddComponentMenu("Ceto/Components/UnderWater")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Ocean))]
	public class UnderWater : OceanComponent
	{

        public const float MAX_REFRACTION_INTENSITY = 2.0f;
        public const float MAX_REFRACTION_DISORTION = 4.0f;

        /// <summary>
        /// Used as the value the ocean depths are normalized to.
        /// Dont change this.
        /// </summary>
        readonly float MAX_DEPTH_DIST = 500.0f;

        /// <summary>
        /// The under water mode.
        /// ABOVE - only renders the underwater effects on the top side mesh 
        /// ABOVE_AND_BELOW - render the underwater effects on the top mesh, the under
        /// side mesh and as a post effect if post effect script attached to camera.
        /// </summary>
		public UNDERWATER_MODE underwaterMode = UNDERWATER_MODE.ABOVE_ONLY;

        /// <summary>
        /// If 'USE_OCEAN_DEPTH_PASS' this will render a separate depth pass and use that information
        /// to apply the underwater effect. This means you will get more draw calls
        /// but allows the depth info to be accessible when it otherwise would not be. 
        /// 
        /// If 'USE_DEPTH_BUFFER' the depth data for the underwater effect will come 
        /// from a copy of the depth buffer. This is faster to do but only works if
        /// the ocean is in the transparent queue as the depths need to be copied from
        /// the _CameraDepthTexture using a command buffer at the AfterSkyBox event.
        /// The reason a copy is needed is because if sampling from the depth buffer and 
        /// writing to it in certain set ups (dx9/Deferred) this does not work correctly on
        /// certain graphics cards (Nivida).
        /// </summary>
        public DEPTH_MODE depthMode = DEPTH_MODE.USE_OCEAN_DEPTH_PASS;
 
        /// <summary>
        /// The layers that will be rendered in the ocean depth buffer.
        /// This is only used if the depth mode is USE_OCEAN_DEPTH_PASS.
        /// </summary>
        public LayerMask oceanDepthsMask = 1;

        /// <summary>
        /// The depth the bottom mesh goes to.
        /// It is recommended to have this be
        /// no larger than half the far plane value.
        /// </summary>
        public float bottomDepth = 500.0f;

        /// <summary>
        /// The underwater effect can be applied using the objects world y value or its
        /// camera z value. This will just lerp between the to methods. 0 is full 'by world y'
        /// and 1 is full 'by camera z'. 
        /// </summary>
        [Range(0.0f, 1.0f)]
		/*public*/ float depthBlend = 0.2f;

        /// <summary>
        /// Amount of fade applied where the ocean meets other objects in scene.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float edgeFade = 0.2f;

        /// <summary>
        /// Modify the intensity of the refraction grab color.
        /// </summary>
        [Range(0.0f, MAX_REFRACTION_INTENSITY)]
        public float aboveRefractionIntensity = 0.5f;
        [Range(0.0f, MAX_REFRACTION_INTENSITY)]
        public float belowRefractionIntensity = 0.5f;

        /// <summary>
        /// The strength of the distortion applied to the refraction uvs.
        /// </summary>
		[Range(0.0f, MAX_REFRACTION_DISORTION)]
        public float refractionDistortion = 0.5f;

        /// <summary>
        /// The absorption cof for red light.
        /// a higher value means more red light is lost 
        /// as the light travels through the water.
        /// </summary>
        [Range(0.0f, 1.0f)]
		public float absorptionR = 0.45f;

        /// <summary>
        /// The absorption cof for green light.
        /// a higher value means more green light is lost 
        /// as the light travels through the water.
        /// </summary>
		[Range(0.0f, 1.0f)]
		public float absorptionG = 0.029f;

        /// <summary>
        /// The absorption cof for blue light.
        /// a higher value means more blue light is lost 
        /// as the light travels through the water.
        /// </summary>
		[Range(0.0f, 1.0f)]
		public float absorptionB = 0.018f;

        /// <summary>
        /// Modify the result of applying the absorption cof when above the mesh looking down.
        /// </summary>
		public AbsorptionModifier aboveAbsorptionModifier 
			= new AbsorptionModifier(2.0f, 1.0f, Color.white);

        /// <summary>
        /// Modify the result of applying the absorption cof when below the mesh looking down.
        /// </summary>
        public AbsorptionModifier belowAbsorptionModifier
            = new AbsorptionModifier(0.1f, 1.0f, Color.white);

        /// <summary>
        /// Modify the result of applying the absorption cof to the subsurface scatter.
        /// </summary>
		public AbsorptionModifier subSurfaceScatterModifier 
			= new AbsorptionModifier(10.0f, 1.5f, new Color32(220, 250, 180, 255));

        /// <summary>
        /// Modify the inscatter when above the mesh looking down.
        /// </summary>
		public InscatterModifier aboveInscatterModifier 
			= new InscatterModifier(50.0f, 1.0f, new Color32(2, 25, 43, 255), INSCATTER_MODE.EXP);

        /// <summary>
        /// Modify the inscatter when below the mesh looking up.
        /// </summary>
		public InscatterModifier belowInscatterModifier 
			= new InscatterModifier(60.0f, 1.0f, new Color32(7, 51, 77, 255), INSCATTER_MODE.EXP);

        /// <summary>
        /// Modifies how the caustics are applied.
        /// </summary>
        public CausticModifier causticModifier
            = new CausticModifier(0.25f, 0.25f, 0.1f, 0.75f, Color.white);

        /// <summary>
        /// The caustic texture.
        /// </summary>
        public CausticTexture causticTexture;

        /// <summary>
        /// Disables the copy depth command.
        /// Use this if providing your own depth buffer grab.
        /// </summary>
        public bool DisableCopyDepthCmd { get; set; }

        /// <summary>
        /// Disables the normal fade command.
        /// Used for the caustics.
        /// </summary>
        public bool DisableNormalFadeCmd { get; set; }

        /// <summary>
        /// The bottom mesh that surrounds the player under the water.
		/// Used to render the correct info for the background.
        /// </summary>
		GameObject m_bottomMask;

        /// <summary>
        /// 
        /// </summary>
		[HideInInspector]
        public Shader oceanBottomSdr, oceanDepthSdr, copyDepthSdr;

		[HideInInspector]
		public Shader oceanMaskSdr, oceanMaskFlippedSdr, normalFadeSdr;

		void Start () 
		{

			try
			{

                Mesh mesh = CreateBottomMesh(32, 512);

				//The bottom used to render the masks.
				m_bottomMask = new GameObject("Ceto Bottom Mask Gameobject");

				MeshFilter filter = m_bottomMask.AddComponent<MeshFilter>();
				MeshRenderer renderer = m_bottomMask.AddComponent<MeshRenderer>();
				NotifyOnWillRender willRender = m_bottomMask.AddComponent<NotifyOnWillRender>();

				filter.sharedMesh = mesh;
				renderer.receiveShadows = false;
				renderer.shadowCastingMode = ShadowCastingMode.Off;
				renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
				renderer.material = new Material(oceanBottomSdr);

                willRender.AddAction(m_ocean.RenderWaveOverlays);
                willRender.AddAction(m_ocean.RenderOceanMask);
				willRender.AddAction(m_ocean.RenderOceanDepth);

				m_bottomMask.layer = LayerMask.NameToLayer(Ocean.OCEAN_LAYER);
				m_bottomMask.hideFlags = HideFlags.HideAndDontSave;
            
				UpdateBottomBounds();

                Destroy(mesh);

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

		
		}

		protected override void OnEnable()
		{

			base.OnEnable();

            try
            {

                Shader.EnableKeyword("CETO_UNDERWATER_ON");

                SetBottomActive(m_bottomMask, true);
            }
            catch (Exception e)
            {
                Ocean.LogError(e.ToString());
                WasError = true;
                enabled = false;
            }
        }
		
		protected override void OnDisable()
		{

			base.OnDisable();

            try
            {
                Shader.DisableKeyword("CETO_UNDERWATER_ON");

                SetBottomActive(m_bottomMask, false);
            }
            catch (Exception e)
            {
                Ocean.LogError(e.ToString());
                WasError = true;
                enabled = false;
            }

        }

        protected override void OnDestroy()
        {

            base.OnDestroy();

            try
            {

                if (m_bottomMask != null)
                {
                    Mesh mesh = m_bottomMask.GetComponent<MeshFilter>().mesh;
                    Destroy(m_bottomMask);
                    Destroy(mesh);
                }

            }
			catch(Exception e)
			{
                Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}


        }

        void Update () 
		{

			try
			{

#if UNITY_WEBGL
                //There is a issue with the webGL projection matrix in the build when converting the
                //depth to world position. Have to use the depth pass instead.
                if (depthMode == DEPTH_MODE.USE_DEPTH_BUFFER)
                {
                    Ocean.LogWarning("Underwater depth mode for WebGL can not be USE_DEPTH_BUFFER. Changing to USE_OCEAN_DEPTH_PASS");
                    depthMode = DEPTH_MODE.USE_OCEAN_DEPTH_PASS;
                }
#endif

                Vector4 absCof = new Vector4(absorptionR, absorptionG, absorptionB, 1.0f);
				Vector4 sssCof = absCof;
				Vector4 belowCof = absCof;

                absCof.w = Mathf.Max(0.0f, aboveAbsorptionModifier.scale);
				sssCof.w = Mathf.Max(0.0f, subSurfaceScatterModifier.scale);
				belowCof.w = Mathf.Max(0.0f, belowAbsorptionModifier.scale);

				Color absTint = aboveAbsorptionModifier.tint * Mathf.Max(0.0f, aboveAbsorptionModifier.intensity);
				Color sssTint = subSurfaceScatterModifier.tint * Mathf.Max(0.0f, subSurfaceScatterModifier.intensity); 
				Color belowTint = belowAbsorptionModifier.tint * Mathf.Max(0.0f, belowAbsorptionModifier.intensity);

                Vector4 causticParam = new Vector4();
                causticParam.x = (causticTexture.scale.x != 0.0f) ? 1.0f / causticTexture.scale.x : 1.0f;
                causticParam.y = (causticTexture.scale.y != 0.0f) ? 1.0f / causticTexture.scale.y : 1.0f;
                causticParam.z = 0.0f;
                causticParam.w = Mathf.Clamp01(causticModifier.depthFade);

                Vector2 causticDistortion = new Vector2();
                causticDistortion.x = causticModifier.aboveDistortion;
                causticDistortion.y = causticModifier.belowDistortion;

                Shader.SetGlobalVector("Ceto_AbsCof", absCof);
				Shader.SetGlobalColor("Ceto_AbsTint", absTint);

				Shader.SetGlobalVector("Ceto_SSSCof", sssCof);
				Shader.SetGlobalColor("Ceto_SSSTint", sssTint);

				Shader.SetGlobalVector("Ceto_BelowCof", belowCof);
				Shader.SetGlobalColor("Ceto_BelowTint", belowTint);

				Color aboveInscatterCol = aboveInscatterModifier.color;
				aboveInscatterCol.a = Mathf.Clamp01(aboveInscatterModifier.intensity);

				Shader.SetGlobalFloat("Ceto_AboveInscatterScale", Mathf.Max(0.1f, aboveInscatterModifier.scale));
				Shader.SetGlobalVector("Ceto_AboveInscatterMode", InscatterModeToMask(aboveInscatterModifier.mode));
				Shader.SetGlobalColor("Ceto_AboveInscatterColor", aboveInscatterCol);

				Color belowInscatterCol = belowInscatterModifier.color;
				belowInscatterCol.a = Mathf.Clamp01(belowInscatterModifier.intensity);
				
				Shader.SetGlobalFloat("Ceto_BelowInscatterScale", Mathf.Max(0.1f, belowInscatterModifier.scale));
				Shader.SetGlobalVector("Ceto_BelowInscatterMode", InscatterModeToMask(belowInscatterModifier.mode));
				Shader.SetGlobalColor("Ceto_BelowInscatterColor", belowInscatterCol);

				Shader.SetGlobalFloat("Ceto_AboveRefractionIntensity", Mathf.Max(0.0f, aboveRefractionIntensity));
                Shader.SetGlobalFloat("Ceto_BelowRefractionIntensity", Mathf.Max(0.0f, belowRefractionIntensity));
                Shader.SetGlobalFloat("Ceto_RefractionDistortion", refractionDistortion * 0.05f);
				Shader.SetGlobalFloat("Ceto_MaxDepthDist", Mathf.Max(0.0f, MAX_DEPTH_DIST));
				Shader.SetGlobalFloat("Ceto_DepthBlend", Mathf.Clamp01(depthBlend));
                Shader.SetGlobalFloat("Ceto_EdgeFade", Mathf.Lerp(20.0f, 2.0f, Mathf.Clamp01(edgeFade)));

                Shader.SetGlobalTexture("Ceto_CausticTexture", ((causticTexture.tex != null) ? causticTexture.tex : Texture2D.blackTexture));
                Shader.SetGlobalVector("Ceto_CausticTextureScale", causticParam);
                Shader.SetGlobalVector("Ceto_CausticDistortion", causticDistortion);
                Shader.SetGlobalColor("Ceto_CausticTint", causticModifier.tint * causticModifier.intensity);

                if (depthMode == DEPTH_MODE.USE_OCEAN_DEPTH_PASS)
				{
					Shader.EnableKeyword("CETO_USE_OCEAN_DEPTHS_BUFFER");

					if(underwaterMode == UNDERWATER_MODE.ABOVE_ONLY)
					{
						SetBottomActive(m_bottomMask, false);
					}
					else			
					{
						SetBottomActive(m_bottomMask, true);
						UpdateBottomBounds();
					}
				}
				else
				{
					Shader.DisableKeyword("CETO_USE_OCEAN_DEPTHS_BUFFER");

					if(underwaterMode == UNDERWATER_MODE.ABOVE_ONLY)
					{
						SetBottomActive(m_bottomMask, false);
					}
					else			
					{
						SetBottomActive(m_bottomMask, true);
						UpdateBottomBounds();
					}
				}


			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
		}

		void SetBottomActive(GameObject bottom, bool active)
		{
			if(bottom != null)
				bottom.SetActive(active);
		}

        /// <summary>
        /// Convert the inscatter mode enum to a vector mask for the shader.
        /// </summary>
		Vector3 InscatterModeToMask(INSCATTER_MODE mode)
		{
			switch(mode)
			{
			case INSCATTER_MODE.LINEAR:
				return new Vector3(1,0,0);

			case INSCATTER_MODE.EXP:
				return new Vector3(0,1,0);

			case INSCATTER_MODE.EXP2:
				return new Vector3(0,0,1);

			default:
				return new Vector3(0,0,1);
			}

		}

        /// <summary>
        /// Moves the bottom mesh to where the camera is.
        /// </summary>
		void FitBottomToCamera()
		{

			if(!enabled || m_bottomMask == null) return;

			Camera cam = Camera.current;

			Vector3 pos = cam.transform.position;
			//Scale must be greater than the fade used in the fade dist used
			//in the OceanDisplacement.cginc OceanPositionAndDisplacement function
			float far = cam.farClipPlane * 0.85f;

            m_bottomMask.transform.localScale = new Vector3(far, bottomDepth, far);

			float depthOffset = 0.0f;
            m_bottomMask.transform.localPosition = new Vector3(pos.x, -bottomDepth + m_ocean.level - depthOffset, pos.z);

		}

		/// <summary>
		/// Need to make bounds big enough that every camera will
		/// render it. The position of the bottom will then be fitted 
		/// to the cameras position on render.
		/// </summary>
		void UpdateBottomBounds()
		{

			float bigNumber = 1e8f;
			float level = m_ocean.level;

			if(m_bottomMask != null && m_bottomMask.activeSelf)
			{
                Bounds bounds = new Bounds(new Vector3(0.0f, level, 0.0f), new Vector3(bigNumber, bottomDepth, bigNumber));

                m_bottomMask.GetComponent<MeshFilter>().mesh.bounds = bounds;
			}

		}

        /// <summary>
        /// Gets the reflection layer mask from the camera settings 
        /// if provided else use the default mask
        /// </summary>
        LayerMask GetOceanDepthsLayermask(OceanCameraSettings settings)
        {
            return (settings != null) ? settings.oceanDepthsMask : oceanDepthsMask;
        }

        /// <summary>
        /// Gets if this camera should render the reflections.
        /// </summary>
        bool GetDisableUnderwater(OceanCameraSettings settings)
        {
            return (settings != null) ? settings.disableUnderwater : false;
        }

        /// <summary>
        /// Renders the ocean mask. The mask is used in the underwater post effect
        /// shader and contains a 1 or 0 in the rgb channels if the pixel is on the
        /// top of the ocean mesh, on the under side of mesh or below the ocean mesh.
        /// The w channel also contains the meshes depth value as if the normal ocean
        /// material does not write to depth buffer the shader wont be able to get its
        /// depth value.
        /// </summary>
		public void RenderOceanMask(GameObject go)
        {

			try
			{

	            if (!enabled) return;

	            if (oceanMaskSdr == null) return;

	            if (underwaterMode == UNDERWATER_MODE.ABOVE_ONLY) return;

	            Camera cam = Camera.current;
                if (cam == null) return;

                CameraData data = m_ocean.FindCameraData(cam);

                if (data.mask == null)
	                data.mask = new MaskData();

	            if (data.mask.IsViewUpdated(cam)) return;
                
                if (cam.name == "SceneCamera" || cam.GetComponent<UnderWaterPostEffect>() == null || SystemInfo.graphicsShaderLevel < 30 || GetDisableUnderwater(data.settings))
                {
                    //Scene camera should never need the mask so just bind something that wont cause a issue.
                    //If the camera is not using a post effect there is no need for the mask to be rendered.

                    Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME0, Texture2D.blackTexture);
                    Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME1, Texture2D.blackTexture);
                    data.mask.SetViewAsUpdated(cam);
                }
                else
                {

                    CreateMaskCameraFor(cam, data.mask);

                    FitBottomToCamera();

                    Shader sdr = (m_ocean.Projection.IsFlipped) ? oceanMaskFlippedSdr : oceanMaskSdr;

                    if (cam.stereoEnabled)
                        RenderSteroOceanMask(data.mask, cam, sdr);
                    else
                    {
                        Shader.DisableKeyword("CETO_STERO_CAMERA");
                        RenderOceanMask(data.mask, data.mask.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix, sdr);
                        Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME0, data.mask.target0);
                    }

                    data.mask.SetViewAsUpdated(cam);
                }

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
        }

        /// <summary>
        /// Render stero mask for VR.
        /// The mask needs to be rendered twice (once for each eye).
        /// </summary>
        void RenderSteroOceanMask(MaskData data, Camera cam, Shader sdr)
        {
#if UNITY_540_OR_HIGHER && CETO_USE_STEAM_VR
            if (OceanVR.OpenVRInUse)
            {
                Shader.EnableKeyword("CETO_STERO_CAMERA");
                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                    OceanVR.GetSteamVRLeftEye(cam, out eyePos, out eyeRot, out projection);
                    RenderOceanMask(data, data.target0, eyePos, eyeRot, projection, sdr);
                    Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME0, data.target0);
                }

                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                    OceanVR.GetSteamVRRightEye(cam, out eyePos, out eyeRot, out projection);
                    RenderOceanMask(data, data.target1, eyePos, eyeRot, projection, sdr);
                    Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME1, data.target1);
                }
            }
            else
            {
                Shader.DisableKeyword("CETO_STERO_CAMERA");
                RenderOceanMask(data, data.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix, sdr);
                Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME0, data.target0);
            }
#else
            Shader.DisableKeyword("CETO_STERO_CAMERA");
            RenderOceanMask(data, data.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix, sdr);
            Shader.SetGlobalTexture(Ocean.OCEAN_MASK_TEXTURE_NAME0, data.target0);
#endif
        }

        /// <summary>
        /// Render the ocean mask pass into the provided target
        /// at the provided camera position.
        /// </summary>
        void RenderOceanMask(MaskData data, RenderTexture target, Vector3 position, Quaternion rotation, Matrix4x4 projection, Shader sdr)
        {
            NotifyOnEvent.Disable = true;

            data.cam.ResetWorldToCameraMatrix();
            data.cam.transform.position = position;
            data.cam.transform.rotation = rotation;
            data.cam.projectionMatrix = projection;

            data.cam.targetTexture = target;
            data.cam.RenderWithShader(sdr, "OceanMask");
            data.cam.targetTexture = null;

            NotifyOnEvent.Disable = false;
        }

        /// <summary>
        /// Render depth information about a object using a replacement shader.
        /// If the ocean renders into the depth buffer then the shader can not get 
        /// depth info about what has been rendered under it as Unity will have already 
        /// written the ocean mesh into depth buffer by then.
        /// Will also create the refraction grab if needed.
        /// </summary>
		public void RenderOceanDepth(GameObject go)
        {

			try
			{

                if (!enabled) return;

                Camera cam = Camera.current;
                if (cam == null) return;

                CameraData data = m_ocean.FindCameraData(cam);

                if (data.depth == null)
                    data.depth = new DepthData();

                if (data.depth.IsViewUpdated(cam)) return;

                //If this camera has disable the underwater turn it off in the shader and return.
                if (GetDisableUnderwater(data.settings))
                {
                    Shader.DisableKeyword("CETO_UNDERWATER_ON");
                    data.depth.SetViewAsUpdated(cam);
                    return;
                }
                else
                {
                    Shader.EnableKeyword("CETO_UNDERWATER_ON");
                }
                
                if (/*cam.name == "SceneCamera" ||*/ SystemInfo.graphicsShaderLevel < 30)
                {
                    //These texture will not be generated so zero to some that will not cause a issue if sampled.
                    Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME0, Texture2D.whiteTexture);
                    Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME1, Texture2D.whiteTexture);
                    Shader.SetGlobalTexture(Ocean.DEPTH_GRAB_TEXTURE_NAME, Texture2D.whiteTexture);
                    Shader.SetGlobalTexture(Ocean.NORMAL_FADE_TEXTURE_NAME, Texture2D.blackTexture);

                    //If not using the ocean depths all thats needed is the IVP
                    //to extract the world pos from the depth buffer.
                    BindIVPMatrix(cam.projectionMatrix, cam.worldToCameraMatrix);

                    data.depth.SetViewAsUpdated(cam);
                }
                else if (depthMode == DEPTH_MODE.USE_DEPTH_BUFFER)
                {
                    //These texture will not be generated so zero to some that will not cause a issue if sampled.
                    Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME0, Texture2D.whiteTexture);
                    Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME1, Texture2D.whiteTexture);

                    //Cam must have depth mode enabled to use depth buffer.
                    cam.depthTextureMode |= DepthTextureMode.Depth;
                    cam.depthTextureMode |= DepthTextureMode.DepthNormals;

                    CreateRefractionCommand(cam, data.depth);

                    //If not using the ocean depths all thats needed is the IVP
                    //to extract the world pos from the depth buffer.
                    BindIVPMatrix(cam.projectionMatrix, cam.worldToCameraMatrix);

                    data.depth.SetViewAsUpdated(cam);
                }
                else if (depthMode == DEPTH_MODE.USE_OCEAN_DEPTH_PASS)
                {
                    //These texture will not be generated so zero to some that will not cause a issue if sampled.
                    Shader.SetGlobalTexture(Ocean.DEPTH_GRAB_TEXTURE_NAME, Texture2D.whiteTexture);
                    Shader.SetGlobalTexture(Ocean.NORMAL_FADE_TEXTURE_NAME, Texture2D.blackTexture);

                    CreateDepthCameraFor(cam, data.depth);
                    CreateRefractionCommand(cam, data.depth);

                    data.depth.cam.cullingMask = GetOceanDepthsLayermask(data.settings);
                    data.depth.cam.cullingMask = OceanUtility.HideLayer(data.depth.cam.cullingMask, Ocean.OCEAN_LAYER);

                    if (cam.stereoEnabled)
                        RenderSteroOceanDepth(data.depth, cam);
                    else
                    {
                        Shader.DisableKeyword("CETO_STERO_CAMERA");
                        RenderOceanDepth(data.depth, data.depth.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix);
                        Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME0, data.depth.target0);
                    }
                    
                    data.depth.SetViewAsUpdated(cam);
                }

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
        }

        /*
        /// <summary>
        /// Bind The inverse vp matrix for stero camera.
        /// Used to convert the depth to world position
        /// </summary>
        void BindSteroIVPMatrix(DepthData data, Camera cam)
        {
            //The USE_DEPTH_BUFFER depth mode is not working in stero VR.
            //The projection and world matrix would need to be per eye.
            //The depth buffer is grabed using a command buffer its unclear how to
            //do that per eye since the command is added to a single camera.

            Shader.SetGlobalFloat("Ceto_Stero_Enabled", 1.0f);

            if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
            {
                Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                OceanVR.GetSteamVRLeftEye(cam, out eyePos, out eyeRot, out projection);

                data.cam.ResetWorldToCameraMatrix();
                data.cam.transform.position = eyePos;
                data.cam.transform.rotation = eyeRot;

                Matrix4x4 ivp = projection * data.cam.worldToCameraMatrix;
                Shader.SetGlobalMatrix("Ceto_Camera_IVP0", ivp.inverse);
            }

            if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
            {
                Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                OceanVR.GetSteamVRRightEye(cam, out eyePos, out eyeRot, out projection);

                data.cam.ResetWorldToCameraMatrix();
                data.cam.transform.position = eyePos;
                data.cam.transform.rotation = eyeRot;

                Matrix4x4 ivp = projection * data.cam.worldToCameraMatrix;
                Shader.SetGlobalMatrix("Ceto_Camera_IVP1", ivp.inverse);
            }
            
        }
        */

        /// <summary>
        /// Bind The inverse vp matrix.
        /// Used to convert the depth to world position
        /// </summary>
        void BindIVPMatrix(Matrix4x4 projection, Matrix4x4 view)
        {
            Matrix4x4 ivp = projection * view;
            Shader.SetGlobalMatrix("Ceto_Camera_IVP0", ivp.inverse);
        }

        /// <summary>
        /// Render stero depth pass for VR.
        /// The depths need to be rendered twice (once for each eye).
        /// </summary>
        void RenderSteroOceanDepth(DepthData data, Camera cam)
        {

#if UNITY_540_OR_HIGHER && CETO_USE_STEAM_VR
            if (OceanVR.OpenVRInUse)
            {
                Shader.EnableKeyword("CETO_STERO_CAMERA");
                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                    OceanVR.GetSteamVRLeftEye(cam, out eyePos, out eyeRot, out projection);
                    RenderOceanDepth(data, data.target0, eyePos, eyeRot, projection);
                    Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME0, data.target0);
                }

                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    Vector3 eyePos; Quaternion eyeRot; Matrix4x4 projection;
                    OceanVR.GetSteamVRRightEye(cam, out eyePos, out eyeRot, out projection);
                    RenderOceanDepth(data, data.target1, eyePos, eyeRot, projection);
                    Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME1, data.target1);
                }
            }
            else
            {
                Shader.DisableKeyword("CETO_STERO_CAMERA");
                RenderOceanDepth(data, data.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix);
                Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME0, data.target0);
            }
#else
            Shader.DisableKeyword("CETO_STERO_CAMERA");
            RenderOceanDepth(data, data.target0, cam.transform.position, cam.transform.rotation, cam.projectionMatrix);
            Shader.SetGlobalTexture(Ocean.OCEAN_DEPTH_TEXTURE_NAME0, data.target0);
#endif

        }

        /// <summary>
        /// Render the ocean depth pass into the provided target
        /// at the provided camera position.
        /// </summary>
        void RenderOceanDepth(DepthData data, RenderTexture target, Vector3 position, Quaternion rotation, Matrix4x4 projection)
        {
            NotifyOnEvent.Disable = true;

            data.cam.ResetWorldToCameraMatrix();
            data.cam.transform.position = position;
            data.cam.transform.rotation = rotation;
            data.cam.projectionMatrix = projection;

            data.cam.targetTexture = target;
            data.cam.RenderWithShader(oceanDepthSdr, "RenderType");
            data.cam.targetTexture = null;
   
            NotifyOnEvent.Disable = false;
        }

        /// <summary>
        /// Create the camera used for the mask.
        /// If it has already been created then just update it.
        /// Also create the render targets for the camera.
        /// </summary>
        void CreateMaskCameraFor(Camera cam, MaskData data)
        {

            if (data.cam == null)
            {

                GameObject go = new GameObject("Ceto Mask Camera: " + cam.name);
                go.hideFlags = HideFlags.HideAndDontSave;
				go.AddComponent<IgnoreOceanEvents>();
				go.AddComponent<DisableFog>();
				go.AddComponent<DisableShadows>();

                data.cam = go.AddComponent<Camera>();

                data.cam.clearFlags = CameraClearFlags.SolidColor;
                data.cam.backgroundColor = new Color(0,1,0,0); //Need to clear r to 0 and g to 1
                data.cam.cullingMask = 1 << LayerMask.NameToLayer(Ocean.OCEAN_LAYER);
                data.cam.enabled = false;
                data.cam.renderingPath = RenderingPath.Forward;
                data.cam.targetTexture = null;
				data.cam.useOcclusionCulling = false;
                data.cam.RemoveAllCommandBuffers();
                data.cam.targetTexture = null;

            }

            //Note - position rotation and projection set before rendering.
            //Update other settings here.
            data.cam.fieldOfView = cam.fieldOfView;
            data.cam.nearClipPlane = cam.nearClipPlane;
            data.cam.farClipPlane = cam.farClipPlane;
            data.cam.orthographic = cam.orthographic;
            data.cam.aspect = cam.aspect;
            data.cam.orthographicSize = cam.orthographicSize;
            data.cam.rect = new Rect(0, 0, 1, 1);

            if (data.target0 == null || data.target0.width != cam.pixelWidth || data.target0.height != cam.pixelHeight)
            {

                data.DestroyTargets();

                int width = cam.pixelWidth;
                int height = cam.pixelHeight;
                int depth = 32;

                RenderTextureFormat format = RenderTextureFormat.RGHalf;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGBHalf;

                data.target0 = new RenderTexture(width, height, depth, format, RenderTextureReadWrite.Linear);
                data.target0.filterMode = FilterMode.Point;
                data.target0.hideFlags = HideFlags.DontSave;
                data.target0.name = "Ceto Mask Render Target0: " + cam.name;

                if(cam.stereoEnabled)
                {
                    data.target1 = new RenderTexture(width, height, depth, format, RenderTextureReadWrite.Linear);
                    data.target1.filterMode = FilterMode.Point;
                    data.target1.hideFlags = HideFlags.DontSave;
                    data.target1.name = "Ceto Mask Render Target1: " + cam.name;
                }

            }

        }

        /// <summary>
        /// Create the camera used for the ocean depths
        /// If it has already been created then just update it.
        /// Also create the render targets for the camera.
        /// </summary>
        void CreateDepthCameraFor(Camera cam, DepthData data)
        {

            if (data.cam == null)
            {

                GameObject go = new GameObject("Ceto Depth Camera: " + cam.name);
                go.hideFlags = HideFlags.HideAndDontSave;
				go.AddComponent<IgnoreOceanEvents>();
				go.AddComponent<DisableFog>();
				go.AddComponent<DisableShadows>();

                data.cam = go.AddComponent<Camera>();

                data.cam.clearFlags = CameraClearFlags.SolidColor;
                data.cam.backgroundColor = Color.white;
                data.cam.enabled = false;
                data.cam.renderingPath = RenderingPath.Forward;
                data.cam.targetTexture = null;
				data.cam.useOcclusionCulling = false;
                data.cam.RemoveAllCommandBuffers();
                data.cam.targetTexture = null;

            }

            //Note - position rotation and projection set before rendering.
            //Update other settings here.
            data.cam.fieldOfView = cam.fieldOfView;
            data.cam.nearClipPlane = cam.nearClipPlane;
            data.cam.farClipPlane = cam.farClipPlane;
            data.cam.orthographic = cam.orthographic;
            data.cam.aspect = cam.aspect;
            data.cam.orthographicSize = cam.orthographicSize;
            data.cam.rect = new Rect(0, 0, 1, 1);
            data.cam.layerCullDistances = cam.layerCullDistances;
            data.cam.layerCullSpherical = cam.layerCullSpherical;

            if (data.target0 == null || data.target0.width != cam.pixelWidth || data.target0.height != cam.pixelHeight)
            {
                data.DestroyTargets();

                int width = cam.pixelWidth;
                int height = cam.pixelHeight;
                int depth = 24;

                RenderTextureFormat format = RenderTextureFormat.RGFloat;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.RGHalf;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGBHalf;

                data.target0 = new RenderTexture(width, height, depth, format, RenderTextureReadWrite.Linear);
                data.target0.filterMode = FilterMode.Point;
				data.target0.hideFlags = HideFlags.DontSave;
                data.target0.name = "Ceto Ocean Depths Render Target0: " + cam.name;

                if(cam.stereoEnabled)
                {
                    data.target1 = new RenderTexture(width, height, depth, format, RenderTextureReadWrite.Linear);
                    data.target1.filterMode = FilterMode.Point;
                    data.target1.hideFlags = HideFlags.DontSave;
                    data.target1.name = "Ceto Ocean Depths Render Target1: " + cam.name;
                }

            }

        }

        /// <summary>
        /// Create the refraction command buffer.
        /// </summary>
        void CreateRefractionCommand(Camera cam, DepthData data)
        {

            if (depthMode == DEPTH_MODE.USE_DEPTH_BUFFER)
            {
                //Need refraction command. Create and update.
                if (data.refractionCommand == null)
                    data.refractionCommand = new RefractionCommand(cam, copyDepthSdr, normalFadeSdr);

                //If commands has been disabled this frame then zeo texture.
                if (!data.refractionCommand.DisableCopyDepthCmd && DisableCopyDepthCmd)
                    Shader.SetGlobalTexture(Ocean.DEPTH_GRAB_TEXTURE_NAME, Texture2D.whiteTexture);

                if (!data.refractionCommand.DisableNormalFadeCmd && DisableNormalFadeCmd)
                    Shader.SetGlobalTexture(Ocean.NORMAL_FADE_TEXTURE_NAME, Texture2D.blackTexture);

                data.refractionCommand.DisableCopyDepthCmd = DisableCopyDepthCmd;
                data.refractionCommand.DisableNormalFadeCmd = DisableNormalFadeCmd;

                data.refractionCommand.UpdateCommands();
            }
            else
            {

                //Dont need the refraction command for this mode
                if (data.refractionCommand != null)
                {
                    data.refractionCommand.ClearCommands();
                    data.refractionCommand = null;
                }

            }
            
        }


        /// <summary>
        /// Create the bottom mesh. Just a radial mesh with the edges pulled up to surface.
        /// </summary>
        Mesh CreateBottomMesh(int segementsX, int segementsY)
        {

            Vector3[] vertices = new Vector3[segementsX * segementsY];
            Vector2[] texcoords = new Vector2[segementsX * segementsY];

            float TAU = Mathf.PI * 2.0f;
            float r;
            for (int x = 0; x < segementsX; x++)
            {
                for (int y = 0; y < segementsY; y++)
                {
                    r = (float)x / (float)(segementsX - 1);

                    vertices[x + y * segementsX].x = r * Mathf.Cos(TAU * (float)y / (float)(segementsY - 1));
                    vertices[x + y * segementsX].y = 0.0f;
                    vertices[x + y * segementsX].z = r * Mathf.Sin(TAU * (float)y / (float)(segementsY - 1));

                    if (x == segementsX - 1)
                    {
                        vertices[x + y * segementsX].y = 1.0f;
                    }
                }
            }

            int[] indices = new int[segementsX * segementsY * 6];

            int num = 0;
            for (int x = 0; x < segementsX - 1; x++)
            {
                for (int y = 0; y < segementsY - 1; y++)
                {
                    indices[num++] = x + y * segementsX;
                    indices[num++] = x + (y + 1) * segementsX;
                    indices[num++] = (x + 1) + y * segementsX;

                    indices[num++] = x + (y + 1) * segementsX;
                    indices[num++] = (x + 1) + (y + 1) * segementsX;
                    indices[num++] = (x + 1) + y * segementsX;

                }
            }

            Mesh mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.uv = texcoords;
            mesh.triangles = indices;
            mesh.name = "Ceto Bottom Mesh";
            mesh.hideFlags = HideFlags.HideAndDontSave;

            mesh.RecalculateNormals();
            ;

            return mesh;

        }

	}

}








