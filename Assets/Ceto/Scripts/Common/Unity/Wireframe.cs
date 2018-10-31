using UnityEngine;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{

	public class Wireframe : MonoBehaviour 
	{

		public bool on = false;

		public KeyCode toggleKey = KeyCode.F2;

		void Start()
		{

		}

		void Update()
		{

			if(Input.GetKeyDown(toggleKey)) on = !on;

		}

		void OnPreRender() 
		{
			if(on)
				GL.wireframe = true;
		}

		void OnPostRender() 
		{
			if(on)
				GL.wireframe = false;
		}

	}

}
