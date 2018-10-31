using UnityEngine;
using System;

namespace Ceto
{
	
	public class PhillipsSpectrumCondition : WaveSpectrumCondition
	{

		public PhillipsSpectrumCondition(int size, float windSpeed, float windDir, float waveAge, int numGrids)
			: base(size, numGrids)
		{

            if (numGrids < 1 || numGrids > 4)
                throw new ArgumentException("PhillipsSpectrumCondition must have 1 to 4 grids not " + numGrids);

            Key = new PhillipsSpectrumConditionKey(windSpeed, size, windDir, SPECTRUM_TYPE.PHILLIPS, numGrids);

            if (numGrids == 1)
            {
                GridSizes = new Vector4(217, 1, 1, 1);
                Choppyness = new Vector4(1.5f, 1.0f, 1.0f, 1.0f);
                WaveAmps = new Vector4(0.5f, 1.0f, 1.0f, 1.0f);
            }
            else if(numGrids == 2)
            {
                GridSizes = new Vector4(217, 97, 1, 1);
                Choppyness = new Vector4(1.5f, 1.2f, 1.0f, 1.0f);
                WaveAmps = new Vector4(0.5f, 1.0f, 1.0f, 1.0f);
            }
            else if (numGrids == 3)
            {
                GridSizes = new Vector4(217, 97, 31, 1);
                Choppyness = new Vector4(1.5f, 1.2f, 1.0f, 1.0f);
                WaveAmps = new Vector4(0.5f, 1.0f, 1.0f, 1.0f);
            }
            else if (numGrids == 4)
            {
                GridSizes = new Vector4(1372, 217, 97, 31);
                Choppyness = new Vector4(1.5f, 1.2f, 1.0f, 1.0f);
                WaveAmps = new Vector4(0.25f, 0.5f, 1.0f, 1.0f);
            }

        }
		
		public override SpectrumTask GetCreateSpectrumConditionTask()
		{

            PhillipsSpectrumConditionKey key = Key as PhillipsSpectrumConditionKey;

            PhillipsSpectrum pspectrum = new PhillipsSpectrum(key.WindSpeed, key.WindDir);

            return new SpectrumTask(this, true, new ISpectrum[] { pspectrum, pspectrum, pspectrum, pspectrum });
		}
		
	}
	
}












