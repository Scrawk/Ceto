using UnityEngine;
using System.Collections;

namespace Ceto
{

    /// <summary>
    /// Projection data.
    /// </summary>
    public class ProjectionData : ViewData
    {

        /// <summary>
        /// If this camera has been checked if the 
        /// projection position needs to be flipped.
        /// </summary>
        public bool checkedForFlipping;
        
        /// <summary>
        /// The projector view projection matrix.
        /// </summary>
        public Matrix4x4 projectorVP;

        /// <summary>
        /// The projector interpolation matrix.
        /// </summary>
        public Matrix4x4 interpolation;

    }

}
