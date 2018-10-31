using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

using Ceto.Common.Unity.Utility;
using Ceto.Common.Threading.Scheduling;

#pragma warning disable 162, 649

namespace Ceto
{
	
    /// <summary>
    /// The Ocean components is responsible for managing the 
    /// child components (ie the Spectrum, Grids etc), storing
    /// the data they generate for each camera rendering the ocean
    /// and handling common behaviour.
    /// </summary>
	[AddComponentMenu("Ceto/Components/Ocean")]
	[DisallowMultipleComponent]
    public class Ocean : MonoBehaviour, ICoroutine
    {

		/// <summary>
		/// These are used to disable certain feature for debugging.
		/// If your not having problems dont touch these.
		/// </summary>
		public static readonly bool DISABLE_FOURIER_MULTITHREADING = false;
		public static readonly bool DISABLE_PROJECTED_GRID_BORDER = false;
		public static bool DISABLE_ALL_MULTITHREADING = false;
        public static readonly bool DISABLE_PROJECTION_FLIPPING = false;
        public static readonly bool DISABLE_PROJECT_SCENE_VIEW = false;

        /// <summary>
        /// Current version. Should match the version number
        /// in the Version.txt file. Dont change this.
        /// </summary>
        public static readonly string VERSION = "1.1.4";

        /// <summary>
        /// Store this instance as a static variable to provide easy 
        /// access to the ocean from any other script.
        /// This will be null if there is no ocean in the scene and
        /// before the ocean awakes. There can be only one ocean in
        /// a scene and if there is more than one the others will report
        /// a error and shut them self down. 
        /// </summary>
		public static Ocean Instance { get; private set; }

        /// <summary>
        /// The default layer for the ocean. 
        /// </summary>
        public static string OCEAN_LAYER = "Water";

        /// <summary>
        /// Texture names used by Ceto.
        /// </summary>
        public static readonly string REFLECTION_TEXTURE_NAME0 = "Ceto_Reflections0";
        public static readonly string REFLECTION_TEXTURE_NAME1 = "Ceto_Reflections1";
        public static readonly string REFRACTION_GRAB_TEXTURE_NAME = "Ceto_RefractionGrab";
        public static readonly string DEPTH_GRAB_TEXTURE_NAME = "Ceto_DepthBuffer";
        public static readonly string OCEAN_DEPTH_TEXTURE_NAME0 = "Ceto_OceanDepth0";
        public static readonly string OCEAN_DEPTH_TEXTURE_NAME1 = "Ceto_OceanDepth1";
        public static readonly string OCEAN_MASK_TEXTURE_NAME0 = "Ceto_OceanMask0";
        public static readonly string OCEAN_MASK_TEXTURE_NAME1 = "Ceto_OceanMask1";
        public static readonly string NORMAL_FADE_TEXTURE_NAME = "Ceto_NormalFade";

        /// <summary>
        /// The max wave height from the spectrum.
        /// This is just based from observation of the 
        /// heights when wind speed and scale are at there highest.
        /// </summary>
        public const float MAX_SPECTRUM_WAVE_HEIGHT = 40.0f;

		/// <summary>
		/// The max wave height from the overlays.
		/// </summary>
		public const float MAX_OVERLAY_WAVE_HEIGHT = 20.0f;

		/// <summary>
		/// The max possible wave height.
		/// </summary>
		public static float MAX_WAVE_HEIGHT { get { return MAX_SPECTRUM_WAVE_HEIGHT + MAX_OVERLAY_WAVE_HEIGHT; } }

		/// <summary>
		/// Disable warnings/info from being printed.
		/// </summary>
		public bool disableWarnings = false;
        public bool disableInfo = false;

        /// <summary>
        /// Should the projection use double precision math.
        /// </summary>
        public bool doublePrecisionProjection = true;

        /// <summary>
        /// Set the projector close to the camera for better resolutions.
        /// May need to disable this for some set ups with a large
        /// degree of freedom of the camera like VR.
        /// </summary>
        public bool tightProjectionFit = true;

        /// <summary>
        /// If a gameobject with a directional light is
        /// bound here the lights direction and color is 
        /// used for certain features like subsurface scatter.
        /// </summary>
		public GameObject m_sun;

        /// <summary>
        /// The ocean level. 
        /// </summary>
        public float level = 0.0f;

        /// <summary>
        /// The size compared to the screen size of the overlay height map.
        /// R - height.
        /// G - mask.
        /// </summary>
		public OVERLAY_MAP_SIZE heightOverlaySize = OVERLAY_MAP_SIZE.HALF;

        /// <summary>
        /// The size compared to the screen size of the overlay normal map.
        /// RGB - world space normal.
        /// A - mask.
        /// </summary>
		public OVERLAY_MAP_SIZE normalOverlaySize = OVERLAY_MAP_SIZE.FULL;

		/// <summary>
		/// The size compared to the screen size of the overlay foam map.
		/// R - foam.
		/// GB - empty.
		/// A - mask.
		public OVERLAY_MAP_SIZE foamOverlaySize = OVERLAY_MAP_SIZE.FULL;

		/// <summary>
		/// The size compared to the screen size of the overlay clip map.
		/// R - clip.
		public OVERLAY_MAP_SIZE clipOverlaySize = OVERLAY_MAP_SIZE.HALF;

        /// <summary>
        /// The mode used to blend the overlays when they are rendered into the buffer.
        /// </summary>
        public OVERLAY_BLEND_MODE heightBlendMode = OVERLAY_BLEND_MODE.ADD;

        /// <summary>
        /// The mode used to blend the overlays when they are rendered into the buffer.
        /// </summary>
        public OVERLAY_BLEND_MODE foamBlendMode = OVERLAY_BLEND_MODE.ADD;

        /// <summary>
        /// If no reflection component is added this is the default reflection color.
        /// </summary>
		public Color defaultSkyColor = new Color32(96, 147, 210, 255);

        /// <summary>
        /// If no underwater component is added this is the default water color.
        /// If a underwater component is added this is the inscatter color.
        /// </summary>
		public Color defaultOceanColor = new Color32(0, 19, 30, 255);
		
		/// <summary>
		/// The wind direction.
		/// </summary>
		[Range(0.0f, 360.0f)]
		public float windDir = 0.0f;
		public Vector3 WindDirVector { get; private set; }

        /// <summary>
        /// The roughness for the BRDF Lighting.
        /// </summary>
		[Range(0.0f, 1.0f)]
		public float specularRoughness = 0.2f;

		/// <summary>
		/// The roughness for the BRDF Lighting.
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float specularIntensity = 0.2f;

        /// <summary>
        /// The power for the fresnel.
        /// </summary>
		[Range(0.0f, 10.0f)]
		public float fresnelPower = 5.0f;

		/// <summary>
		/// The minimum fresnel.
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float minFresnel = 0.02f;

        /// <summary>
        /// The foam Tint. 
        /// </summary>
        public Color foamTint = Color.white;

        /// <summary>
        /// The foam intensity
        /// </summary>
        [Range(0.0f, 3.0f)]
        public float foamIntensity = 1.0f;

        /// <summary>
        /// The foam texture. 
        /// Adds the detail to the foam from the overlays and spectrum.
        /// </summary>
        public FoamTexture foamTexture0;

        /// <summary>
        /// The foam texture.
        /// Adds the detail to the foam from the overlays and spectrum.
        /// </summary>
        public FoamTexture foamTexture1;

        /// <summary>
        /// The shader used to render the overlays.
        /// </summary>
		[HideInInspector]
		public Shader waveOverlaySdr;

        /// <summary>
        /// The component used to manage the ocean grid.
        /// </summary>
		public ProjectedGrid Grid { get; set; }
		
        /// <summary>
        /// The component used to manage the reflections.
        /// </summary>
		public PlanarReflection Reflection { get; set; }
		
        /// <summary>
        /// The component used to manage the wave generation.
        /// </summary>
		public WaveSpectrum Spectrum { get; set; }
		
        /// <summary>
        /// The component used to manage the under water effects.
        /// </summary>
		public UnderWater UnderWater { get; set; }

        /// <summary>
        /// The time value used to generate the waves from.
        /// </summary>
		public IOceanTime OceanTime { get; set; }

		/// <summary>
		/// The projection used for the projected grid and the overlays.
		/// </summary>
		public IProjection Projection { get; private set; }

        /// <summary>
        /// Used to offset the position of the ocean.
        /// </summary>
        Vector3 m_positionOffset;
        public Vector3 PositionOffset
        {
            get { return m_positionOffset; }
            set
            {
                m_positionOffset = value;
                Shader.SetGlobalVector("Ceto_PosOffset", m_positionOffset);
            }
        }

        /// <summary>
        /// Hold the data generated for each camera that renders the ocean.
        /// </summary>
		Dictionary<Camera, CameraData> m_cameraData = new Dictionary<Camera, CameraData>();

        /// <summary>
        /// The number of cameras rendering the ocean.
        /// </summary>
        public int CameraCount { get { return m_cameraData.Count; } }

        /// <summary>
        /// The material used to render the overlays.
        /// </summary>
		Material m_waveOverlayMat;

        /// <summary>
        /// Manager class for the overlays.
        /// </summary>
		public OverlayManager OverlayManager { get; private set; }

        /// <summary>
        /// The scheduler used to manage tasks. 
        /// </summary>
		Scheduler m_scheduler;

		/// <summary>
		/// Uses this query for simple xz query's to
		/// save creating a new one per call.
		/// </summary>
		WaveQuery m_query = new WaveQuery(0.0f, 0.0f);

        /// <summary>
        /// If there was a error detected while running.
        /// If true this will shut the ocean down and all
        /// the attached components.
        /// </summary>
		public bool WasError { get; private set; }

		void Awake()
		{

            try
            {

                //There can only be one ocean in a scene
                if (Instance != null)
					throw new InvalidOperationException("There can only be one ocean instance.");
				else
					Instance = this;

                //This is so I dont forget to set these back to false when finished debugging.
                if (DISABLE_ALL_MULTITHREADING) LogInfo("Disabled multithreading is on");
                if (DISABLE_FOURIER_MULTITHREADING) LogInfo("Disabled Fourier multithreading is on");
                if (DISABLE_PROJECTED_GRID_BORDER) LogInfo("Disabled projection border is on");
                if (DISABLE_PROJECTION_FLIPPING) LogInfo("Disabled flipping is on");
                if (DISABLE_PROJECT_SCENE_VIEW) LogInfo("Disabled project scene is on");

#if CETO_DEBUG_SCHEDULER
				LogInfo("Debug scheduler is on");
#endif

#if UNITY_WEBGL
                DISABLE_ALL_MULTITHREADING = true;
                LogInfo("Disabled multithreading for WebGL");
#endif

#if CETO_USE_STEAM_VR
                LogInfo("Ceto using StreamVR enabled");
#endif

                OceanVR.Initialize();

                WindDirVector = CalculateWindDirVector();

				if(doublePrecisionProjection)
					Projection = new Projection3d(this);
				else
					Projection = new Projection3f(this);
				
				OceanTime = new OceanTime();
				
				m_waveOverlayMat = new Material(waveOverlaySdr);

				OverlayManager = new OverlayManager( m_waveOverlayMat);

                m_scheduler = new Scheduler(100, 100, this);

			}
			catch(Exception e)
			{
				LogError(e.ToString());
				DisableOcean();
			}

		}

        void Start()
        {
			try
			{
                //Create a texture to screen space matrix
                //Used in the overlay shader.
				Matrix4x4 T2S = Matrix4x4.identity;
				T2S.m00 = 2.0f; T2S.m03 = -1.0f;
				T2S.m11 = 2.0f; T2S.m13 = -1.0f;

				//Flip Y for render texture.
				for (int i = 0; i < 4; i++) {
					T2S[1,i] = -T2S[1,i];
				}

				Shader.SetGlobalMatrix("Ceto_T2S", T2S);

                //Zero these uniforms to something that wont cause a issue
				//if the component thats sets them is disabled.
				Shader.SetGlobalTexture("Ceto_Overlay_NormalMap", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_Overlay_HeightMap", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_Overlay_FoamMap", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_Overlay_ClipMap", Texture2D.blackTexture);
				Shader.SetGlobalTexture(REFRACTION_GRAB_TEXTURE_NAME, Texture2D.blackTexture);
                Shader.SetGlobalTexture(DEPTH_GRAB_TEXTURE_NAME, Texture2D.whiteTexture);
                Shader.SetGlobalTexture(OCEAN_MASK_TEXTURE_NAME0, Texture2D.blackTexture);
                Shader.SetGlobalTexture(OCEAN_MASK_TEXTURE_NAME1, Texture2D.blackTexture);
                Shader.SetGlobalTexture(OCEAN_DEPTH_TEXTURE_NAME0, Texture2D.blackTexture);
                Shader.SetGlobalTexture(OCEAN_DEPTH_TEXTURE_NAME1, Texture2D.blackTexture);
                Shader.SetGlobalTexture(NORMAL_FADE_TEXTURE_NAME, Texture2D.blackTexture);
                Shader.SetGlobalTexture("Ceto_SlopeMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_SlopeMap1", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap1", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap2", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap3", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_FoamMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_FoamMap1", Texture2D.blackTexture);
				Shader.SetGlobalVector("Ceto_GridSizes", Vector4.one);
				Shader.SetGlobalVector("Ceto_GridScale", Vector4.one);
				Shader.SetGlobalVector("Ceto_Choppyness", Vector4.one);
				Shader.SetGlobalFloat("Ceto_MapSize", 1);
                Shader.SetGlobalColor("Ceto_FoamTint", Color.white);
                Shader.SetGlobalTexture("Ceto_FoamTexture0", Texture2D.whiteTexture);
                Shader.SetGlobalTexture("Ceto_FoamTexture1", Texture2D.whiteTexture);
				Shader.SetGlobalVector("Ceto_FoamTextureScale0", Vector4.one);
				Shader.SetGlobalVector("Ceto_FoamTextureScale1", Vector4.one);
				Shader.SetGlobalFloat("Ceto_MaxWaveHeight", MAX_SPECTRUM_WAVE_HEIGHT);
                Shader.SetGlobalVector("Ceto_PosOffset", Vector3.zero);
                Shader.SetGlobalTexture("Ceto_CausticTexture", Texture2D.blackTexture);
                Shader.SetGlobalVector("Ceto_CausticTextureScale", new Vector4(1,1,0,0));
                Shader.SetGlobalColor("Ceto_CausticTint", Color.white);
                Shader.SetGlobalFloat("Ceto_Stero_Enabled", 0.0f);

            }
			catch(Exception e)
			{
				LogError(e.ToString());
				DisableOcean();
			}

        }

		void OnEnable()
		{
			//If there was a error you can not re-enable ocean.
			if(WasError)
				DisableOcean();

			//Camera.onPreRender += OceanOnPreRender;
			//Camera.onPreCull += OceanOnPreCull;
			Camera.onPostRender += OceanOnPostRender;

        }

		void OnDisable()
		{
			//Unless there was a error you can not disable ocean component 
			//as it is responsible for managing other components.
			//If you want to disable the ocean do it from the gameobject
			//enable not the component enable.
			if(!WasError)
				enabled = true;

			//Camera.onPreRender -= OceanOnPreRender;
			//Camera.onPreCull -= OceanOnPreCull;
			Camera.onPostRender -= OceanOnPostRender;

		}

        /// <summary>
        /// Disables the ocean gameobject 
        /// This is only called if there is a error.
        /// </summary>
		void DisableOcean()
		{
			WasError = true;
			enabled = false;
			gameObject.AddComponent<DisableGameObject>();
		}

        void Update()
        {

			try
			{

                WindDirVector = CalculateWindDirVector();

                Projection.TightFit = tightProjectionFit;

				UpdateOceanScheduler();

				OverlayManager.Update();

                specularRoughness = Mathf.Clamp01(specularRoughness);
				specularIntensity = Mathf.Max(0.0f, specularIntensity);
				minFresnel = Mathf.Clamp01(minFresnel);
				fresnelPower = Mathf.Max(0.0f, fresnelPower);
                foamIntensity = Math.Max(0.0f, foamIntensity);

				float sr = Mathf.Lerp(2e-5f, 2e-2f, specularRoughness);

	            Shader.SetGlobalColor("Ceto_DefaultSkyColor", defaultSkyColor);
				Shader.SetGlobalColor("Ceto_DefaultOceanColor", defaultOceanColor);
	            Shader.SetGlobalFloat("Ceto_SpecularRoughness", sr);
	            Shader.SetGlobalFloat("Ceto_FresnelPower", fresnelPower);
				Shader.SetGlobalFloat("Ceto_SpecularIntensity", specularIntensity);
				Shader.SetGlobalFloat("Ceto_MinFresnel", minFresnel);
				Shader.SetGlobalFloat("Ceto_OceanLevel", level);
				Shader.SetGlobalFloat("Ceto_MaxWaveHeight", MAX_SPECTRUM_WAVE_HEIGHT);
                Shader.SetGlobalColor("Ceto_FoamTint", foamTint * foamIntensity);
                Shader.SetGlobalVector("Ceto_SunDir", SunDir());
				Shader.SetGlobalVector("Ceto_SunColor", SunColor());
            
				Vector4 foamParam0 = new Vector4();
				foamParam0.x = (foamTexture0.scale.x != 0.0f) ? 1.0f / foamTexture0.scale.x : 1.0f;
				foamParam0.y = (foamTexture0.scale.y != 0.0f) ? 1.0f / foamTexture0.scale.y : 1.0f;
                foamParam0.z = foamTexture0.scrollSpeed * OceanTime.Now;
                foamParam0.w = 0.0f;

                Vector4 foamParam1 = new Vector4();
                foamParam1.x = (foamTexture1.scale.x != 0.0f) ? 1.0f / foamTexture1.scale.x : 1.0f;
                foamParam1.y = (foamTexture1.scale.y != 0.0f) ? 1.0f / foamTexture1.scale.y : 1.0f;
                foamParam1.z = foamTexture1.scrollSpeed * OceanTime.Now;
                foamParam1.w = 0.0f;

                Shader.SetGlobalTexture("Ceto_FoamTexture0", ((foamTexture0.tex != null) ? foamTexture0.tex : Texture2D.whiteTexture));
				Shader.SetGlobalVector("Ceto_FoamTextureScale0", foamParam0);

				Shader.SetGlobalTexture("Ceto_FoamTexture1", ((foamTexture1.tex != null) ? foamTexture1.tex : Texture2D.whiteTexture));
				Shader.SetGlobalVector("Ceto_FoamTextureScale1", foamParam1);

                //Rest each data element so they are updated this frame.
                var e = m_cameraData.GetEnumerator();
				while(e.MoveNext())
	            {
                    CameraData data = e.Current.Value;

					if(data.mask != null) data.mask.ClearUpdatedViews();
                    if (data.depth != null) data.depth.ClearUpdatedViews();
                    if (data.overlay != null) data.overlay.ClearUpdatedViews();
                    if (data.reflection != null) data.reflection.ClearUpdatedViews();

                    if (data.projection != null)
                    {
                        data.projection.ClearUpdatedViews();
                        data.projection.checkedForFlipping = false;
                    }
                }
			}
			catch(Exception e)
			{
				LogError(e.ToString());
				DisableOcean();
			}
        }

		void OnDestroy()
		{

			try
			{

				Ocean.Instance = null;

                if(OverlayManager != null)
                    OverlayManager.Release();

                if (m_scheduler != null)
                {
                    m_scheduler.ShutingDown = true;
                    m_scheduler.CancelAllTasks();
                }

                List<Camera> tmp = new List<Camera>(m_cameraData.Keys);
				foreach (Camera cam in tmp)
				{
                    RemoveCameraData(cam);
				}
			}
			catch(Exception e)
			{
				LogError(e.ToString());
				DisableOcean();
			}

		}

        void OnDrawGizmos()
        {
            //Draw projector frustum for debugging

            //if (Camera.main == null) return;
            //CameraData data = FindCameraData(Camera.main);
            //if (data.projection == null) return;
            //DrawCameraFrustum.DrawFrustum(data.projection.projectorVP, Color.red);

        }

        /// <summary>
        /// Register the component with the ocean.
        /// The OceanComponent base class handles the 
        /// general house keeping issues so need to
        /// cast to subclass. 
        /// </summary>
        public void Register(OceanComponent component)
		{

			if(component is ProjectedGrid)
				Grid = component as ProjectedGrid;
			else if(component is WaveSpectrum)
				Spectrum = component as WaveSpectrum;
			else if(component is PlanarReflection)
				Reflection = component as PlanarReflection;
			else if(component is UnderWater)
				UnderWater = component as UnderWater;
			else
				throw new InvalidCastException("Could not cast ocean component " + component.GetType());

		}

        /// <summary>
        /// De register the component with the ocean.
        /// The OceanComponent base class handles the 
        /// general house keeping issues so need to
        /// cast to subclass. 
        /// </summary>
		public void Deregister(OceanComponent component)
		{
			
			if(component is ProjectedGrid)
				Grid = null;
			else if(component is WaveSpectrum)
				Spectrum = null;
			else if(component is PlanarReflection)
				Reflection = null;
			else if(component is UnderWater)
				UnderWater = null;
			else
				throw new InvalidCastException("Could not cast ocean component " + component.GetType());
			
		}

		/// <summary>
		/// Logs the error.
		/// </summary>
		public static void LogError(string msg)
		{
            Debug.Log("<color=red>Ceto (" + VERSION + ") Error:</color> " + msg);
		}

		/// <summary>
		/// Logs the warning.
		/// </summary>
		public static void LogWarning(string msg)
		{
			if(Instance != null && Instance.disableWarnings) return;
            Debug.Log("<color=yellow>Ceto (" + VERSION + ") Warning:</color> " + msg);
		}

		/// <summary>
		/// Logs the info.
		/// </summary>
		public static void LogInfo(string msg)
		{
            if (Instance != null && Instance.disableInfo) return;
            Debug.Log("<color=cyan>Ceto (" + VERSION + ") Info:</color> " + msg);
		}

        /// <summary>
        /// Used to hook the scheduling code into Unity.
        /// </summary>
        public void RunCoroutine(IEnumerator e)
        {
            StartCoroutine(e);
        }

		/// <summary>
		/// Update Scheduler to process any threaded tasks.
		/// </summary>
		void UpdateOceanScheduler()
		{
			try
			{
                if (m_scheduler == null) return;
				m_scheduler.DisableMultithreading = DISABLE_ALL_MULTITHREADING;
				m_scheduler.CheckForException();
				m_scheduler.Update();
			}
			catch(Exception e)
			{
				LogError(e.ToString());
				DisableOcean();
			}
		}

		/// <summary>
		/// Calculates the wind dir vector
		/// from the wind angle.
		/// </summary>
		public Vector3 CalculateWindDirVector()
		{
			float theta = windDir * Mathf.PI / 180.0f;
			float u = -Mathf.Cos(theta);
			float v = Mathf.Sin(theta);

			Vector3 dir = new Vector3(u, 0.0f, v);
			return dir.normalized;
		}

		/// <summary>
		/// Suns the dir or up if node has been added.
		/// </summary>
		public Vector3 SunDir()
		{

			if(m_sun == null || m_sun.GetComponent<Light>() == null) 
				return Vector3.up;

			return m_sun.transform.forward * -1.0f;
		}

		/// <summary>
		/// Suns the color or white if node has been added.
		/// </summary>
		public Color SunColor()
		{
			
			if(m_sun == null || m_sun.GetComponent<Light>() == null) 
				return Color.white;
			
			return m_sun.GetComponent<Light>().color;
		}

        /// <summary>
        /// Finds the camera data for this camera.
        /// If no data exists then create new data
        /// object and return it.
        /// </summary>
		public CameraData FindCameraData(Camera cam)
		{

            if (cam == null)
                throw new InvalidOperationException("Can not find camera data for null camera");

            CameraData data = null;

            if (!m_cameraData.TryGetValue(cam, out data))
            {
                data = new CameraData();
                m_cameraData.Add(cam, data);
            }

            if (data.settings == null && !data.checkedForSettings)
            {
                data.settings = cam.GetComponent<OceanCameraSettings>();
                data.checkedForSettings = true;
            }

            return data;
		}

        /// <summary>
        /// Remove and release this camera 
        /// and its data from the ocean.
        /// </summary>
        public void RemoveCameraData(Camera cam)
        {
            if (!m_cameraData.ContainsKey(cam)) return;

            CameraData data = m_cameraData[cam];

            if (data.overlay != null)
            {
                OverlayManager.DestroyBuffers(data.overlay);
            }

            if (data.reflection != null)
            {
                data.reflection.DestroyTargets();
                data.reflection.DestroyCamera();
            }

            if(data.depth != null)
            {
                data.depth.DestroyTargets();
                data.depth.DestroyCamera();
            }

            if (data.mask != null)
            {
                data.mask.DestroyTargets();
                data.mask.DestroyCamera();
            }

            m_cameraData.Remove(cam);
        }

        /// <summary>
        /// The max displacement of the waves on vertical axis.
        /// </summary>
		public float FindMaxDisplacement(bool inculdeOverlays)
		{
			float max = 0.0f;

            if (!enabled) return max;

			if(Spectrum != null && Spectrum.enabled)
				max += Spectrum.MaxDisplacement.y;

            if (inculdeOverlays && OverlayManager != null)
                max += OverlayManager.MaxDisplacement;

            max = Mathf.Max(0.0f, max);

			return max;

		}

        /// <summary>
        /// Changes what the default query will sample.
        /// </summary>
        public void SetQueryWavesSampling(bool overlays, bool grid0, bool grid1, bool grid2, bool grid3)
        {

            m_query.sampleOverlay = overlays;
            m_query.sampleSpectrum[0] = grid0;
            m_query.sampleSpectrum[1] = grid1;
            m_query.sampleSpectrum[2] = grid2;
            m_query.sampleSpectrum[3] = grid3;

        }

        /// <summary>
        /// Queries the normal at this location using finite difference.
        /// This method is not 100% accurate and will be a bit slow but
        /// its the only option.
        /// </summary>
        public Vector3 QueryNormal(float x, float z)
        {

            float d = 0.5f;
    
            float hx0 = QueryWaves(x - d, z);
            float hz0 = QueryWaves(x, z - d);

            float hx1 = QueryWaves(x + d, z);
            float hz1 = QueryWaves(x, z + d);

            float dx = hx0 - hx1;
            float dz = hz0 - hz1;

            Vector3 n = new Vector3(dx, Mathf.Sqrt(1.0f - dx * dx - dz * dz), dz);

            return n;
        }

        /// <summary>
        /// Query the waves at world position xz.
        /// </summary>
		public float QueryWaves(float x, float z)
		{

			m_query.result.Clear();
			m_query.posX = x;
			m_query.posZ = z;
			
			if(enabled)
			{
				if(Spectrum != null)
					Spectrum.QueryWaves(m_query);

				if(OverlayManager != null)
					OverlayManager.QueryWaves(m_query);
			}
			
			return m_query.result.height + level;
		}

        /// <summary>
        /// Query the waves at world position xz.
        /// </summary>
        public float QueryWaves(float x, float z, out bool isClipped)
        {

            m_query.result.Clear();
            m_query.posX = x;
            m_query.posZ = z;

            if (enabled)
            {
                if (Spectrum != null)
                    Spectrum.QueryWaves(m_query);

                if (OverlayManager != null)
                    OverlayManager.QueryWaves(m_query);
            }

            isClipped = m_query.result.isClipped;

            return m_query.result.height + level;
        }

        /// <summary>
        /// Query the waves at world position xz.
        /// </summary>
        public void QueryWaves(WaveQuery query)
		{
            //Clear previous result in query.
			query.result.Clear();

			if(enabled)
			{
				if(Spectrum != null)
					Spectrum.QueryWaves(query);
				
				if(OverlayManager != null)
					OverlayManager.QueryWaves(query);
			}

			query.result.height += level;
		}

        /// <summary>
        /// Query the waves at world position xz
        /// with a batch of querys.
        /// </summary>
		public void QueryWaves(IEnumerable<WaveQuery> querys)
		{

			foreach(WaveQuery query in querys)
			{
                //Clear previous result in query.
				query.result.Clear();
				
				if(enabled)
				{
					if(Spectrum != null)
						Spectrum.QueryWaves(query);
					
					if(OverlayManager != null)
						OverlayManager.QueryWaves(query);
				}

				query.result.height += level;

			}

		}

        /// <summary>
        /// Query the waves at world position xz with a batch of querys in a task.
        /// This will run the querys on a separate thread.
        /// The call back function will be called when the batch has finished.
        /// </summary>
        public void QueryWavesWithAsyncTask(WaveQueryTask task)
        {

            if (task == null || m_scheduler == null) return;
            if (task.IsScheduled && !task.Done) return;

            if (enabled)
            {
                task.Reset(Spectrum, PositionOffset, level);

                if (task.SupportsOverlays)
                    task.OverlaySampler = OverlayManager;
                else
                    task.OverlaySampler = null;

                task.IsScheduled = true;
                m_scheduler.Run(task);
            }

        }

        /// <summary>
        /// Called before the camera renders the ocean.
        /// </summary>
        void OceanOnPreRender(Camera cam)
		{

            if (!cam.enabled) return;

            //if (cam.GetComponent<IgnoreOceanEvents>() != null) return;

            CameraData data = FindCameraData(cam);

			if(Grid != null)
				Grid.OceanOnPreRender(cam, data);

			if(Spectrum != null)
				Spectrum.OceanOnPreRender(cam, data);

			if(Reflection != null)
				Reflection.OceanOnPreRender(cam, data);

			if(UnderWater != null)
				UnderWater.OceanOnPreRender(cam, data);
		}

		/// <summary>
		/// Called before the camera culls the ocean.
		/// </summary>
		void OceanOnPreCull(Camera cam)
		{

            if (!cam.enabled) return;

            //if (cam.GetComponent<IgnoreOceanEvents>() != null) return;

			CameraData data = FindCameraData(cam);

			if(Grid != null)
				Grid.OceanOnPreCull(cam, data);
			
			if(Spectrum != null)
				Spectrum.OceanOnPreCull(cam, data);
			
			if(Reflection != null)
				Reflection.OceanOnPreCull(cam, data);
			
			if(UnderWater != null)
				UnderWater.OceanOnPreCull(cam, data);
		}

		/// <summary>
		/// Called after the camera renders the ocean.
		/// </summary>
		void OceanOnPostRender(Camera cam)
		{

            if (!cam.enabled) return;

			//if(cam.GetComponent<IgnoreOceanEvents>() != null) return;

            CameraData data = FindCameraData(cam);

			if(Grid != null)
				Grid.OceanOnPostRender(cam, data);
			
			if(Spectrum != null)
				Spectrum.OceanOnPostRender(cam, data);
			
			if(Reflection != null)
				Reflection.OceanOnPostRender(cam, data);
			
			if(UnderWater != null)
				UnderWater.OceanOnPostRender(cam, data);
		}

        /// <summary>
        /// Gets if this camera should render the reflections.
        /// </summary>
        bool GetDisableAllOverlays(OceanCameraSettings settings)
        {
            return (settings != null) ? settings.disableAllOverlays : false;
        }

        /// <summary>
        /// This game object is about to be rendered
        /// and requires the wave overlays.
        /// Create them for the camera rendering the object  
        /// if they have not already been updated this frame.
        /// </summary>
        public void RenderWaveOverlays(GameObject go)
        {

			try
			{

	            if (!enabled) return;

	            Camera cam = Camera.current;

	            if (!m_cameraData.ContainsKey(cam))
	                m_cameraData.Add(cam, new CameraData());

	            CameraData data = m_cameraData[cam];

				if (data.overlay == null)
					data.overlay = new WaveOverlayData();

				if(data.projection == null)
					data.projection = new ProjectionData();

				if(data.overlay.IsViewUpdated(cam)) return;

				//If the projection for this camera has not been updated this frame do it now.
				if(!data.projection.IsViewUpdated(cam))
				{
					Projection.UpdateProjection(cam, data);
				
					Shader.SetGlobalMatrix("Ceto_Interpolation", data.projection.interpolation);
					Shader.SetGlobalMatrix("Ceto_ProjectorVP", data.projection.projectorVP);
				}

                //If overlays have been disabled for this camera
                //clear the buffers and return;
                if (GetDisableAllOverlays(data.settings))
                {
                    OverlayManager.DestroyBuffers(data.overlay);
                    Shader.SetGlobalTexture("Ceto_Overlay_NormalMap", Texture2D.blackTexture);
                    Shader.SetGlobalTexture("Ceto_Overlay_HeightMap", Texture2D.blackTexture);
                    Shader.SetGlobalTexture("Ceto_Overlay_FoamMap", Texture2D.blackTexture);
                    Shader.SetGlobalTexture("Ceto_Overlay_ClipMap", Texture2D.blackTexture);
                }
                else
                {

                    OVERLAY_MAP_SIZE normalSize = (data.settings != null) ? data.settings.normalOverlaySize : normalOverlaySize;
                    OVERLAY_MAP_SIZE heightSize = (data.settings != null) ? data.settings.heightOverlaySize : heightOverlaySize;
                    OVERLAY_MAP_SIZE foamSize = (data.settings != null) ? data.settings.foamOverlaySize : foamOverlaySize;
                    OVERLAY_MAP_SIZE clipSize = (data.settings != null) ? data.settings.clipOverlaySize : clipOverlaySize;

                    //Create the overlay buffers.
                    OverlayManager.CreateOverlays(cam, data.overlay, normalSize, heightSize, foamSize, clipSize);

                    //Update blend modes.
                    OverlayManager.HeightOverlayBlendMode = heightBlendMode;
                    OverlayManager.FoamOverlayBlendMode = foamBlendMode;

                    //Render the overlays
                    OverlayManager.RenderWaveOverlays(cam, data.overlay);
                }

                data.overlay.SetViewAsUpdated(cam);

			}
			catch(Exception e)
			{
				LogError(e.ToString());
				DisableOcean();
			}

        }

        /// <summary>
        /// This game object is about to be rendered
        /// and requires the reflections.
        /// Create them for the camera rendering the object  
        /// if they have not already been updated this frame.
        /// </summary>
		public void RenderReflection(GameObject go)
		{
			
			if(!enabled || Reflection == null) return;
			
			Reflection.RenderReflection(go);
		}

        /// <summary>
        /// This game object is about to be rendered
        /// and requires the ocean mask.
        /// Create them for the camera rendering the object  
        /// if they have not already been updated this frame.
        /// </summary>
        public void RenderOceanMask(GameObject go)
        {

            if (!enabled || UnderWater == null) return;

            UnderWater.RenderOceanMask(go);
        }

        /// <summary>
        /// This game object is about to be rendered
        /// and requires the ocean depths.
        /// Create them for the camera rendering the object  
        /// if they have not already been updated this frame.
        /// </summary>
        public void RenderOceanDepth(GameObject go)
        {

            if (!enabled || UnderWater == null) return;

            UnderWater.RenderOceanDepth(go);
        }


    }

}













