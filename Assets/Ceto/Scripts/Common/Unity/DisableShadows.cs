using UnityEngine;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{
	
	public class DisableShadows : MonoBehaviour 
	{
		
		float storedShadowDistance;
		
		void Start()
		{
			
		}
		
		void OnPreRender () 
		{
			storedShadowDistance = QualitySettings.shadowDistance;
			QualitySettings.shadowDistance = 0;
		}
		
		void OnPostRender () 
		{
			QualitySettings.shadowDistance = storedShadowDistance;
		}
	}
	
}
