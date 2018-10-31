using UnityEngine;
using System.Collections.Generic;

namespace Ceto
{

	public class FreeCam : MonoBehaviour 
	{

		public float m_speed = 50.0f;

		public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
		public RotationAxes axes = RotationAxes.MouseXAndY;
		public float sensitivityX = 15F;
		public float sensitivityY = 15F;
		
		public float minimumX = -360F;
		public float maximumX = 360F;
		
		public float minimumY = -60F;
		public float maximumY = 60F;
		
		public float rotationY = 0F;

		bool m_takeMouseInput = true;

		void Start () 
		{

			transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
		
		}

		void OnGUI()
		{
			
			if(Event.current == null) return;
			
			if(Event.current.isMouse)
				m_takeMouseInput = true;
			else
				m_takeMouseInput = false;
			
		}
		
		void Update () 
		{

			float speed = m_speed;

			if(Input.GetKey(KeyCode.Space)) speed *= 10.0f;
			
			Vector3 move = new Vector3(0,0,0);
			
			//move left
			if(Input.GetKey(KeyCode.A))
				move = new Vector3(-1,0,0) * Time.deltaTime * speed;
			
			//move right
			if(Input.GetKey(KeyCode.D))
				move = new Vector3(1,0,0) * Time.deltaTime * speed;
			
			//move forward
			if(Input.GetKey(KeyCode.W))
				move = new Vector3(0,0,1) * Time.deltaTime * speed;
			
			//move back
			if(Input.GetKey(KeyCode.S))
				move = new Vector3(0,0,-1) * Time.deltaTime * speed;
			
			//move up
			if(Input.GetKey(KeyCode.E))
				move = new Vector3(0,-1,0) * Time.deltaTime * speed;
			
			//move down
			if(Input.GetKey(KeyCode.Q))
				move = new Vector3(0,1,0) * Time.deltaTime * speed;


			transform.Translate(move);

			if(m_takeMouseInput)
			{

				if (axes == RotationAxes.MouseXAndY)
				{
					float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
					
					rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
					rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
					
					transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
				}
				else if (axes == RotationAxes.MouseX)
				{
					transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
				}
				else
				{
					rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
					rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
					
					transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
				}
			}

		
		}
	}

}
