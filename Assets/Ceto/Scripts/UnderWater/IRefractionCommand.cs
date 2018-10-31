
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;


namespace Ceto
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRefractionCommand
    {

        /// <summary>
        /// Disables the copy depth command.
        /// Use this if providing your own depth buffer grab.
        /// </summary>
        bool DisableCopyDepthCmd { get;  set; }

        /// <summary>
        /// Disables the normal fade command.
        /// Used for the caustics.
        /// </summary>
        bool DisableNormalFadeCmd { get;  set; }

        /// <summary>
        /// Remove the command buffer from all the cameras.
        /// </summary>
        void ClearCommands();

        /// <summary>
        /// Create or remove commands if needed.
        /// </summary>
        void UpdateCommands();


    }

}
