using UnityEngine;
using System;

namespace Ceto
{

    public class UnifiedPhillipsSpectrumCondition : WaveSpectrumCondition
    {

        public UnifiedPhillipsSpectrumCondition(int size, float windSpeed, float windDir, float waveAge, int numGrids)
            : base(size, numGrids)
        {

            if (numGrids < 1 || numGrids > 4)
                throw new ArgumentException("UnifiedPhillipsSpectrumCondition must have 1 to 4 grids not " + numGrids);

            Key = new UnifiedSpectrumConditionKey(windSpeed, waveAge, size, windDir, SPECTRUM_TYPE.UNIFIED_PHILLIPS, numGrids);

            if (numGrids == 1)
            {
                GridSizes = new Vector4(772, 1, 1, 1);
                Choppyness = new Vector4(2.3f, 1.0f, 1.0f, 1.0f);
                WaveAmps = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            }
            else if (numGrids == 2)
            {
                GridSizes = new Vector4(772, 97, 1, 1);
                Choppyness = new Vector4(2.3f, 1.2f, 1.0f, 1.0f);
                WaveAmps = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            }
            else if (numGrids == 3)
            {
                GridSizes = new Vector4(1372, 392, 31, 1);
                Choppyness = new Vector4(2.3f, 2.1f, 1.0f, 1.0f);
                WaveAmps = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            }
            else if (numGrids == 4)
            {
                GridSizes = new Vector4(1372, 392, 31, 4);
                Choppyness = new Vector4(2.3f, 2.1f, 1.0f, 0.9f);
                WaveAmps = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            }

        }

        public override SpectrumTask GetCreateSpectrumConditionTask()
        {

            UnifiedSpectrumConditionKey key = Key as UnifiedSpectrumConditionKey;

            UnifiedSpectrum uspectrum = new UnifiedSpectrum(key.WindSpeed, key.WindDir, key.WaveAge);

            PhillipsSpectrum pspectrum = new PhillipsSpectrum(key.WindSpeed, key.WindDir);

            if (Key.NumGrids == 1)
                return new SpectrumTask(this, true, new ISpectrum[] { uspectrum, null, null, null });
            else if (Key.NumGrids == 2)
                return new SpectrumTask(this, true, new ISpectrum[] { uspectrum, pspectrum, null, null });
            else if (Key.NumGrids == 3)
                return new SpectrumTask(this, true, new ISpectrum[] { uspectrum, uspectrum, pspectrum, null });
            else //numGrids == 4 
                return new SpectrumTask(this, true, new ISpectrum[] { uspectrum, uspectrum, pspectrum, uspectrum });

        }

    }

}












