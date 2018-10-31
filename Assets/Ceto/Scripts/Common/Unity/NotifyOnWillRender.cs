using UnityEngine;
using System;
using System.Collections.Generic;


namespace Ceto.Common.Unity.Utility
{

    /// <summary>
    /// Allows a list of functions to be added to a gameobject.
    /// When the object gets rendered each function is called.
    /// Allows for some custom code to run before rendering.
    /// </summary>
	public class NotifyOnWillRender : NotifyOnEvent
	{
	
        /// <summary>
        /// Called when this gameobject gets rendered.
        /// </summary>
		void OnWillRenderObject()
		{
			OnEvent();
		}


	}
}
