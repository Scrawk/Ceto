using UnityEngine;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{

	public class Quit : MonoBehaviour 
	{

		public KeyCode quitKey = KeyCode.Escape;

		void OnGUI() 
		{

			if(Input.GetKeyDown(quitKey))
				Application.Quit();
		
		}
	}

}
