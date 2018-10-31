using UnityEngine;
using System.Collections;

namespace Ceto
{
	[AddComponentMenu("Ceto/Camera/OceanCameraSettings")]
	[RequireComponent (typeof(Camera))]
	public class OceanCameraSettings : MonoBehaviour 
	{

        public bool disableAllOverlays;

        public OVERLAY_MAP_SIZE heightOverlaySize = OVERLAY_MAP_SIZE.HALF;

		public OVERLAY_MAP_SIZE normalOverlaySize = OVERLAY_MAP_SIZE.FULL;

        public OVERLAY_MAP_SIZE foamOverlaySize = OVERLAY_MAP_SIZE.FULL;

        public OVERLAY_MAP_SIZE clipOverlaySize = OVERLAY_MAP_SIZE.HALF;

        public bool disableReflections;

		public LayerMask reflectionMask = 1;

        public REFLECTION_RESOLUTION reflectionResolution = REFLECTION_RESOLUTION.HALF;

        public bool disableUnderwater;

        public LayerMask oceanDepthsMask = 1;

    }
}
