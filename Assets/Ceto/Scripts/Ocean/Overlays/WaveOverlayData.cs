using UnityEngine;
using System.Collections;

namespace Ceto
{
    /// <summary>
    /// Holds the overlay maps and if
    /// overlays have been updated this frame.
    /// </summary>
    public class WaveOverlayData : ViewData
    {

        /// <summary>
        /// The texture the normal overlays are rendered into.
        /// </summary>
        public RenderTexture normal;

        /// <summary>
        /// The texture the height overlays are rendered into.
        /// </summary>
        public RenderTexture height;

        /// <summary>
        /// The texture the foam overlays are rendered into.
        /// </summary>
        public RenderTexture foam;

        /// <summary>
        /// The texture the clip overlays are rendered into.
        /// </summary>
        public RenderTexture clip;
    }

}
