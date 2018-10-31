using UnityEngine;
using System;

namespace Ceto
{

    public class PhillipsSpectrumConditionKey : WaveSpectrumConditionKey
    {


        public float WindSpeed { get; private set; }

        public PhillipsSpectrumConditionKey(float windSpeed, int size, float windDir, SPECTRUM_TYPE spectrumType, int numGrids)
            : base(size, windDir, spectrumType, numGrids)
        {

            WindSpeed = windSpeed;

        }

        protected override bool Matches(WaveSpectrumConditionKey k)
        {
            PhillipsSpectrumConditionKey key = k as PhillipsSpectrumConditionKey;

            if (key == null) return false;
            if (WindSpeed != key.WindSpeed) return false;

            return true;
            
        }

        protected override int AddToHashCode(int hashcode)
        {
            hashcode = (hashcode * 37) + WindSpeed.GetHashCode();

            return hashcode;
        }

        public override string ToString()
        {
            return string.Format("[PhillipsSpectrumConditionKey WindSpeed={0}, Size={1}, WindDir={2}, Type={3}, NumGrids={4}",
                WindSpeed, Size, WindDir, SpectrumType, NumGrids);
        }

    }

}






