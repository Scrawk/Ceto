using UnityEngine;
using System.Collections;

namespace Ceto
{

    //[AddComponentMenu("Ceto/DisplayTexture")]
    [RequireComponent(typeof(Camera))]
    public class DisplayTexture : MonoBehaviour
    {

        public enum DISPLAY
        {
            NONE,
            OVERLAY_HEIGHT, OVERLAY_NORMAL, OVERLAY_FOAM, OVERLAY_CLIP,
            REFLECTION0, REFLECTION1, OCEAN_MASK0, OCEAN_MASK1, OCEAN_DEPTH0, OCEAN_DEPTH1,
            WAVE_SLOPEMAP0, WAVE_SLOPEMAP1,
            WAVE_DISPLACEMENTMAP0, WAVE_DISPLACEMENTMAP1,
            WAVE_DISPLACEMENTMAP2, WAVE_DISPLACEMENTMAP3,
            WAVE_FOAM0, WAVE_FOAM1
        };

        public bool enlarge;

        public DISPLAY display = DISPLAY.NONE;

        void Start()
        {

        }

        void OnGUI()
        {

            if (Ocean.Instance == null) return;

            Camera cam = GetComponent<Camera>();

            CameraData data = Ocean.Instance.FindCameraData(cam);

            if (data == null) return;

            Texture tex = FindTexture(data, cam);

            if (tex == null) return;

            int width, height;

            if ((tex.width == Screen.width && tex.height == Screen.height) || (tex.width == Screen.width / 2 && tex.height == Screen.height / 2))
            {
                width = Screen.width / ((enlarge) ? 2 : 3);
                height = Screen.height / ((enlarge) ? 2 : 3);
            }
            else
            {
                width = 256 * ((enlarge) ? 2 : 1);
                height = 256 * ((enlarge) ? 2 : 1);
            }

            GUI.DrawTexture(new Rect(Screen.width - width - 5, 5, width, height), tex, ScaleMode.StretchToFill, false);

        }


        Texture FindTexture(CameraData data, Camera cam)
        {

            if (Ocean.Instance == null) return null;

            WaveSpectrum spectrum = Ocean.Instance.GetComponent<WaveSpectrum>();

            switch (display)
            {

                case DISPLAY.OVERLAY_HEIGHT:
                    return (data.overlay == null) ? null : data.overlay.height;

                case DISPLAY.OVERLAY_NORMAL:
                    return (data.overlay == null) ? null : data.overlay.normal;

                case DISPLAY.OVERLAY_FOAM:
                    return (data.overlay == null) ? null : data.overlay.foam;

                case DISPLAY.OVERLAY_CLIP:
                    return (data.overlay == null) ? null : data.overlay.clip;

                case DISPLAY.REFLECTION0:
                    return (data.reflection == null) ? null : data.reflection.target0;

                case DISPLAY.REFLECTION1:
                    return (data.reflection == null) ? null : data.reflection.target1;

                case DISPLAY.OCEAN_MASK0:
                    return (data.mask == null) ? null : data.mask.target0;

                case DISPLAY.OCEAN_MASK1:
                    return (data.mask == null) ? null : data.mask.target1;

                case DISPLAY.OCEAN_DEPTH0:
                    return (data.depth == null) ? null : data.depth.target0;

                case DISPLAY.OCEAN_DEPTH1:
                    return (data.depth == null) ? null : data.depth.target1;

                case DISPLAY.WAVE_SLOPEMAP0:
                    return (spectrum == null) ? null : spectrum.SlopeMaps[0];

                case DISPLAY.WAVE_SLOPEMAP1:
                    return (spectrum == null) ? null : spectrum.SlopeMaps[1];

                case DISPLAY.WAVE_DISPLACEMENTMAP0:
                    return (spectrum == null) ? null : spectrum.DisplacementMaps[0];

                case DISPLAY.WAVE_DISPLACEMENTMAP1:
                    return (spectrum == null) ? null : spectrum.DisplacementMaps[1];

                case DISPLAY.WAVE_DISPLACEMENTMAP2:
                    return (spectrum == null) ? null : spectrum.DisplacementMaps[2];

                case DISPLAY.WAVE_DISPLACEMENTMAP3:
                    return (spectrum == null) ? null : spectrum.DisplacementMaps[3];

                case DISPLAY.WAVE_FOAM0:
                    return (spectrum == null) ? null : spectrum.FoamMaps[0];

                case DISPLAY.WAVE_FOAM1:
                    return (spectrum == null) ? null : spectrum.FoamMaps[1];

                default:
                    return null;
            }

        }

    }

}













