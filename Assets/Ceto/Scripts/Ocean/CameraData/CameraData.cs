using UnityEngine;

using System.Collections.Generic;

namespace Ceto
{

    /// <summary>
    /// Holds all the data for a camera. 
    /// Each camera rendering the ocean has its own copy.
    /// </summary>
    public class CameraData
    {
        public bool checkedForSettings;
        public OceanCameraSettings settings;
        public MaskData mask;
        public DepthData depth;
        public WaveOverlayData overlay;
        public ProjectionData projection;
        public ReflectionData reflection;
    }


}