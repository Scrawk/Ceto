using UnityEngine;
using System;

namespace Ceto
{
	
	/// <summary>
	/// 
	/// </summary>
	public class PhillipsSpectrum : ISpectrum
	{

        readonly float GRAVITY = SpectrumTask.GRAVITY;

        readonly float AMP = 0.02f;

        readonly float WindSpeed;

        readonly Vector2 WindDir;

        readonly float length2, dampedLength2;

		public PhillipsSpectrum(float windSpeed, float windDir)
		{
            WindSpeed = windSpeed;

            float theta = windDir * Mathf.PI / 180.0f;
            WindDir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

            float L = WindSpeed * WindSpeed / GRAVITY;
            length2 = L * L;

            float damping = 0.001f;
            dampedLength2 = length2 * damping * damping;

        }
		
		public float Spectrum(float kx, float kz)
		{

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
