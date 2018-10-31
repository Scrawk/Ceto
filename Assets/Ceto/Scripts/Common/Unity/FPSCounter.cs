using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{
    public class FPSCounter : MonoBehaviour
    {


        float updateInterval = 0.5f;

        float accum = 0; // FPS accumulated over the interval
        float frames = 0; // Frames drawn over the interval
        float timeleft = 0; // Left time for current interval

		public float FrameRate { get; set; }

        void Start()
        {
            timeleft = updateInterval;
        }

        void Update()
        {
            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;

            if (timeleft <= 0.0f)
            {
				FrameRate = accum / frames;
                timeleft = updateInterval;
                accum = 0;
                frames = 0;
            }
        }
    }
}
