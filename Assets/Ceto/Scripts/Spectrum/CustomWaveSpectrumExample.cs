using UnityEngine;
using System;

namespace Ceto
{

    /// <summary>
    /// This a example of how to add a custom spectrum to the WaveSpectrum component.
    /// This example just uses Phillips Spectrum as a example.
    /// This script would need to be added to the ocean game object in the scene.
    /// 
    /// You need to implement the ICustomWaveSpectrum interface on your component. 
    /// 
    /// You need to select the CUSTOM spectrum type on the WaveSpectrum script
    /// from the editor to have the custom spectrum be used.
    /// 
    /// </summary>
    [AddComponentMenu("Ceto/Components/CustomWaveSpectrumExample")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Ocean))]
    [RequireComponent(typeof(WaveSpectrum))]
    public class CustomWaveSpectrumExample : MonoBehaviour, ICustomWaveSpectrum
    {

        /// <summary>
        /// You need to define some properties that your spectrum
        /// will be based off. Here I am just using the wind speed
        /// but it could be whatever you like.
        /// </summary>
        [Range(0.0f, 30.0f)]
        public float windSpeed = 10.0f;

        /// <summary>
        /// If you want your spectrum data to be created on a separate thread
        /// return true here. This is recommended but if you cant make your
        /// spectrum code thread safe then return false.
        /// </summary>
        public bool MultiThreadTask { get { return true; } }

        void Awake()
        {

            //Grab the WaveSpectrum and add this to the CustomWaveSpectrum interface.
            WaveSpectrum spectrum = GetComponent<WaveSpectrum>();
            spectrum.CustomWaveSpectrum = this;

        }

        void Start()
        {

        }

        void Update()
        {

        }

        /// <summary>
        /// You need to create a key class (see below) and return it here.
        /// It needs to take the size, windDir, spectrum type and numGrids 
        /// as well as what ever settings you use to create the spectrum.
        /// The key is used by Ceto to work out when the settings have changed 
        /// and new spectrum data needs to be created. The key is also used to
        /// store the spectrum data so it can be reused if needed.
        /// </summary>
        public WaveSpectrumConditionKey CreateKey(int size, float windDir, SPECTRUM_TYPE spectrumType, int numGrids)
        {
            return new CustomSpectrumConditionKey(windSpeed, size, windDir, spectrumType, numGrids);
        }

        /// <summary>
        /// You need to create a class (see below) that implements the ISpectrum 
        /// interface and is what will be used by the SpectrumTask script to generate the
        /// spectrum data. The task will call the Spectrum function from a separate
        /// thread as its being created so it need to be thread safe.
        /// </summary>
        public ISpectrum CreateSpectrum(WaveSpectrumConditionKey key)
        {
            //Cast from base class to your key type
            CustomSpectrumConditionKey k = key as CustomSpectrumConditionKey;

            //If this happens something has gone wrong. 
            //Check what your returning in the CreateKey function.
            if (k == null)
                throw new InvalidCastException("Spectrum condition key is null or not the correct type");

            //Get the settings the spectrum will be created from the key.
            float windSpeed = k.WindSpeed;
            float windDir = k.WindDir;

            //return a new spectrum.
            return new CustomSpectrum(windSpeed, windDir);
        }


        /// <summary>
        /// Return what grid sizes you want to use
        /// based of the number of grids used.
        /// Here I am using a different grid size if
        /// 4 grids used but the same for 1, 2, or 3 grids.
        /// The grids sizes control the amount of space the wave
        /// textures will be tiled over.
        /// The x value would be used for grid 1, y for grid 2, etc.
        /// <param name="numGrids">num of grids used will be 1, 2, 3 or 4</param>
        public Vector4 GetGridSizes(int numGrids)
        {
            if(numGrids == 4)
                return new Vector4(1372, 217, 97, 31);
            else
                return new Vector4(217, 97, 31, 1);
        }

        /// <summary>
        /// Return what choppyness you want to use
        /// based of the number of grids used.
        /// Here I am using the same value for each grid number.
        /// The choppyness controls the amount of xz displacement.
        /// The x value would be used for grid 1, y for grid 2, etc.
        /// <param name="numGrids">num of grids used will be 1, 2, 3 or 4</param>
        public Vector4 GetChoppyness(int numGrids)
        {
            return new Vector4(1.5f, 1.2f, 1.0f, 1.0f);
        }

        /// <summary>
        /// Return what what wave amps you want to use
        /// based of the number of grids used.
        /// Here I am using a different wave amp if
        /// 4 grids used but the same for 1, 2, or 3 grids.
        /// The wave amp controllers the amplitude of the spectrum and therefore
        /// the amount of y displacement of the waves..
        /// The x value would be used for grid 1, y for grid 2, etc.
        /// <param name="numGrids">num of grids used will be 1, 2, 3 or 4</param>
        public Vector4 GetWaveAmps(int numGrids)
        {
            if (numGrids == 4)
                return new Vector4(0.5f, 1.0f, 1.0f, 1.0f);
            else
                return new Vector4(0.25f, 0.5f, 1.0f, 1.0f);
        }

        /// <summary>
        /// This is the key class return for the CreateKey function.
        /// It should pass the size, windDir, spectrumType and numGrids
        /// to the WaveSpectrumConditionKey base class. 
        /// </summary>
        public class CustomSpectrumConditionKey : WaveSpectrumConditionKey
        {

            /// <summary>
            /// Add a property for any settings your spectrum is based off.
            /// </summary>
            public float WindSpeed { get; private set; }

            public CustomSpectrumConditionKey(float windSpeed, int size, float windDir, SPECTRUM_TYPE spectrumType, int numGrids)
                : base(size, windDir, spectrumType, numGrids)
            {

                WindSpeed = windSpeed;

            }

            /// <summary>
            /// You need to check here if two keys are the same.
            /// You only need to compare your settings. The base 
            /// class will do the comparison for the other settings 
            /// and only call the Matches function if they do match.
            /// </summary>
            /// <param name="k">another key</param>
            /// <returns>If this key has the same settings as the other key</returns>
            protected override bool Matches(WaveSpectrumConditionKey k)
            {
                //Cast the key to your type.
                CustomSpectrumConditionKey key = k as CustomSpectrumConditionKey;

                //If the key is not of the same type return false
                if (key == null) return false;
                //If the key does not match your settings return false.
                if (WindSpeed != key.WindSpeed) return false;

                //else they have the same setting so would generate the same spectrum data so return true.
                return true;

            }

            /// <summary>
            /// You need to add your settings hash to the hash code.
            /// The base class will add the other settings.
            /// How you hash it is up to you.
            /// </summary>
            /// <param name="hashCode">unique number based of your settings</param>
            /// <returns>the hash code</returns>
            protected override int AddToHashCode(int hashcode)
            {
                //Add your settings hash to the current hash code.
                hashcode = (hashcode * 37) + WindSpeed.GetHashCode();

                return hashcode;
            }
        }

        /// <summary>
        /// This is the class that will be used to generate the spectrum and
        /// will be returned by the CreateSpectrum function.
        /// It should implement the ISpectrum interface and contain any
        /// settings your spectrum will be based off.
        /// This will create a Phillips Spectrum.
        /// </summary>
        public class CustomSpectrum : ISpectrum
        {

            readonly float GRAVITY = SpectrumTask.GRAVITY;

            readonly float AMP = 0.02f;

            readonly float WindSpeed;

            readonly Vector2 WindDir;

            readonly float length2, dampedLength2;

            /// <summary>
            /// You will need to pass in any settings your spectrum is based of and the wind direction.
            /// </summary>
            public CustomSpectrum(float windSpeed, float windDir)
            {
                WindSpeed = windSpeed;

                //You need to convert the wind direction (a value between 0-360) into a vector.
                float theta = windDir * Mathf.PI / 180.0f;
                WindDir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

                //You should precompute as much of the math as you can.
                float L = WindSpeed * WindSpeed / GRAVITY;
                length2 = L * L;

                float damping = 0.001f;
                dampedLength2 = length2 * damping * damping;

            }

            /// <summary>
            /// Return the spectrum value for kx, kz here.
            /// </summary>
            public float Spectrum(float kx, float kz)
            {
                //Rotate the spectrum based on the wind direction.
                float u = kx * WindDir.x - kz * WindDir.y;
                float v = kx * WindDir.y + kz * WindDir.x;

                kx = u;
                kz = v;

                float k_length = Mathf.Sqrt(kx * kx + kz * kz);
                if (k_length < 0.000001f) return 0.0f;

                float k_length2 = k_length * k_length;
                float k_length4 = k_length2 * k_length2;

                kx /= k_length;
                kz /= k_length;

                float k_dot_w = kx * 1.0f + kz * 0.0f;
                float k_dot_w2 = k_dot_w * k_dot_w * k_dot_w * k_dot_w * k_dot_w * k_dot_w;

                return AMP * Mathf.Exp(-1.0f / (k_length2 * length2)) / k_length4 * k_dot_w2 * Mathf.Exp(-k_length2 * dampedLength2);

            }
        }

    }

}