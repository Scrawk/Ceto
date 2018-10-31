using UnityEngine;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{

	public class DisableGameObject : MonoBehaviour 
	{

		void Update () 
		{
			gameObject.SetActive(false);
		}

		void OnEnable()
		{
			gameObject.SetActive(false);
		}
	}

}
