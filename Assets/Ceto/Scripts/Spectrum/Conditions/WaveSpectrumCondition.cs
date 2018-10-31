using UnityEngine;
using System;

namespace Ceto
{

	public abstract class WaveSpectrumCondition  
	{

        public bool Done { get; set; }

        public Vector4 GridSizes { get; protected set; }

        public Vector4 Choppyness { get; protected set; }

        public Vector4 WaveAmps { get; protected set; }

        public WaveSpectrumConditionKey Key { get; protected set; }

		public Texture2D Spectrum01 { get; private set; }

		public Color[] SpectrumData01 { get; private set; }

		public Texture2D Spectrum23 { get; private set; }

		public Color[] SpectrumData23 { get; private set; }

        public Texture2D WTable { get; private set; }

        public Color[] WTableData { get; private set; }

		public int LastUpdated { get; set; }

		public bool SupportsJacobians { get; protected set; }

        public WaveSpectrumCondition(int size, int numGrids)
		{

            GridSizes = Vector4.one;
            Choppyness = Vector4.one;
            WaveAmps = Vector4.one;

			SpectrumData01 = new Color[size*size];

            if(numGrids > 2)
			    SpectrumData23 = new Color[size*size];

            WTableData = new Color[size * size];

			LastUpdated = -1;
			SupportsJacobians = true;

		}

        public abstract SpectrumTask GetCreateSpectrumConditionTask();

        public InitSpectrumDisplacementsTask GetInitSpectrumDisplacementsTask(DisplacementBufferCPU buffer, float time)
        {
            return new InitSpectrumDisplacementsTask(buffer, this, time);
        }

        public Vector4 InverseGridSizes()
        {

            float factor = 2.0f * Mathf.PI * Key.Size;
            return new Vector4(factor / GridSizes.x,
                               factor / GridSizes.y,
                               factor / GridSizes.z,
                               factor / GridSizes.w);

        }

        public void Release()
        {
            if (Spectrum01 != null)
            {
                UnityEngine.Object.Destroy(Spectrum01);
                Spectrum01 = null;
            }

            if (Spectrum23 != null)
            {
                UnityEngine.Object.Destroy(Spectrum23);
                Spectrum23 = null;
            }

            if (WTable != null)
            {
                UnityEngine.Object.Destroy(WTable);
                WTable = null;
            }
        }

		public void CreateTextures()
		{

            if (Spectrum01 == null)
            {
                Spectrum01 = new Texture2D(Key.Size, Key.Size, TextureFormat.RGBAFloat, false, true);
                Spectrum01.filterMode = FilterMode.Point;
                Spectrum01.wrapMode = TextureWrapMode.Repeat;
				Spectrum01.hideFlags = HideFlags.HideAndDontSave;
                Spectrum01.name = "Ceto Spectrum01 Condition";
            }

            if (Spectrum23 == null && Key.NumGrids > 2)
            {
                Spectrum23 = new Texture2D(Key.Size, Key.Size, TextureFormat.RGBAFloat, false, true);
                Spectrum23.filterMode = FilterMode.Point;
                Spectrum23.wrapMode = TextureWrapMode.Repeat;
				Spectrum23.hideFlags = HideFlags.HideAndDontSave;
                Spectrum23.name = "Ceto Spectrum23 Condition";
            }

            if(WTable == null)
            {
                WTable = new Texture2D(Key.Size, Key.Size, TextureFormat.RGBAFloat, false, true);
                WTable.filterMode = FilterMode.Point;
                WTable.wrapMode = TextureWrapMode.Clamp;
                WTable.hideFlags = HideFlags.HideAndDontSave;
                WTable.name = "Ceto Wave Spectrum GPU WTable";
            }

		}

        public void Apply(Color[] spectrum01, Color[] spectrum23, Color[] wtable)
        {

            if (Spectrum01 != null && spectrum01 != null)
            {
                Spectrum01.SetPixels(spectrum01);
                Spectrum01.Apply();

                System.Array.Copy(spectrum01, SpectrumData01, spectrum01.Length);
            }

            if (Spectrum23 != null && spectrum23 != null)
            {
                Spectrum23.SetPixels(spectrum23);
                Spectrum23.Apply();

                System.Array.Copy(spectrum23, SpectrumData23, spectrum23.Length);
            }

            if (WTable != null && wtable != null)
            {
                WTable.SetPixels(wtable);
                WTable.Apply();

                System.Array.Copy(wtable, WTableData, wtable.Length);
            }
        }

	}

}












