using UnityEngine;
using System;
using System.Collections;

using Ceto.Common.Threading.Tasks;
using Ceto.Common.Containers.Interpolation;

namespace Ceto
{

    /// <summary>
    /// 
    /// </summary>
    public class SpectrumTask : ThreadedTask
    {

        public const float GRAVITY = 9.818286f;
        public const float WAVE_CM = 0.23f;
        public const float WAVE_KM = 370.0f;

        public int Size { get; private set; }

        public int NumGrids { get; private set; }

        public Vector4 GridSizes { get; private set; }

        public Vector4 InverseGridSizes { get; private set; }

        public Vector4 WaveAmps { get; private set; }

        protected WaveSpectrumCondition Condition { get; private set; }

        Color[] m_spectrum01;

        Color[] m_spectrum23;

        Color[] m_wtable;

        ISpectrum[] m_spectrums;

        System.Random m_rnd;

        SPECTRUM_DISTRIBUTION m_distibution;

        public SpectrumTask(WaveSpectrumCondition condition, bool multiThreadTask, ISpectrum[] spectrums)
            : base(multiThreadTask)
        {

            if (spectrums == null || spectrums.Length != 4)
                throw new ArgumentException("Spectrums array must have a length of 4");

            m_spectrums = spectrums;

            Condition = condition;
            Size = condition.Key.Size;
            GridSizes = condition.GridSizes;
            WaveAmps = condition.WaveAmps;
            NumGrids = condition.Key.NumGrids;

            m_rnd = new System.Random(0);
            m_distibution = SPECTRUM_DISTRIBUTION.LINEAR;

            float factor = 2.0f * Mathf.PI * Size;
            InverseGridSizes = new Vector4(factor / GridSizes.x, factor / GridSizes.y, factor / GridSizes.z, factor / GridSizes.w);

            m_spectrum01 = new Color[Size * Size];

            if(NumGrids > 2)
                m_spectrum23 = new Color[Size * Size];

            m_wtable = new Color[Size * Size];

        }

        protected float RandomNumber()
        {

            switch(m_distibution)
            {
                case SPECTRUM_DISTRIBUTION.LINEAR:
                    return (float)m_rnd.NextDouble();

                case SPECTRUM_DISTRIBUTION.GAUSSIAN:
                    return GaussianRandomNumber();

                default:
                    return (float)m_rnd.NextDouble();
            }
            
        }

        float GaussianRandomNumber()
        {
            float x1, x2, w;
            do
            {
                x1 = 2.0f * (float)m_rnd.NextDouble() - 1.0f;
                x2 = 2.0f * (float)m_rnd.NextDouble() - 1.0f;
                w = x1 * x1 + x2 * x2;
            }
            while (w >= 1.0f);

            w = Mathf.Sqrt((-2.0f * Mathf.Log(w)) / w);

            return x1 * w;
        }

        public override void Start()
        {

            base.Start();

            Condition.CreateTextures();

            Condition.Done = false;

        }

        public override IEnumerator Run()
        {

            CreateWTable();
            GenerateWavesSpectrum();

            FinishedRunning();

            return null;
        }

        public override void End()
        {

            base.End();

            Condition.Apply(m_spectrum01, m_spectrum23, m_wtable);

            Condition.LastUpdated = Time.frameCount;
            Condition.Done = true;

        }

        float GetSpectrumSample(float i, float j, float dk, float kMin, float amp, ISpectrum spectrum)
        {

            if (spectrum == null) return 0.0f;

            float kx = i * dk;
            float ky = j * dk;
            float h = 0.0f;

            if (Math.Abs(kx) >= kMin || Math.Abs(ky) >= kMin)
            {
                float S = spectrum.Spectrum(kx, ky) * amp;
                h = Mathf.Sqrt(S * 0.5f) * dk;

                if (float.IsNaN(h) || float.IsInfinity(h)) h = 0.0f;
            }

            return h;
        }

        /// <summary>
        /// 
        /// </summary>
        void GenerateWavesSpectrum()
        {

            int size = Size;
            int hsize = size / 2;
            float fsize = (float)size;
            int grids = NumGrids;
            int idx;
            float i, j;

            Vector4 sample;

            float phi;
            const float sqrt2 = 1.414213562f;

            //float t = Time.realtimeSinceStartup;

            float PI_2 = Mathf.PI * 2.0f;
            float PI_GRID_X = Mathf.PI / GridSizes.x;
            float PI_GRID_SIZE_X = Mathf.PI * fsize / GridSizes.x;
            float PI_GRID_SIZE_Y = Mathf.PI * fsize / GridSizes.y;
            float PI_GRID_SIZE_Z = Mathf.PI * fsize / GridSizes.z;

            float dkx = PI_2 / GridSizes.x;
            float dky = PI_2 / GridSizes.y;
            float dkz = PI_2 / GridSizes.z;
            float dkw = PI_2 / GridSizes.w;

            float ampx = WaveAmps.x;
            float ampy = WaveAmps.y;
            float ampz = WaveAmps.z;
            float ampw = WaveAmps.w;

            for (int y = 0; y < size; y++)
            {

                for (int x = 0; x < size; x++)
                {
                    if (Cancelled) return;

                    idx = x + y * size;
                    i = (x >= hsize) ? (x - fsize) : x;
                    j = (y >= hsize) ? (y - fsize) : y;

                    if (grids > 0)
                    {
                        phi = RandomNumber() * PI_2;
                        sample.x = GetSpectrumSample(i, j, dkx, PI_GRID_X, ampx, m_spectrums[0]);
                        m_spectrum01[idx].r = sample.x * Mathf.Cos(phi) * sqrt2;
                        m_spectrum01[idx].g = sample.x * Mathf.Sin(phi) * sqrt2;
                    }

                    if (grids > 1)
                    {
                        phi = RandomNumber() * PI_2;
                        sample.y = GetSpectrumSample(i, j, dky, PI_GRID_SIZE_X, ampy, m_spectrums[1]);
                        m_spectrum01[idx].b = sample.y * Mathf.Cos(phi) * sqrt2;
                        m_spectrum01[idx].a = sample.y * Mathf.Sin(phi) * sqrt2;
                    }

                    if (grids > 2)
                    {
                        phi = RandomNumber() * PI_2;
                        sample.z = GetSpectrumSample(i, j, dkz, PI_GRID_SIZE_Y, ampz, m_spectrums[2]);
                        m_spectrum23[idx].r = sample.z * Mathf.Cos(phi) * sqrt2;
                        m_spectrum23[idx].g = sample.z * Mathf.Sin(phi) * sqrt2;
                    }

                    if (grids > 3)
                    {
                        phi = RandomNumber() * PI_2;
                        sample.w = GetSpectrumSample(i, j, dkw, PI_GRID_SIZE_Z, ampw, m_spectrums[3]);
                        m_spectrum23[idx].b = sample.w * Mathf.Cos(phi) * sqrt2;
                        m_spectrum23[idx].a = sample.w * Mathf.Sin(phi) * sqrt2;
                    }

                }
            }

            //Debug.Log("Spectrum time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

        /// <summary>
        ///
        /// </summary>
        void CreateWTable()
        {

            int i;
            int size = Size;
            float fsize = size;
            float isize = 1.0f / fsize;
            int grids = NumGrids;

            Vector4 inverseGridSizes2;
            inverseGridSizes2.x = InverseGridSizes.x * InverseGridSizes.x;
            inverseGridSizes2.y = InverseGridSizes.y * InverseGridSizes.y;
            inverseGridSizes2.z = InverseGridSizes.z * InverseGridSizes.z;
            inverseGridSizes2.w = InverseGridSizes.w * InverseGridSizes.w;

            float WAVE_KM_2 = WAVE_KM * WAVE_KM;

            Vector2 uv, st, st2;
            float k1, k2, k3, k4, w1, w2, w3, w4;

            //float t = Time.realtimeSinceStartup;

            for (int y = 0; y < size; y++)
            {

                for (int x = 0; x < size; x++)
                {
                    if (Cancelled) return;

                    i = x + y * size;

                    uv.x = x * isize;
                    uv.y = y * isize;

                    st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
                    st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

                    st2.x = st.x * st.x;
                    st2.y = st.y * st.y;

                    if (grids > 0)
                    {
                        k1 = Mathf.Sqrt(st2.x * inverseGridSizes2.x + st2.y * inverseGridSizes2.x);
                        w1 = Mathf.Sqrt(GRAVITY * k1 * (1.0f + k1 * k1 / WAVE_KM_2));
                        m_wtable[i].r = w1;
                    }

                    if (grids > 1)
                    {
                        k2 = Mathf.Sqrt(st2.x * inverseGridSizes2.y + st2.y * inverseGridSizes2.y);
                        w2 = Mathf.Sqrt(GRAVITY * k2 * (1.0f + k2 * k2 / WAVE_KM_2));
                        m_wtable[i].g = w2;
                    }

                    if (grids > 2)
                    {
                        k3 = Mathf.Sqrt(st2.x * inverseGridSizes2.z + st2.y * inverseGridSizes2.z);
                        w3 = Mathf.Sqrt(GRAVITY * k3 * (1.0f + k3 * k3 / WAVE_KM_2));
                        m_wtable[i].b = w3;
                    }

                    if (grids > 3)
                    {
                        k4 = Mathf.Sqrt(st2.x * inverseGridSizes2.w + st2.y * inverseGridSizes2.w);
                        w4 = Mathf.Sqrt(GRAVITY * k4 * (1.0f + k4 * k4 / WAVE_KM_2));
                        m_wtable[i].a = w4;
                    }
                }
            }

            //Debug.Log("WTable time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

    }

}