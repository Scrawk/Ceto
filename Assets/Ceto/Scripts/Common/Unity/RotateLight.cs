using UnityEngine;
using System.Collections;

namespace Ceto.Common.Unity.Utility
{
    public class RotateLight : MonoBehaviour
    {

        public float speed = 50.0f;

		public Vector3 axis = new Vector3(1,0,0);

		public KeyCode decrementKey = KeyCode.KeypadMinus;

		public KeyCode incrementKey = KeyCode.KeypadPlus;

        // Use this for initialization
        void Start()
        {

        }

        void Update()
        {

			float dt = Time.deltaTime * speed;

			Vector3 v = new Vector3(dt,dt,dt);

			if (Input.GetKey(decrementKey))
            {
				v.x *= -axis.x;
				v.y *= -axis.y;
				v.z *= -axis.z;

				transform.Rotate(v);
            }

			if (Input.GetKey(incrementKey))
            {
				v.x *= axis.x;
				v.y *= axis.y;
				v.z *= axis.z;
				
				transform.Rotate(v);
            }

        }
    }
}