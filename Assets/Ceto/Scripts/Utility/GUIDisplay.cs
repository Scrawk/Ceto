using UnityEngine;
using System.Collections;

//using Ceto.Common.Unity.Utility;
//using uSky;

namespace Ceto
{

	public class GUIDisplay : MonoBehaviour 
	{

		float m_textWidth = 150.0f;

		Rect m_hideToggle = new Rect(20, 20, 95, 30);

		Rect m_reflectionsToggle = new Rect(120, 20, 95, 30);

		Rect m_refractionToggle = new Rect(220, 20, 95, 30);

        Rect m_detailToggle = new Rect(320, 20, 95, 30);

        Rect m_settings = new Rect(20, 60, 340, 600);

        Common.Unity.Utility.FPSCounter m_fps;

        bool m_ultraDetailOn;

        bool m_supportsDX11;

        public GameObject m_camera;

		public bool m_hide;

        //public GameObject m_uSky;

		void Start()
		{

			m_fps = GetComponent<Common.Unity.Utility.FPSCounter>();

            m_supportsDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;

        }

		void OnGUI() 
		{

			if(Ocean.Instance == null) return;

			ShipCamera shipCam = m_camera.GetComponent<ShipCamera>();
			UnderWaterPostEffect postEffect = m_camera.GetComponent< UnderWaterPostEffect>();

			WaveSpectrum spectrum = Ocean.Instance.GetComponent<WaveSpectrum>();
			PlanarReflection reflection = Ocean.Instance.GetComponent<PlanarReflection>();
			UnderWater underWater = Ocean.Instance.GetComponent<UnderWater>();
            ProjectedGrid grid = Ocean.Instance.GetComponent<ProjectedGrid>();

			if(true)
			{
				GUILayout.BeginArea(m_hideToggle);
				GUILayout.BeginHorizontal("Box");
				m_hide = GUILayout.Toggle(m_hide, " Hide GUI");
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
			}

			shipCam.disableInput = false;
			if (m_hide) return;
			shipCam.disableInput = true;

			if (reflection != null)
			{
				bool on = reflection.enabled;
				
				GUILayout.BeginArea(m_reflectionsToggle);
				GUILayout.BeginHorizontal("Box");
				on = GUILayout.Toggle(on, " Reflection");
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				
				reflection.enabled = on;
			}

			if(underWater != null)
			{
				bool on = underWater.enabled;
				
				GUILayout.BeginArea(m_refractionToggle);
				GUILayout.BeginHorizontal("Box");
				on = GUILayout.Toggle(on, " Refraction");
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				
				underWater.enabled = on;
			}

            if (spectrum != null && grid != null)
            {
     
                GUILayout.BeginArea(m_detailToggle);
                GUILayout.BeginHorizontal("Box");
                m_ultraDetailOn = GUILayout.Toggle(m_ultraDetailOn, " Ultra Detail");
                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                if (m_ultraDetailOn)
                {
                    grid.resolution = MESH_RESOLUTION.ULTRA;
                    spectrum.fourierSize = FOURIER_SIZE.ULTRA_256_GPU;
                    spectrum.disableReadBack = !m_supportsDX11;
                }
                else
                {
                    grid.resolution = MESH_RESOLUTION.HIGH;
                    spectrum.fourierSize = FOURIER_SIZE.MEDIUM_64_CPU;
                    spectrum.disableReadBack = true;
                }
            }

            GUILayout.BeginArea(m_settings);
			GUILayout.BeginVertical("Box");

			if(true)
			{
				float windDir = Ocean.Instance.windDir;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Wind Direction", GUILayout.MaxWidth(m_textWidth));
				windDir = GUILayout.HorizontalSlider(windDir, 0.0f, 360.0f);
				GUILayout.EndHorizontal();
				Ocean.Instance.windDir = windDir;
			}

            /*
            if (m_uSky != null)
            {
                float timeLine = m_uSky.GetComponent<uSkyManager>().Timeline;

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("Sun Dir", GUILayout.MaxWidth(m_textWidth));
                timeLine = GUILayout.HorizontalSlider(timeLine, 0.0f, 23.0f);
                GUILayout.EndHorizontal();

                m_uSky.GetComponent<uSkyManager>().Timeline = timeLine;
            }
            */

            if (spectrum != null)
			{
				float windSpeed = spectrum.windSpeed;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Wind Speed", GUILayout.MaxWidth(m_textWidth));
				windSpeed = GUILayout.HorizontalSlider(windSpeed, 0.0f, WaveSpectrum.MAX_WIND_SPEED);
				GUILayout.EndHorizontal();

				spectrum.windSpeed = windSpeed;
			}

			if(spectrum != null)
			{
				float waveAge = spectrum.waveAge;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Wave Age", GUILayout.MaxWidth(m_textWidth));
				waveAge = GUILayout.HorizontalSlider(waveAge, WaveSpectrum.MIN_WAVE_AGE, WaveSpectrum.MAX_WAVE_AGE);
				GUILayout.EndHorizontal();

				spectrum.waveAge = waveAge;
			}

			if(spectrum != null)
			{
				float waveSpeed = spectrum.waveSpeed;
				
				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Wave Speed", GUILayout.MaxWidth(m_textWidth));
				waveSpeed = GUILayout.HorizontalSlider(waveSpeed, 0.0f, WaveSpectrum.MAX_WAVE_SPEED);
				GUILayout.EndHorizontal();
				
				spectrum.waveSpeed = waveSpeed;
			}

			if(spectrum != null)
			{
				float choppyness = spectrum.choppyness;
				
				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Choppyness", GUILayout.MaxWidth(m_textWidth));
				choppyness = GUILayout.HorizontalSlider(choppyness, 0.0f, WaveSpectrum.MAX_CHOPPYNESS);
				GUILayout.EndHorizontal();
				
				spectrum.choppyness = choppyness;
			}

			if(spectrum != null)
			{
				float foamAmount = spectrum.foamAmount;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Foam Amount", GUILayout.MaxWidth(m_textWidth));
				foamAmount = GUILayout.HorizontalSlider(foamAmount, 0.0f, WaveSpectrum.MAX_FOAM_AMOUNT);
				GUILayout.EndHorizontal();

				spectrum.foamAmount = foamAmount;
			}

			if(spectrum != null)
			{
				float foamCoverage = spectrum.foamCoverage;
				
				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Foam Coverage", GUILayout.MaxWidth(m_textWidth));
				foamCoverage = GUILayout.HorizontalSlider(foamCoverage, 0.0f, WaveSpectrum.MAX_FOAM_COVERAGE);
				GUILayout.EndHorizontal();
				
				spectrum.foamCoverage = foamCoverage;
			}

			if(reflection != null)
			{
				int iterations = reflection.blurIterations;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Reflection blur", GUILayout.MaxWidth(m_textWidth));
				iterations = (int)GUILayout.HorizontalSlider(iterations, 0, 4);
				GUILayout.EndHorizontal();
	
				reflection.blurIterations = iterations;
			}

			if(reflection != null)
			{
				float intensity = reflection.reflectionIntensity;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Reflection Intensity", GUILayout.MaxWidth(m_textWidth));
				intensity = GUILayout.HorizontalSlider(intensity, 0.0f, PlanarReflection.MAX_REFLECTION_INTENSITY);
				GUILayout.EndHorizontal();
				
				reflection.reflectionIntensity = intensity;
			}

            if (underWater != null)
            {
                float intensity = underWater.aboveRefractionIntensity;

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("Refraction Intensity", GUILayout.MaxWidth(m_textWidth));
                intensity = GUILayout.HorizontalSlider(intensity, 0.0f, UnderWater.MAX_REFRACTION_INTENSITY);
                GUILayout.EndHorizontal();

                underWater.aboveRefractionIntensity = intensity;
            }

            if (spectrum != null)
            {
                int numGrids = spectrum.numberOfGrids;

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("Num Grids", GUILayout.MaxWidth(m_textWidth));
                numGrids = (int)GUILayout.HorizontalSlider(numGrids, 1, 4);
                GUILayout.EndHorizontal();

                spectrum.numberOfGrids = numGrids;
            }

            if (spectrum != null)
			{
				float scale = spectrum.gridScale;

				GUILayout.BeginHorizontal("Box");
				GUILayout.Label("Grid Scale", GUILayout.MaxWidth(m_textWidth));
				scale = GUILayout.HorizontalSlider(scale, WaveSpectrum.MIN_GRID_SCALE, WaveSpectrum.MAX_GRID_SCALE);
				GUILayout.EndHorizontal();

				spectrum.gridScale = scale;
			}


            if (underWater != null)
            {
                float intensity = underWater.subSurfaceScatterModifier.intensity;

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("SSS Intensity", GUILayout.MaxWidth(m_textWidth));
                intensity = GUILayout.HorizontalSlider(intensity, 0.0f, 10.0f);
                GUILayout.EndHorizontal();

                underWater.subSurfaceScatterModifier.intensity = intensity;
            }

            if (postEffect != null)
            {
                int blur = postEffect.blurIterations;

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("Underwater Blur", GUILayout.MaxWidth(m_textWidth));
                blur = (int)GUILayout.HorizontalSlider(blur, 0, 4);
                GUILayout.EndHorizontal();

                postEffect.blurIterations = blur;
            }

            if (true)
			{

				string info = 
@"W to move ship forward. A/D to turn.
Left click and drag to rotate camera.
Keypad +/- to move sun.
Ceto Version " + Ocean.VERSION;

				if(m_fps != null)
				{
					info += "\nCurrent FPS = " + m_fps.FrameRate.ToString("F2");
				}

				GUILayout.BeginHorizontal("Box");
				GUILayout.TextArea(info);
				GUILayout.EndHorizontal();

			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

	}

}


















