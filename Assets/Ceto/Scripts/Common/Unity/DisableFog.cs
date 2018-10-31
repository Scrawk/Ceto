using UnityEngine;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{

	public class DisableFog : MonoBehaviour 
	{

		bool revertFogState;

		void Start()
		{

		}
		
		void OnPreRender () 
		{
			revertFogState = RenderSettings.fog;
			RenderSettings.fog = false;
		}
		
		void OnPostRender () 
		{
			RenderSettings.fog = revertFogState;
		}
	}

}
