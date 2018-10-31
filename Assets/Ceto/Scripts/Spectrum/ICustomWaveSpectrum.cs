using System;
using UnityEngine;

namespace Ceto
{
	//See the CustomWaveSpectrumExample script for a example of how to implement this.
    public interface ICustomWaveSpectrum
    {

        bool MultiThreadTask { get; }

        WaveSpectrumConditionKey CreateKey(int size, float windDir, SPECTRUM_TYPE spectrumType, int numGrids);

        ISpectrum CreateSpectrum(WaveSpectrumConditionKey key);

        Vector4 GetGridSizes(int numGrids);

        Vector4 GetChoppyness(int numGrids);

        Vector4 GetWaveAmps(int numGrids);

    }

}