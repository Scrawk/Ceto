using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Ceto.Common.Unity.Utility;
using Ceto.Common.Threading.Scheduling;
using Ceto.Common.Threading.Tasks;
using Ceto.Common.Containers.Interpolation;
using Ceto.Common.Containers.Queues;

#pragma warning disable 414

namespace Ceto
{

    /// <summary>
    /// The WaveSpectrum component is responsible for
    /// creating the wave displacement, slope and foam textures.
    /// A spectrum of frequencies is created based on the wind
    /// speed, wind direction and wave age. This spectrum is then
    /// transformed from the frequency domain to the spatial domain
    /// using a Fast Fourier Transform algorithm. The wave displacements
    /// can be transformed on the CPU using threading or on the GPU.
    /// The slope and jacobians (foam) are always performed on the GPU.
    /// </summary>
	[AddComponentMenu("Ceto/Components/WaveSpectrum")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Ocean))]
	public class WaveSpectrum : OceanComponent
	{

        //These settings have been moved to the SpectrumTask script
        //public const float WAVE_CM = 0.23f;	// Eq 59
        //public const float WAVE_KM = 370.0f;	// Eq 59

        //These settings have been moved to the WaveSpectrumCondition script
        //and are set by the parent PhillipsSpectrumCondition and UnifiedSpectrumCondition scripts.
        //public static readonly Vector4 GRID_SIZES = new Vector4(1372, 392, 28, 4);
        //public static readonly Vector4 CHOPPYNESS = new Vector4(2.3f, 2.1f, 1.6f, 0.9f);

        public const float MAX_CHOPPYNESS = 1.2f;
		public const float MAX_FOAM_AMOUNT = 6.0f;
		public const float MAX_FOAM_COVERAGE = 0.5f;
		public const float MAX_WIND_SPEED = 30.0f;
		public const float MIN_WAVE_AGE = 0.5f;
		public const float MAX_WAVE_AGE = 1.0f;
		public const float MAX_WAVE_SPEED = 10.0f;
		public const float MIN_GRID_SCALE = 0.1f;
		public const float MAX_GRID_SCALE = 1.0f;
		public const float MAX_WAVE_SMOOTHING = 6.0f;
		public const float MIN_WAVE_SMOOTHING = 1.0f;
		public const float MAX_SLOPE_SMOOTHING = 6.0f;
		public const float MIN_SLOPE_SMOOTHING = 1.0f;
		public const float MAX_FOAM_SMOOTHING = 6.0f;
		public const float MIN_FOAM_SMOOTHING = 1.0f;

        /// <summary>
        /// Helper struct to keep track of the
        /// settings currently used for the buffers.
        /// </summary>
        struct BufferSettings
        {
            public bool beenCreated;
            public bool isCpu;
            public int size;
        }
		
		/// <summary>
		/// This is used to determine the fourier transform size and if its ran on the CPU or GPU. 
		/// </summary>
		public FOURIER_SIZE fourierSize = FOURIER_SIZE.MEDIUM_64_CPU;

        /// <summary>
        /// What type of spectrum to use.
        /// </summary>
        public SPECTRUM_TYPE spectrumType = SPECTRUM_TYPE.UNIFIED;

        /// <summary>
        /// Number of spectrum grids to use.
        /// </summary>
        [Range(1,4)]
        public int numberOfGrids = 4;

        /// <summary>
        /// If displacements are done on the GPU disables the read back to the CPU.
        /// This means there can be no height query's but fps will be better.
        /// </summary>
        public bool disableReadBack = true;

        /// <summary>
        /// Set to true if you want to disable any of the buffers.
        /// </summary>
        public bool disableDisplacements;
        public bool disableSlopes;
        public bool disableFoam;

		/// <summary>
		///  Should the white cap foam use the foam texture.
		/// </summary>
		public bool textureFoam = true;
		
		/// <summary>
		/// Amount of displacement for each grid on the xz axis.
		/// </summary>
		[Range(0.0f, MAX_CHOPPYNESS)]
		public float choppyness = 0.8f;

		/// <summary>
		/// Controls the amount of foam.
		/// </summary>
		[Range(0.0f, MAX_FOAM_AMOUNT)]
		public float foamAmount = 1.0f;

		/// <summary>
		/// Controls the foam coverage at the wave peaks.
		/// </summary>
		[Range(0.0f, MAX_FOAM_COVERAGE)]
		public float foamCoverage = 0.1f;

        /// <summary>
        /// A higher wind speed creates bigger waves.
        /// </summary>
		[Range(0.0f, MAX_WIND_SPEED)]
		public float windSpeed = 8.0f;

        /// <summary>
        /// Controls how long the waves last for.
        /// A higher values means the waves decay faster and are shorter lived. 
        /// </summary>
		[Range(MIN_WAVE_AGE, MAX_WAVE_AGE)]
		public float waveAge = 0.64f;

		/// <summary>
		/// Scales the time value used to generate waves from.
		/// </summary>
		[Range(0.0f, MAX_WAVE_SPEED)]
		public float waveSpeed = 1.0f;

		/// <summary>
		/// The grid scale on the horizontal and vertical axis.
		/// Scale both by same amount as there is a bug at the moment
		/// where if they are not scaled the same the height query's
		/// dont match the rendered waves. 
		/// </summary>
		[Range(MIN_GRID_SCALE, MAX_GRID_SCALE)]
		public float gridScale = 0.5f;

		/// <summary>
		/// Scales the derivative when sampling the displacement maps
		/// Helps remove aliasing issues.
		/// </summary>
		[Range(MIN_WAVE_SMOOTHING, MAX_WAVE_SMOOTHING)]
		public float waveSmoothing = 2.0f;

		/// <summary>
		/// Scales the derivative when sampling the slope maps
		/// Helps remove aliasing issues.
		/// </summary>
		[Range(MIN_SLOPE_SMOOTHING, MAX_SLOPE_SMOOTHING)]
		/*public*/ float slopeSmoothing = 1.0f;

		/// <summary>
		/// Scales the derivative when sampling the foam maps
		/// Helps remove aliasing issues.
		/// </summary>
		[Range(MIN_FOAM_SMOOTHING, MAX_FOAM_SMOOTHING)]
		/*public*/ float foamSmoothing = 2.0f;
	
		/// <summary>
		/// The textures holding the heights for each grid size.
		/// </summary>
		public IList<RenderTexture> DisplacementMaps { get { return m_displacementMaps; } }
		RenderTexture[] m_displacementMaps;
		
		/// <summary>
		/// The textures holding the slope for each grid size.
		/// </summary>
		public IList<RenderTexture> SlopeMaps { get { return m_slopeMaps; } }
		RenderTexture[] m_slopeMaps;

        /// <summary>
        /// The textures holding the foam for each grid size.
        /// </summary>
		public IList<RenderTexture> FoamMaps { get { return m_foamMaps; } }
		RenderTexture[] m_foamMaps;

        /// <summary>
        /// The maximum displacement on the x/z and y axis 
        /// for the current wave conditions. 
        /// </summary>
		public Vector2 MaxDisplacement { get; set; }

        /// <summary>
        /// Is true if the wind speed, wind direction or wave age has changed
        /// and the new spectrum is still being created. The spectrum is created
        /// on a separate thread and can take ~80ms at a fourier size of 64. 
        /// </summary>
		public bool IsCreatingNewCondition
        {
            get { return (m_conditions == null) ? false : m_conditions[1] != null; }
        }

        /// <summary>
        /// The materials used to copy the data created by the buffers into the textures.
        /// The materials reorganise the data so it can be sampled more efficiently and 
        /// allow for some post processing if needed. 
        /// </summary>
		Material m_slopeCopyMat, m_displacementCopyMat, m_foamCopyMat;

        /// <summary>
        /// Materials used to init the fourier data for the GPU buffers.
        /// </summary>
        Material m_slopeInitMat, m_displacementInitMat, m_foamInitMat;

        /// <summary>
        /// Gets the grid sizes.
        /// </summary>
        public Vector4 GridSizes { get { return m_gridSizes; } }
        Vector4 m_gridSizes = Vector4.one;

        /// <summary>
        /// The choppyness as a vector, one for each grid.
        /// </summary>
		public Vector4 Choppyness { get { return m_choppyness; } }
        Vector4 m_choppyness = Vector4.one;

        /// <summary>
        /// Used to run the threaded tasks for generating the spectrum 
        /// and the displacement FFT on the CPU.
        /// </summary>
		Scheduler m_scheduler;

        /// <summary>
        /// Holds the spectrum for the current conditions.
        /// The array[0] is the current conditions and the
        /// array[1] if not null is the new conditions that
        /// are still being generated on a separate thread. 
        /// </summary>
		WaveSpectrumCondition[] m_conditions;

        /// <summary>
        /// The buffers that manage the transformation of the spectrum into
        /// the displacement, slope or jacobian data. Can run on the CPU or GPU
        /// depending on the parent class type. 
        /// </summary>
		WaveSpectrumBuffer m_displacementBuffer, m_slopeBuffer, m_jacobianBuffer;

		/// <summary>
		/// Used to find the max range of the displacements.
		/// For larger fourier sizes this can take a while so 
		/// use a threaded task.
		/// </summary>
		FindRangeTask m_findRangeTask;

		/// <summary>
		/// The used to read data back from the GPU.
		/// </summary>
		ComputeBuffer m_readBuffer;

		/// <summary>
		/// All the scale parameters needed for wave query's.
		/// </summary>
		QueryGridScaling m_queryScaling = new QueryGridScaling();

        /// <summary>
        /// Some settings for the current buffers.
        /// </summary>
        BufferSettings m_bufferSettings = new BufferSettings();

        /// <summary>
        /// Cache the conditions as they can take a while to create.
        /// If the settings change and there is condition in the cache
        /// it can be used instead of creating a new one.
        /// </summary>
        DictionaryQueue<WaveSpectrumConditionKey, WaveSpectrumCondition> m_conditionCache 
            = new DictionaryQueue<WaveSpectrumConditionKey, WaveSpectrumCondition>();

        /// <summary>
        /// Max number of conditions cached.
        /// </summary>
        int m_maxConditionCacheSize = 10;
        public int MaxConditionCacheSize
        {
            get { return m_maxConditionCacheSize; }
            set { m_maxConditionCacheSize = value; }
        }

		/// <summary>
		/// Gets the displacement buffer.
		/// </summary>
		public IDisplacementBuffer DisplacementBuffer 
		{ 
			get { return m_displacementBuffer as IDisplacementBuffer; } 
		}

        /// <summary>
        /// Used to add a custom spectrum type.
        /// If this is not null and the spectrum type has been set to CUSTOM
        /// this interface will be used to create the required data.
        /// </summary>
        public ICustomWaveSpectrum CustomWaveSpectrum { get; set; }

        /// <summary>
        /// Required shaders that should be bound to the script.
        /// </summary>
		[HideInInspector]
		public Shader initSlopeSdr, initDisplacementSdr, initJacobianSdr, fourierSdr;

        /// <summary>
        /// Required shaders that should be bound to the script.
        /// </summary>
		[HideInInspector]
		public Shader slopeCopySdr, displacementCopySdr, foamCopySdr;

		/// <summary>
		/// Used to read displacement data from the GPU.
		/// </summary>
		[HideInInspector]
		public ComputeShader readSdr;

		void Start()
		{

			try
			{

				//Zero all textures
				Shader.SetGlobalTexture("Ceto_SlopeMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_SlopeMap1", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap1", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap2", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap3", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_FoamMap0", Texture2D.blackTexture);

				m_slopeCopyMat = new Material(slopeCopySdr);
				m_displacementCopyMat = new Material(displacementCopySdr);
				m_foamCopyMat = new Material(foamCopySdr);

                m_slopeInitMat = new Material(initSlopeSdr);
                m_displacementInitMat = new Material(initDisplacementSdr);
                m_foamInitMat = new Material(initJacobianSdr);

                m_scheduler = new Scheduler();

                CreateBuffers();
                CreateRenderTextures();
                CreateConditions();

                UpdateQueryScaling();

            }
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

		}

        protected override void OnDisable()
        {
            base.OnDisable();

            Shader.DisableKeyword("CETO_USE_4_SPECTRUM_GRIDS");

            //Zero all textures
            Shader.SetGlobalTexture("Ceto_SlopeMap0", Texture2D.blackTexture);
            Shader.SetGlobalTexture("Ceto_SlopeMap1", Texture2D.blackTexture);
            Shader.SetGlobalTexture("Ceto_DisplacementMap0", Texture2D.blackTexture);
            Shader.SetGlobalTexture("Ceto_DisplacementMap1", Texture2D.blackTexture);
            Shader.SetGlobalTexture("Ceto_DisplacementMap2", Texture2D.blackTexture);
            Shader.SetGlobalTexture("Ceto_DisplacementMap3", Texture2D.blackTexture);
            Shader.SetGlobalTexture("Ceto_FoamMap0", Texture2D.blackTexture);
            
        }

        protected override void OnDestroy()
		{

			base.OnDestroy();

			try
			{
                if (m_scheduler != null)
                {
                    m_scheduler.ShutingDown = true;
                    m_scheduler.CancelAllTasks();
                }

                //Clear the cache before releasing other resources.
				if(m_conditionCache != null)
				{
					//Release all conditions and mark cache as null
					//so current conditions are not cached.
					foreach(var condition in m_conditionCache)
						condition.Release();

					m_conditionCache.Clear();
					m_conditionCache = null;
				}

                Release();
			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
		
		}

        /// <summary>
        /// Release all data.
        /// </summary>
        void Release()
        {
            if (m_displacementBuffer != null)
            {
                m_displacementBuffer.Release();
                m_displacementBuffer = null;
            }

            if (m_slopeBuffer != null)
            {
                m_slopeBuffer.Release();
                m_slopeBuffer = null;
            }

            if (m_jacobianBuffer != null)
            {
                m_jacobianBuffer.Release();
                m_jacobianBuffer = null;
            }

            if (m_readBuffer != null)
            {
                m_readBuffer.Release();
                m_readBuffer = null;
            }

			if(m_conditions != null && m_conditions[0] != null && m_conditions[0].Done)
            {

                //Cache the condition so it can be resued.
                CacheCondition(m_conditions[0]);

                //if not a cached condition then release it.
                if (m_conditionCache == null || !m_conditionCache.ContainsKey(m_conditions[0].Key))
                {
                    m_conditions[0].Release();
                    m_conditions[0] = null;
                }
     
            }

			if(m_conditions != null && m_conditions[1] != null && m_conditions[1].Done)
            {
                //if not a cached condition then release it.
                if (m_conditionCache == null || !m_conditionCache.ContainsKey(m_conditions[1].Key))
                {
                    m_conditions[1].Release();
                    m_conditions[1] = null;
                }
            }

            m_conditions = null;
			m_findRangeTask = null;

            RTUtility.ReleaseAndDestroy(m_displacementMaps);
            m_displacementMaps = null;

            RTUtility.ReleaseAndDestroy(m_slopeMaps);
            m_slopeMaps = null;

            RTUtility.ReleaseAndDestroy(m_foamMaps);
            m_foamMaps = null;

        }

		void Update()
		{

			try
			{

				gridScale = Mathf.Clamp(gridScale, MIN_GRID_SCALE, MAX_GRID_SCALE);
				windSpeed = Mathf.Clamp(windSpeed, 0.0f, MAX_WIND_SPEED);
				waveAge = Mathf.Clamp(waveAge, MIN_WAVE_AGE, MAX_WAVE_AGE);
				waveSpeed = Mathf.Clamp(waveSpeed, 0.0f, MAX_WAVE_SPEED);
				foamAmount = Mathf.Clamp(foamAmount, 0.0f, MAX_FOAM_AMOUNT);
				foamCoverage = Mathf.Clamp(foamCoverage, 0.0f, MAX_FOAM_COVERAGE);
				waveSmoothing = Mathf.Clamp(waveSmoothing, MIN_WAVE_SMOOTHING, MAX_WAVE_SMOOTHING);
				slopeSmoothing = Mathf.Clamp(slopeSmoothing, MIN_SLOPE_SMOOTHING, MAX_SLOPE_SMOOTHING);
				foamSmoothing = Mathf.Clamp(foamSmoothing, MIN_FOAM_SMOOTHING, MAX_FOAM_SMOOTHING);
                numberOfGrids = Mathf.Clamp(numberOfGrids, 1, 4);

                //The time value used to create the waves
                float time = m_ocean.OceanTime.Now * waveSpeed;

                //If the settings have changed create a new buffers or conditions. 
                CreateBuffers();
                CreateRenderTextures();
                CreateConditions();

				int numGrids = m_conditions[0].Key.NumGrids;

				if(numGrids > 2)
					Shader.EnableKeyword("CETO_USE_4_SPECTRUM_GRIDS");
				else
					Shader.DisableKeyword("CETO_USE_4_SPECTRUM_GRIDS");

                UpdateQueryScaling();

				Shader.SetGlobalVector("Ceto_GridSizes", GridSizes);
				Shader.SetGlobalVector("Ceto_GridScale", new Vector2(gridScale, gridScale));
				Shader.SetGlobalVector("Ceto_Choppyness", Choppyness);
				Shader.SetGlobalFloat("Ceto_MapSize", m_bufferSettings.size);
				Shader.SetGlobalFloat("Ceto_WaveSmoothing", waveSmoothing);
				Shader.SetGlobalFloat("Ceto_SlopeSmoothing", slopeSmoothing);
				Shader.SetGlobalFloat("Ceto_FoamSmoothing", foamSmoothing);
				Shader.SetGlobalFloat("Ceto_TextureWaveFoam", (textureFoam) ? 1.0f : 0.0f);

                //Update the scheduler so any tasks that have just finished are processed. 
                UpdateSpectrumScheduler();
				
	            //Generate new data from the current time value. 
	            GenerateDisplacement(time);
	            GenerateSlopes(time);
	            GenerateFoam(time);

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
		}

        /// <summary>
        /// Update Scheduler to process any tasks that have been added.
        /// </summary>
		void UpdateSpectrumScheduler()
		{
			try
			{
				m_scheduler.DisableMultithreading = Ocean.DISABLE_ALL_MULTITHREADING;
				m_scheduler.CheckForException();
				m_scheduler.Update();
			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
		}

        /// <summary>
        /// Updates the settings for wave queries and also the grids size and choppyness vectors.
        /// </summary>
        void UpdateQueryScaling()
        {

            m_choppyness = m_conditions[0].Choppyness * Mathf.Clamp(choppyness, 0.0f, MAX_CHOPPYNESS);
            m_gridSizes = m_conditions[0].GridSizes;

            Vector4 invGridSizes = new Vector4();
            invGridSizes.x = 1.0f / (GridSizes.x * gridScale);
            invGridSizes.y = 1.0f / (GridSizes.y * gridScale);
            invGridSizes.z = 1.0f / (GridSizes.z * gridScale);
            invGridSizes.w = 1.0f / (GridSizes.w * gridScale);

            //update the query scaling settings.
            m_queryScaling.invGridSizes = invGridSizes;
            m_queryScaling.scaleY = gridScale;
            m_queryScaling.choppyness = Choppyness * gridScale;
            m_queryScaling.offset = m_ocean.PositionOffset;
            m_queryScaling.numGrids = m_conditions[0].Key.NumGrids;

        }

        /// <summary>
        /// Generates the slopes that will be used for the normal.
        /// Runs by transforming the spectrum on the GPU.
        /// </summary>
        void GenerateSlopes(float time)
		{

            //Need multiple render targets to run.
            if (!disableSlopes && SystemInfo.graphicsShaderLevel < 30)
            {
                Ocean.LogWarning("Spectrum slopes needs at least SM3 to run. Disabling slopes.");
                disableSlopes = true;
            }

            if (disableSlopes)
				m_slopeBuffer.DisableBuffer(-1);
			else
				m_slopeBuffer.EnableBuffer(-1);

            //If slopes disabled zero textures
            if (m_slopeBuffer.EnabledBuffers() == 0)
			{
                Shader.SetGlobalTexture("Ceto_SlopeMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_SlopeMap1", Texture2D.blackTexture);
			}
			else
			{
				int numGrids = m_conditions[0].Key.NumGrids;

				if(numGrids <= 2)
					m_slopeBuffer.DisableBuffer(1);

                //If the buffers has been run and this is the same time value as
                //last used then there is no need to run again.
                if (!m_slopeBuffer.HasRun || m_slopeBuffer.TimeValue != time) 
				{
                    m_slopeBuffer.InitMaterial = m_slopeInitMat;
                    m_slopeBuffer.InitPass = numGrids - 1;
					m_slopeBuffer.Run(m_conditions[0], time);
				}

				if(!m_slopeBuffer.BeenSampled)
				{
					m_slopeBuffer.EnableSampling();

                    //COPY GRIDS 1 and 2
					if(numGrids > 0)
					{
                    	m_slopeCopyMat.SetTexture("Ceto_SlopeBuffer", m_slopeBuffer.GetTexture(0));
                    	Graphics.Blit(null, m_slopeMaps[0], m_slopeCopyMat, 0);
						Shader.SetGlobalTexture("Ceto_SlopeMap0", m_slopeMaps[0]);
					}
					else
					{
						Shader.SetGlobalTexture("Ceto_SlopeMap0", Texture2D.blackTexture);
					}

                    //COPY GRIDS 3 and 4
                    if (numGrids > 2)
					{
						m_slopeCopyMat.SetTexture("Ceto_SlopeBuffer", m_slopeBuffer.GetTexture(1));
                    	Graphics.Blit(null, m_slopeMaps[1], m_slopeCopyMat, 0);
						Shader.SetGlobalTexture("Ceto_SlopeMap1", m_slopeMaps[1]);
					}
					else
					{
						Shader.SetGlobalTexture("Ceto_SlopeMap1", Texture2D.blackTexture);
					}

					m_slopeBuffer.DisableSampling();
					m_slopeBuffer.BeenSampled = true;
				}

			}

		}

		/// <summary>
		/// Generates the displacement.
		/// Runs by transforming the spectrum on the GPU or CPU.
		/// Buffer 0 does the heights while buffer 1 and 2 does the xz displacement.
		/// If buffer 0 is disable buffers 1 and 2 must also be disabled. 
		/// </summary>
        void GenerateDisplacement(float time)
		{

            //Need multiple render targets to run if running on GPU
            if (!disableDisplacements && SystemInfo.graphicsShaderLevel < 30 && m_displacementBuffer.IsGPU)
            {
                Ocean.LogWarning("Spectrum displacements needs at least SM3 to run on GPU. Disabling displacement.");
                disableDisplacements = true;
            }

			m_displacementBuffer.EnableBuffer(-1);

            if (disableDisplacements)
                m_displacementBuffer.DisableBuffer(-1);

            if (!disableDisplacements && choppyness == 0.0f)
			{
				//If choppyness is 0 then there will be no xz displacement so disable buffers 1 and 2.
				m_displacementBuffer.DisableBuffer(1);
                m_displacementBuffer.DisableBuffer(2);
			}

            if (!disableDisplacements && choppyness > 0.0f)
            {
                //If choppyness is > 0 then there will be xz displacement so eanable buffers 1 and 2.
                m_displacementBuffer.EnableBuffer(1);
                m_displacementBuffer.EnableBuffer(2);
            }

            //If all the buffers are disabled then zero the textures
            if (m_displacementBuffer.EnabledBuffers() == 0)
			{
				Shader.SetGlobalTexture("Ceto_DisplacementMap0", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap1", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap2", Texture2D.blackTexture);
				Shader.SetGlobalTexture("Ceto_DisplacementMap3", Texture2D.blackTexture);
                return;
			}
			else if(m_displacementBuffer.Done)
			{

                int numGrids = m_conditions[0].Key.NumGrids;
	
				if(numGrids <= 2)
					m_displacementBuffer.DisableBuffer(2);

				//Only enter if the buffers are done. Important as if running on the
				//CPU you must wait for all the threaded tasks to finish.
				//If the buffers has been run and this is the same time value as
				//last used then there is no need to run again.
				if(!m_displacementBuffer.HasRun || m_displacementBuffer.TimeValue != time) 
				{
                    m_displacementBuffer.InitMaterial = m_displacementInitMat;
                    m_displacementBuffer.InitPass = numGrids - 1;
                    m_displacementBuffer.Run(m_conditions[0], time);
				}

				if(!m_displacementBuffer.BeenSampled)
				{

                    m_displacementBuffer.EnableSampling();

					m_displacementCopyMat.SetTexture("Ceto_HeightBuffer", m_displacementBuffer.GetTexture(0));
					m_displacementCopyMat.SetTexture("Ceto_DisplacementBuffer", m_displacementBuffer.GetTexture(1));

                    //COPY GRIDS 1
                    if (numGrids > 0)
					{
                        //If only 1 grids used use pass 4 as the packing is different.
						Graphics.Blit(null, m_displacementMaps[0], m_displacementCopyMat, (numGrids == 1) ? 4 : 0);
						Shader.SetGlobalTexture("Ceto_DisplacementMap0", m_displacementMaps[0]);
					}
					else
					{
						Shader.SetGlobalTexture("Ceto_DisplacementMap0", Texture2D.blackTexture);
					}

                    //COPY GRIDS 2
                    if (numGrids > 1)
                    {
                        Graphics.Blit(null, m_displacementMaps[1], m_displacementCopyMat, 1);
                        Shader.SetGlobalTexture("Ceto_DisplacementMap1", m_displacementMaps[1]);
                    }
                    else
                    {
                        Shader.SetGlobalTexture("Ceto_DisplacementMap1", Texture2D.blackTexture);
                    }

                    m_displacementCopyMat.SetTexture("Ceto_DisplacementBuffer", m_displacementBuffer.GetTexture(2));

                    //COPY GRIDS 3
                    if (numGrids > 2)
					{
						Graphics.Blit(null, m_displacementMaps[2], m_displacementCopyMat, 2);
						Shader.SetGlobalTexture("Ceto_DisplacementMap2", m_displacementMaps[2]);
					}
					else
					{
						Shader.SetGlobalTexture("Ceto_DisplacementMap2", Texture2D.blackTexture);
					}

                    //COPY GRIDS 4
                    if (numGrids > 3)
                    {
                        Graphics.Blit(null, m_displacementMaps[3], m_displacementCopyMat, 3);
                        Shader.SetGlobalTexture("Ceto_DisplacementMap3", m_displacementMaps[3]);
                    }
                    else
                    {
                        Shader.SetGlobalTexture("Ceto_DisplacementMap3", Texture2D.blackTexture);
                    }

                    m_displacementBuffer.DisableSampling();
					m_displacementBuffer.BeenSampled = true;

					//If this is a GPU buffer then read data back to CPU.
					if(m_displacementBuffer.IsGPU)
						ReadFromGPU(numGrids);
	
					//Run the task to find the range of the data.
					FindRanges();
				}

			}
		
		}

		/// <summary>
		/// If the buffer generates the displacements on the GPU
		/// read them back to the CPU.
		/// </summary>
		void ReadFromGPU(int numGrids)
		{

			if(!disableReadBack && readSdr == null)
				Ocean.LogWarning("Trying to read GPU displacement data but the read shader is null");

			bool supportDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;

			if(!disableReadBack && readSdr != null && m_readBuffer != null && supportDX11)
			{

				//float t = Time.realtimeSinceStartup;

				InterpolatedArray2f[] buffer = DisplacementBuffer.GetReadDisplacements();

				if(numGrids > 0)
				{
					CBUtility.ReadFromRenderTexture(m_displacementMaps[0], 3, m_readBuffer, readSdr);
					m_readBuffer.GetData(buffer[0].Data);
				}
				else
				{
					buffer[0].Clear();
				}

				if(numGrids > 1)
				{
					CBUtility.ReadFromRenderTexture(m_displacementMaps[1], 3, m_readBuffer, readSdr);
					m_readBuffer.GetData(buffer[1].Data);
				}
				else
				{
					buffer[1].Clear();
				}

				if(numGrids > 2)
				{
					CBUtility.ReadFromRenderTexture(m_displacementMaps[2], 3, m_readBuffer, readSdr);
					m_readBuffer.GetData(buffer[2].Data);
				}
				else
				{
					buffer[2].Clear();
				}

				if(numGrids > 3)
				{
                    //not done. Waves to small to be worth reading back.
                    //CBUtility.ReadFromRenderTexture(m_displacementMaps[3], 3, m_readBuffer, readSdr);
                    //m_readBuffer.GetData(buffer[3].Data);
                }
				else
				{
					//buffer[3].Clear();
				}
	

				//Debug.Log("Read time = " + (Time.realtimeSinceStartup-t)*1000.0f);
			}

		}

		/// <summary>
		/// Finds the max range of the displacement data.
		/// Used to determine the range the waves cover.
		/// </summary>
		void FindRanges()
		{

			if(disableReadBack && DisplacementBuffer.IsGPU)
			{
				//If using GPU and readback disabled then have to set
				//max disable to the max possible. This may cause 
				//loss of some resolution in the projected grid.
				MaxDisplacement = new Vector2(0.0f, Ocean.MAX_SPECTRUM_WAVE_HEIGHT * gridScale);
			}
			else if(m_findRangeTask == null || m_findRangeTask.Done)
			{
				if(m_findRangeTask == null)
					m_findRangeTask = new FindRangeTask(this);
				
				m_findRangeTask.Reset();
				m_scheduler.Run(m_findRangeTask);
			}

            //Debug.Log(Time.frameCount + " " + MaxDisplacement.y);

		}

		/// <summary>
		/// Generates the foam.
		/// Runs by transforming the spectrum on the GPU.
		/// </summary>
        void GenerateFoam(float time)
		{

            Vector4 foamChoppyness = Choppyness;
            //foamChoppyness = m_conditions[0].Choppyness;

            //need multiple render targets to run.
            if(!disableFoam && SystemInfo.graphicsShaderLevel < 30)
            {
                Ocean.LogWarning("Spectrum foam needs at least SM3 to run. Disabling foam.");
                disableFoam = true;
            }

            float sqrMag = foamChoppyness.sqrMagnitude;

            m_jacobianBuffer.EnableBuffer(-1);

            if (disableFoam || foamAmount == 0.0f || sqrMag == 0.0f || !m_conditions[0].SupportsJacobians)
            {
                m_jacobianBuffer.DisableBuffer(-1);
            }

			//If all buffers disable zero textures.
			if(m_jacobianBuffer.EnabledBuffers() == 0)
			{
				Shader.SetGlobalTexture("Ceto_FoamMap0", Texture2D.blackTexture);
			}
			else
			{

				int numGrids = m_conditions[0].Key.NumGrids;

                if (numGrids == 1)
                {
                    m_jacobianBuffer.DisableBuffer(1);
                    m_jacobianBuffer.DisableBuffer(2);
                }
                else if(numGrids == 2)
                {
                    m_jacobianBuffer.DisableBuffer(2);
                }

                //If the buffers has been run and this is the same time value as
                //last used then there is no need to run again.
                if (!m_jacobianBuffer.HasRun || m_jacobianBuffer.TimeValue != time) 
				{
					m_foamInitMat.SetFloat("Ceto_FoamAmount", foamAmount);
                    m_jacobianBuffer.InitMaterial = m_foamInitMat;
                    m_jacobianBuffer.InitPass = numGrids - 1;
                    m_jacobianBuffer.Run(m_conditions[0], time);
				}

				if(!m_jacobianBuffer.BeenSampled)
				{

					m_jacobianBuffer.EnableSampling();

					m_foamCopyMat.SetTexture("Ceto_JacobianBuffer0", m_jacobianBuffer.GetTexture(0));
					m_foamCopyMat.SetTexture("Ceto_JacobianBuffer1", m_jacobianBuffer.GetTexture(1));
					m_foamCopyMat.SetTexture("Ceto_JacobianBuffer2", m_jacobianBuffer.GetTexture(2));
					m_foamCopyMat.SetTexture("Ceto_HeightBuffer", m_displacementBuffer.GetTexture(0));
                    m_foamCopyMat.SetVector("Ceto_FoamChoppyness", foamChoppyness);
					m_foamCopyMat.SetFloat("Ceto_FoamCoverage", foamCoverage);

                    Graphics.Blit(null, m_foamMaps[0], m_foamCopyMat, numGrids-1);
                    Shader.SetGlobalTexture("Ceto_FoamMap0", m_foamMaps[0]);

					m_jacobianBuffer.DisableSampling();
					m_jacobianBuffer.BeenSampled = true;
				}

			}

		}

		/// <summary>
		/// Queries the waves.
		/// </summary>
		public void QueryWaves(WaveQuery query)
		{

			if(!enabled) return;

			IDisplacementBuffer buffer = DisplacementBuffer;

			if(buffer == null) return;

			//Nothing to query if displacement done on GPU but read back disabled.
			if(disableReadBack && buffer.IsGPU) return;

            //Only these modes are relevant to this code.
            if (query.mode != QUERY_MODE.DISPLACEMENT && query.mode != QUERY_MODE.POSITION) return;

			//No spectrum grids will be sampled so return.
			if(!query.SamplesSpectrum) return;

            buffer.QueryWaves(query, m_queryScaling);

		}

        /// <summary>
        /// Creates all the buffers and related data. 
        /// If fourier settings have changed the buffers
        /// are all released and recreated with new settings.
        /// </summary>
        void CreateBuffers()
        {
            int size;
            bool isCpu;
            GetFourierSize(out size, out isCpu);

            if (m_bufferSettings.beenCreated)
            {
                //buffers have already been created.
                //Check to see if settings have changed.

                if (m_bufferSettings.size == size &&
                    m_bufferSettings.isCpu == isCpu)
                {
                    //settings have not changed.
                    return;
                }
                else
                {
					//Process all tasks until scheduler is empty.
					while(m_scheduler.HasTasks())
						UpdateSpectrumScheduler();

                    //Settings changed and safe to recreate buffers.
                    //Clear everything and reset.
                    Release();
                    m_bufferSettings.beenCreated = false;
                    
                }

            }

            //Displacements can be carried out on the CPU or GPU.
            //Only CPU displacements support height queries for buoyancy currently.
            if (isCpu)
                m_displacementBuffer = new DisplacementBufferCPU(size, m_scheduler);
            else
                m_displacementBuffer = new DisplacementBufferGPU(size, fourierSdr);

            m_slopeBuffer = new WaveSpectrumBufferGPU(size, fourierSdr, 2);
            m_jacobianBuffer = new WaveSpectrumBufferGPU(size, fourierSdr, 3);

            m_readBuffer = new ComputeBuffer(size * size, sizeof(float) * 3);

            m_conditions = new WaveSpectrumCondition[2];
            m_displacementMaps = new RenderTexture[4];
            m_slopeMaps = new RenderTexture[2];
            m_foamMaps = new RenderTexture[1];

            m_bufferSettings.beenCreated = true;
            m_bufferSettings.size = size;
            m_bufferSettings.isCpu = isCpu;

        }
		
		/// <summary>
		/// Create all the textures need to hold the data.
		/// </summary>
		void CreateRenderTextures()
		{

            int size = m_bufferSettings.size;
			int aniso = 9;

			//Must be float as some ATI cards will not render 
			//these textures correctly if format is half.
			RenderTextureFormat format = RenderTextureFormat.ARGBFloat;

			for(int i = 0; i < m_displacementMaps.Length; i++) {
				CreateMap(ref m_displacementMaps[i], "Displacement", format, size, aniso);
			}

			for(int i = 0; i < m_slopeMaps.Length; i++) {
				CreateMap(ref m_slopeMaps[i], "Slope", format, size, aniso);
			}

			for(int i = 0; i < m_foamMaps.Length; i++) {
				CreateMap(ref m_foamMaps[i], "Foam", format, size, aniso);
			}

		}

        void CreateMap(ref RenderTexture map, string name, RenderTextureFormat format, int size, int ansio)
        {


            if (map != null)
            {
                if (!map.IsCreated()) map.Create();
                return;
            }

            map = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            map.filterMode = FilterMode.Trilinear;
            map.wrapMode = TextureWrapMode.Repeat;
            map.anisoLevel = ansio;
            map.useMipMap = true;
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "Ceto Wave Spectrum " + name + " Texture";
            map.Create();

        }

        /// <summary>
        /// Creates the spectrum for a given set of conditions. 
        /// </summary>
        void CreateConditions()
		{

            int size = m_bufferSettings.size;
        
            var key = NewSpectrumConditionKey(size, windSpeed, m_ocean.windDir, waveAge);

			//If condition 0 is null this is start up or buffers have been recreated.
			if(m_conditions[0] == null)
			{
                if (m_conditionCache.ContainsKey(key))
                {
                    //A condition with new settings has already been created and is in the cache. Use that one.
                    m_conditions[0] = m_conditionCache[key];
                }
                else
                {
                    m_conditions[0] = NewSpectrumCondition(size, windSpeed, m_ocean.windDir, waveAge);

                    IThreadedTask task = m_conditions[0].GetCreateSpectrumConditionTask();

                    //This is the condition used each frame so it must be created now.
                    task.Start();
                    task.Run();
                    task.End();

                    //Cache condition.
                    CacheCondition(m_conditions[0]);
                }

			}
			else if(m_conditions[1] != null && m_conditions[1].Done)
			{
				//If condition 1 is not null and it is done then 
				//this is the new conditions and the task is now complete
				//so replace it with conditions 0.
				m_conditions[0] = m_conditions[1];
				m_conditions[1] = null;

                //Cache condition.
                CacheCondition(m_conditions[0]);

            }
			else if(m_conditions[1] == null && m_conditions[0].Done && key != m_conditions[0].Key)
			{
                //Condition settings have changed. Need to create new spectrum condition data.
                if (m_conditionCache.ContainsKey(key))
                {
                    //A condition with new settings has already been created and is in the cache.
                    m_conditions[0] = m_conditionCache[key];
                }
                else
                {
                    //A new condition must be created.
                    //If condition 1 is null it means there are currently no new conditions
                    //being created and if the settings have changed then create the new conditions.
                    m_conditions[1] = NewSpectrumCondition(size, windSpeed, m_ocean.windDir, waveAge);

                    //This can take quite a while to do
                    //so run on a separate thread using a task. 
                    IThreadedTask task = m_conditions[1].GetCreateSpectrumConditionTask();
                    m_scheduler.Add(task);
                }

			}

		}

		/// <summary>
		/// Caches the condition and trims cache to max size.
		/// </summary>
		void CacheCondition(WaveSpectrumCondition condition)
		{
			if(condition == null || m_conditionCache == null) return;

			if(m_conditionCache.ContainsKey(condition.Key)) return;

			m_conditionCache.AddFirst(condition.Key, condition);

			//Trim cache to the max size.
			while (m_conditionCache.Count != 0 && m_conditionCache.Count > MaxConditionCacheSize)
			{
				var tmp = m_conditionCache.RemoveLast();
				tmp.Release();
			}

		}

		/// <summary>
		/// Creates a condition from the settings and adds to cache.
		/// </summary>
		public void CreateAndCacheCondition(int fourierSize, float windSpeed, float windDir, float waveAge)
		{

			if(m_conditionCache == null) return;

			if(m_conditionCache.Count >= MaxConditionCacheSize)
			{
				Ocean.LogWarning("Condition cache full. Condition not cached.");
				return;
			}

			if(!Mathf.IsPowerOfTwo(fourierSize) || fourierSize < 32 || fourierSize > 512)
			{
				Ocean.LogWarning("Fourier size must be a pow2 number from 32 to 512. Condition not cached.");
				return;
			}

			var condition = NewSpectrumCondition(fourierSize, windSpeed, windDir, waveAge);

			if(m_conditionCache.ContainsKey(condition.Key)) return;
			
			IThreadedTask task = condition.GetCreateSpectrumConditionTask();
			
			//This is the condition must be created now.
			task.Start();
			task.Run();
			task.End();

			m_conditionCache.AddFirst(condition.Key, condition);

		}

        /// <summary>
        /// Create a new wave condition depending on the spectrum type used.
        /// </summary>
        WaveSpectrumCondition NewSpectrumCondition(int fourierSize, float windSpeed, float windDir, float waveAge)
        {

            WaveSpectrumCondition condition = null;

            switch(spectrumType)
            {
                case SPECTRUM_TYPE.UNIFIED:
                    condition = new UnifiedSpectrumCondition(fourierSize, windSpeed, windDir, waveAge, numberOfGrids);
                    break;

                case SPECTRUM_TYPE.UNIFIED_PHILLIPS:
                    condition = new UnifiedPhillipsSpectrumCondition(fourierSize, windSpeed, windDir, waveAge, numberOfGrids);
                    break;

                case SPECTRUM_TYPE.PHILLIPS:
                    condition = new PhillipsSpectrumCondition(fourierSize, windSpeed, windDir, waveAge, numberOfGrids);
                    break;

                case SPECTRUM_TYPE.CUSTOM:
                    {
                        if (CustomWaveSpectrum == null)
                        {
                            Ocean.LogWarning("Custom spectrum type selected but no custom spectrum interface has been added to the wave spectrum. Defaulting to Unified Spectrum");
                            spectrumType = SPECTRUM_TYPE.UNIFIED;
                            condition = new UnifiedSpectrumCondition(fourierSize, windSpeed, windDir, waveAge, numberOfGrids);
                        }
                        else
                        {
                            condition = new CustomWaveSpectrumCondition(CustomWaveSpectrum, fourierSize, windDir, numberOfGrids);
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException("Invalid spectrum type = " + spectrumType);
            }

            return condition;

        }

        /// <summary>
        /// Create a new wave condition key depending on the spectrum type used.
        /// </summary>
        WaveSpectrumConditionKey NewSpectrumConditionKey(int fourierSize, float windSpeed, float windDir, float waveAge)
        {

            WaveSpectrumConditionKey key = null;

            switch (spectrumType)
            {
                case SPECTRUM_TYPE.UNIFIED:
                    key = new UnifiedSpectrumConditionKey(windSpeed, waveAge, fourierSize, windDir, spectrumType, numberOfGrids);
                    break;

                case SPECTRUM_TYPE.UNIFIED_PHILLIPS:
                    key = new UnifiedSpectrumConditionKey(windSpeed, waveAge, fourierSize, windDir, spectrumType, numberOfGrids);
                    break;

                case SPECTRUM_TYPE.PHILLIPS:;
                    key = new PhillipsSpectrumConditionKey(windSpeed, fourierSize, windDir, spectrumType, numberOfGrids);
                    break;

                case SPECTRUM_TYPE.CUSTOM:
                    {
                        if (CustomWaveSpectrum == null)
                        {
                            Ocean.LogWarning("Custom spectrum type selected but no custom spectrum interface has been added to the wave spectrum. Defaulting to Unified Spectrum");
                            spectrumType = SPECTRUM_TYPE.UNIFIED;
                            key = new UnifiedSpectrumConditionKey(windSpeed, waveAge, fourierSize, windDir, spectrumType, numberOfGrids);
                        }
                        else
                        {
                            key = CustomWaveSpectrum.CreateKey(fourierSize, windDir, spectrumType, numberOfGrids);
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException("Invalid spectrum type = " + spectrumType);
            }

            return key;

        }

		/// <summary>
		/// Gets the size of the fourier transform and if the displacements
		/// are run on the CPU or GPU.
		/// </summary>
		void GetFourierSize(out int size, out bool isCpu)
		{
			
			switch((int)fourierSize)
			{
				
			case (int)FOURIER_SIZE.LOW_32_CPU:
				size = 32;
				isCpu = true;
				break;
				
			case (int)FOURIER_SIZE.LOW_32_GPU:
				size = 32;
				isCpu = false;
				break;
				
			case (int)FOURIER_SIZE.MEDIUM_64_CPU:
				size = 64;
				isCpu = true;
				break;
				
			case (int)FOURIER_SIZE.MEDIUM_64_GPU:
				size = 64;
				isCpu = false;
				break;
				
			case (int)FOURIER_SIZE.HIGH_128_CPU:
				size = 128;
				isCpu = true;
				break;
				
			case (int)FOURIER_SIZE.HIGH_128_GPU:
				size = 128;
				isCpu = false;
				break;
				
			case (int)FOURIER_SIZE.ULTRA_256_GPU:
				size = 256;
				isCpu = false;
				break;
				
			case (int)FOURIER_SIZE.EXTREME_512_GPU:
				size = 512;
				isCpu = false;
				break;
				
			default:
				size = 64;
				isCpu = true;
				break;
				
			}
			
			bool supportDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;
			
			if(!isCpu && !disableReadBack && !supportDX11)
			{
				Ocean.LogWarning("You card does not support dx11. Fourier can not be GPU. Changing to CPU. Disable read backs to use GPU but with no height querys.");
				fourierSize = FOURIER_SIZE.MEDIUM_64_CPU;
				size = 64;
				isCpu = true;
			}
			
		}
		
	}
}







