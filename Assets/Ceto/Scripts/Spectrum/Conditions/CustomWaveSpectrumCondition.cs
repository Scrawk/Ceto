using UnityEngine;
using System;

namespace Ceto
{

    public class CustomWaveSpectrumCondition : WaveSpectrumCondition
    {

        ICustomWaveSpectrum m_custom;

        public CustomWaveSpectrumCondition(ICustomWaveSpectrum custom, int size, float windDir, int numGrids)
            : base(size, numGrids)
        {

            if (numGrids < 1 || numGrids > 4)
                throw new ArgumentException("UCustomSpectrumCondition must have 1 to 4 grids not " + numGrids);

            m_custom = custom;

            Key = m_custom.CreateKey(size, windDir, SPECTRUM_TYPE.CUSTOM, numGrids);

            GridSizes = m_custom.GetGridSizes(numGrids);
            Choppyness = m_custom.GetChoppyness(numGrids);
            WaveAmps = m_custom.GetWaveAmps(numGrids);

        }

        public override SpectrumTask GetCreateSpectrumConditionTask()
        {
            ISpectrum spectrum = m_custom.CreateSpectrum(Key);

            bool multiThreadTask = m_custom.MultiThreadTask;

            return new SpectrumTask(this, multiThreadTask, new ISpectrum[] { spectrum, spectrum, spectrum, spectrum });
        }

    }

}












