using UnityEngine;
using System;
using System.Collections;

using Ceto.Common.Threading.Tasks;

namespace Ceto
{

    public class InitSpectrumDisplacementsTask : ThreadedTask
    {

        public int NumGrids { get; private set; }

        public int Size { get; private set; }

        public int LastUpdated { get; protected set; }

        public float TimeValue { get; protected set; }

        public SPECTRUM_TYPE SpectrumType { get; private set; }

        protected DisplacementBufferCPU Buffer { get; private set; }

        protected Vector4[] Data0 { get; set; }
        protected Vector4[] Data1 { get; set; }
        protected Vector4[] Data2 { get; set; }

        Color[] m_spectrum01;

        Color[] m_spectrum23;

        Color[] m_wtable;

        Vector3[] m_ktable1, m_ktable2, m_ktable3, m_ktable4;


        public InitSpectrumDisplacementsTask(DisplacementBufferCPU buffer, WaveSpectrumCondition condition, float time)
            : base(true)
        {

            Buffer = buffer;
            NumGrids = condition.Key.NumGrids;
            Size = condition.Key.Size;
            SpectrumType = condition.Key.SpectrumType;
            TimeValue = time;

            Reset(condition, time);

            CreateKTables(condition.InverseGridSizes());

        }

        public void Reset(WaveSpectrumCondition condition, float time)
        {

            if (condition.Key.SpectrumType != SpectrumType)
                throw new InvalidOperationException("Trying to reset a Unified InitSpectrum task with wrong condition type = " + condition.Key.SpectrumType);

            if (condition.Key.Size != Size)
                throw new InvalidOperationException("Trying to reset a Unified InitSpectrum task with wrong condition size = " + condition.Key.Size);

            base.Reset();

            int S2 = Size * Size;

            if (m_spectrum01 == null)
                m_spectrum01 = new Color[S2];

            if (m_spectrum23 == null && NumGrids > 2)
                m_spectrum23 = new Color[S2];

            if (m_wtable == null)
                m_wtable = new Color[S2];

            TimeValue = time;

            Data0 = Buffer.GetReadBuffer(0);
            Data1 = Buffer.GetReadBuffer(1);
            Data2 = Buffer.GetReadBuffer(2);

            var buffer0 = Buffer.GetBuffer(0);
            var buffer1 = Buffer.GetBuffer(1);
            var buffer2 = Buffer.GetBuffer(2);

            if (buffer0 != null)
            {
                if(NumGrids > 2)
                    buffer0.doublePacked = true;
                else
                    buffer0.doublePacked = false;
            }

            if (buffer1 != null)
            {
                if (NumGrids > 1)
                    buffer1.doublePacked = true;
                else
                    buffer1.doublePacked = false;
            }

            if (buffer2 != null)
            {
                if(NumGrids > 3)
                    buffer2.doublePacked = true;
                else
                    buffer2.doublePacked = false;
            }

            if (LastUpdated != condition.LastUpdated)
            {
                LastUpdated = condition.LastUpdated;

                if(m_spectrum01 != null && condition.SpectrumData01 != null)
                    System.Array.Copy(condition.SpectrumData01, m_spectrum01, S2);

                if (m_spectrum23 != null && condition.SpectrumData23 != null)
                    System.Array.Copy(condition.SpectrumData23, m_spectrum23, S2);

                if (m_wtable != null && condition.WTableData != null)
                    System.Array.Copy(condition.WTableData, m_wtable, S2);
            }

        }

        public override IEnumerator Run()
        {

            if (NumGrids == 1)
            {
                InitilizeGrids1();
            }
            else if (NumGrids == 2)
            {
                InitilizeGrids2();
            }
            else if (NumGrids == 3)
            {
                InitilizeGrids3();
            }
            else if (NumGrids == 4)
            {
                InitilizeGrids4();
            }

            FinishedRunning();
            return null;
        }

        void InitilizeGrids1()
        {

            Vector2 uv, st, h1, n1;
            Vector3 k1;
            Color s12, s12c;
            int i, j;
            float w;
            float c, s;

            int size = Size;
            float ifsize = 1.0f / (float)size;

            //float t = Time.realtimeSinceStartup;

            for (int y = 0; y < size; y++)
            {

                for (int x = 0; x < size; x++)
                {
                    if (Cancelled) return;

                    uv.x = x * ifsize;
                    uv.y = y * ifsize;

                    st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
                    st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

                    i = x + y * size;
                    j = ((size - x) % size) + ((size - y) % size) * size;

                    s12 = m_spectrum01[i];
                    s12c = m_spectrum01[j];

                    w = m_wtable[i].r * TimeValue;

                    c = Mathf.Cos(w);
                    s = Mathf.Sin(w);

                    h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
                    h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

                    if (Data0 != null)
                    {
                        Data0[i].x = h1.x;
                        Data0[i].y = h1.y;
                        Data0[i].z = 0.0f;
                        Data0[i].w = 0.0f;
                    }

                    if (Data1 != null)
                    {
                        k1 = m_ktable1[i];

                        n1.x = -(k1.x * h1.y) - k1.y * h1.x;
                        n1.y = k1.x * h1.x - k1.y * h1.y;

                        Data1[i].x = n1.x * k1.z;
                        Data1[i].y = n1.y * k1.z;
                        Data1[i].z = 0.0f;
                        Data1[i].w = 0.0f;
                    }

                }
            }

            //Debug.Log("InitSpectrum 1 grid time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

        void InitilizeGrids2()
        {

            Vector2 uv, st;
            Vector3 k1, k2;
            Vector2 h1, h2;
            Vector2 n1, n2;
            Color s12, s12c;
            int i, j;
            Color w;
            float c, s;

            int size = Size;
            float ifsize = 1.0f / (float)size;

            //float t = Time.realtimeSinceStartup;

            for (int y = 0; y < size; y++)
            {

                for (int x = 0; x < size; x++)
                {
                    if (Cancelled) return;

                    uv.x = x * ifsize;
                    uv.y = y * ifsize;

                    st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
                    st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

                    i = x + y * size;
                    j = ((size - x) % size) + ((size - y) % size) * size;

                    s12 = m_spectrum01[i];
                    s12c = m_spectrum01[j];

                    w = m_wtable[i];

                    w.r *= TimeValue;
                    w.g *= TimeValue;

                    c = Mathf.Cos(w.r);
                    s = Mathf.Sin(w.r);

                    h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
                    h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

                    c = Mathf.Cos(w.g);
                    s = Mathf.Sin(w.g);

                    h2.x = (s12.b + s12c.b) * c - (s12.a + s12c.a) * s;
                    h2.y = (s12.b - s12c.b) * s + (s12.a - s12c.a) * c;

                    if (Data0 != null)
                    {
                        Data0[i].x = h1.x + -h2.y;
                        Data0[i].y = h1.y + h2.x;
                        Data0[i].z = 0.0f;
                        Data0[i].w = 0.0f;
                    }

                    if (Data1 != null)
                    {
                        k1 = m_ktable1[i];
                        k2 = m_ktable2[i];

                        n1.x = -(k1.x * h1.y) - k1.y * h1.x;
                        n1.y = k1.x * h1.x - k1.y * h1.y;

                        n2.x = -(k2.x * h2.y) - k2.y * h2.x;
                        n2.y = k2.x * h2.x - k2.y * h2.y;

                        Data1[i].x = n1.x * k1.z;
                        Data1[i].y = n1.y * k1.z;
                        Data1[i].z = n2.x * k2.z;
                        Data1[i].w = n2.y * k2.z;
                    }

                }
            }

            //Debug.Log("InitSpectrum 4 grids time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

        void InitilizeGrids3()
        {

            Vector2 uv, st;
            Vector3 k1, k2, k3;
            Vector2 h1, h2, h3;
            Vector2 n1, n2, n3;
            Color s12, s34, s12c, s34c;
            int i, j;
            Color w;
            float c, s;

            int size = Size;
            float ifsize = 1.0f / (float)size;

            //float t = Time.realtimeSinceStartup;

            for (int y = 0; y < size; y++)
            {

                for (int x = 0; x < size; x++)
                {
                    if (Cancelled) return;

                    uv.x = x * ifsize;
                    uv.y = y * ifsize;

                    st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
                    st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

                    i = x + y * size;
                    j = ((size - x) % size) + ((size - y) % size) * size;

                    s12 = m_spectrum01[i];
                    s34 = m_spectrum23[i];

                    s12c = m_spectrum01[j];
                    s34c = m_spectrum23[j];

                    w = m_wtable[i];

                    w.r *= TimeValue;
                    w.g *= TimeValue;
                    w.b *= TimeValue;
                    w.a *= TimeValue;

                    c = Mathf.Cos(w.r);
                    s = Mathf.Sin(w.r);

                    h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
                    h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

                    c = Mathf.Cos(w.g);
                    s = Mathf.Sin(w.g);

                    h2.x = (s12.b + s12c.b) * c - (s12.a + s12c.a) * s;
                    h2.y = (s12.b - s12c.b) * s + (s12.a - s12c.a) * c;

                    c = Mathf.Cos(w.b);
                    s = Mathf.Sin(w.b);

                    h3.x = (s34.r + s34c.r) * c - (s34.g + s34c.g) * s;
                    h3.y = (s34.r - s34c.r) * s + (s34.g - s34c.g) * c;

                    if (Data0 != null)
                    {
                        Data0[i].x = h1.x + -h2.y;
                        Data0[i].y = h1.y + h2.x;
                        Data0[i].z = h3.x;
                        Data0[i].w = h3.y;
                    }

                    if (Data1 != null)
                    {
                        k1 = m_ktable1[i];
                        k2 = m_ktable2[i];

                        n1.x = -(k1.x * h1.y) - k1.y * h1.x;
                        n1.y = k1.x * h1.x - k1.y * h1.y;

                        n2.x = -(k2.x * h2.y) - k2.y * h2.x;
                        n2.y = k2.x * h2.x - k2.y * h2.y;

                        Data1[i].x = n1.x * k1.z;
                        Data1[i].y = n1.y * k1.z;
                        Data1[i].z = n2.x * k2.z;
                        Data1[i].w = n2.y * k2.z;
                    }

                    if (Data2 != null)
                    {
                        k3 = m_ktable3[i];

                        n3.x = -(k3.x * h3.y) - k3.y * h3.x;
                        n3.y = k3.x * h3.x - k3.y * h3.y;

                        Data2[i].x = n3.x * k3.z;
                        Data2[i].y = n3.y * k3.z;
                        Data2[i].z = 0.0f;
                        Data2[i].w = 0.0f;
                    }

                }
            }

            //Debug.Log("InitSpectrum 4 grids time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

        void InitilizeGrids4()
        {

            Vector2 uv, st;
            Vector3 k1, k2, k3, k4;
            Vector2 h1, h2, h3, h4;
            Vector2 n1, n2, n3, n4;
            Color s12, s34, s12c, s34c;
            int i, j;
            Color w;
            float c, s;

            int size = Size;
            float ifsize = 1.0f / (float)size;

            //float t = Time.realtimeSinceStartup;

            for (int y = 0; y < size; y++)
            {
                
                for (int x = 0; x < size; x++)
                {

                    if (Cancelled) return;

                    uv.x = x * ifsize;
                    uv.y = y * ifsize;

                    st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
                    st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

                    i = x + y * size;
                    j = ((size - x) % size) + ((size - y) % size) * size;

                    s12 = m_spectrum01[i];
                    s34 = m_spectrum23[i];

                    s12c = m_spectrum01[j];
                    s34c = m_spectrum23[j];

                    w = m_wtable[i];

                    w.r *= TimeValue;
                    w.g *= TimeValue;
                    w.b *= TimeValue;
                    w.a *= TimeValue;

                    c = Mathf.Cos(w.r);
                    s = Mathf.Sin(w.r);

                    h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
                    h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

                    c = Mathf.Cos(w.g);
                    s = Mathf.Sin(w.g);

                    h2.x = (s12.b + s12c.b) * c - (s12.a + s12c.a) * s;
                    h2.y = (s12.b - s12c.b) * s + (s12.a - s12c.a) * c;

                    c = Mathf.Cos(w.b);
                    s = Mathf.Sin(w.b);

                    h3.x = (s34.r + s34c.r) * c - (s34.g + s34c.g) * s;
                    h3.y = (s34.r - s34c.r) * s + (s34.g - s34c.g) * c;

                    c = Mathf.Cos(w.a);
                    s = Mathf.Sin(w.a);

                    h4.x = (s34.b + s34c.b) * c - (s34.a + s34c.a) * s;
                    h4.y = (s34.b - s34c.b) * s + (s34.a - s34c.a) * c;

                    if (Data0 != null)
                    {
                        Data0[i].x = h1.x + -h2.y;
                        Data0[i].y = h1.y + h2.x;
                        Data0[i].z = h3.x + -h4.y;
                        Data0[i].w = h3.y + h4.x;
                    }

                    if (Data1 != null)
                    {
                        k1 = m_ktable1[i];
                        k2 = m_ktable2[i];

                        n1.x = -(k1.x * h1.y) - k1.y * h1.x;
                        n1.y = k1.x * h1.x - k1.y * h1.y;

                        n2.x = -(k2.x * h2.y) - k2.y * h2.x;
                        n2.y = k2.x * h2.x - k2.y * h2.y;

                        Data1[i].x = n1.x * k1.z;
                        Data1[i].y = n1.y * k1.z;
                        Data1[i].z = n2.x * k2.z;
                        Data1[i].w = n2.y * k2.z;
                    }

                    if (Data2 != null)
                    {
                        k3 = m_ktable3[i];
                        k4 = m_ktable4[i];

                        n3.x = -(k3.x * h3.y) - k3.y * h3.x;
                        n3.y = k3.x * h3.x - k3.y * h3.y;

                        n4.x = -(k4.x * h4.y) - k4.y * h4.x;
                        n4.y = k4.x * h4.x - k4.y * h4.y;

                        Data2[i].x = n3.x * k3.z;
                        Data2[i].y = n3.y * k3.z;
                        Data2[i].z = n4.x * k4.z;
                        Data2[i].w = n4.y * k4.z;
                    }

                }
            }

            //Debug.Log("InitSpectrum 4 grids time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

        void CreateKTables(Vector4 inverseGridSizes)
        {

            int size = Size;
            float ifsize = 1.0f / (float)size;
            int grids = NumGrids;

            if(grids > 0)
                m_ktable1 = new Vector3[size * size];
            if (grids > 1)
                m_ktable2 = new Vector3[size * size];
            if (grids > 2)
                m_ktable3 = new Vector3[size * size];
            if (grids > 3)
                m_ktable4 = new Vector3[size * size];

            int i;
            Vector2 uv, st, k1, k2, k3, k4;
            float K1, K2, K3, K4, IK1, IK2, IK3, IK4;

            //float t = Time.realtimeSinceStartup;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {

                    uv.x = x * ifsize;
                    uv.y = y * ifsize;

                    st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
                    st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

                    i = x + y * size;

                    if (grids > 0)
                    {
                        k1.x = st.x * inverseGridSizes.x;
                        k1.y = st.y * inverseGridSizes.x;
                        K1 = Mathf.Sqrt(k1.x * k1.x + k1.y * k1.y);
                        IK1 = K1 == 0.0f ? 0.0f : 1.0f / K1;

                        m_ktable1[i].x = k1.x;
                        m_ktable1[i].y = k1.y;
                        m_ktable1[i].z = IK1;
                    }

                    if (grids > 1)
                    {
                        k2.x = st.x * inverseGridSizes.y;
                        k2.y = st.y * inverseGridSizes.y;
                        K2 = Mathf.Sqrt(k2.x * k2.x + k2.y * k2.y);
                        IK2 = K2 == 0.0f ? 0.0f : 1.0f / K2;

                        m_ktable2[i].x = k2.x;
                        m_ktable2[i].y = k2.y;
                        m_ktable2[i].z = IK2;
                    }

                    if (grids > 2)
                    {
                        k3.x = st.x * inverseGridSizes.z;
                        k3.y = st.y * inverseGridSizes.z;
                        K3 = Mathf.Sqrt(k3.x * k3.x + k3.y * k3.y);
                        IK3 = K3 == 0.0f ? 0.0f : 1.0f / K3;

                        m_ktable3[i].x = k3.x;
                        m_ktable3[i].y = k3.y;
                        m_ktable3[i].z = IK3;
                    }

                    if (grids > 3)
                    {
                        k4.x = st.x * inverseGridSizes.w;
                        k4.y = st.y * inverseGridSizes.w;
                        K4 = Mathf.Sqrt(k4.x * k4.x + k4.y * k4.y);
                        IK4 = K4 == 0.0f ? 0.0f : 1.0f / K4;

                        m_ktable4[i].x = k4.x;
                        m_ktable4[i].y = k4.y;
                        m_ktable4[i].z = IK4;
                    }

                }

            }

            //Debug.Log("Create KTable time = " + (Time.realtimeSinceStartup - t) * 1000.0f);

        }

    }

}
