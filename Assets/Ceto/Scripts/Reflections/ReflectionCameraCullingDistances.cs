using UnityEngine;
using System.Collections;


namespace Ceto
{
	[AddComponentMenu("Ceto/Camera/ReflectionCameraCullingDistances")]
	[RequireComponent (typeof(Camera))]
	public class ReflectionCameraCullingDistances : MonoBehaviour 
	{

		public bool sphericalCulling = true;

		public float[] distances = new float[32];

		Camera m_camera;

		// Use this for initialization
		void Start () 
		{
		
			m_camera = GetComponent<Camera>();

		}
		
		// Update is called once per frame
		void Update () 
		{

			//If ocean instance null there is no ocean in the scene or
			//it has not been enabled yet. 
			if(Ocean.Instance == null || distances.Length != 32) return;

			CameraData data = Ocean.Instance.FindCameraData(m_camera);

			//If reflection data is null this camera has not rendered the ocean yet.
			if(data.reflection == null) return;

			Camera reflectionCam = data.reflection.cam;

			//Update the culling settings for the reflection cam.
			reflectionCam.layerCullDistances = distances;
			reflectionCam.layerCullSpherical = sphericalCulling;
		
		}
	}
}
