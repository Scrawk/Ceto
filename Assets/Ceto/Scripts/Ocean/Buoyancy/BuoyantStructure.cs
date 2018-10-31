using UnityEngine;
using System.Collections.Generic;


namespace Ceto
{

	[AddComponentMenu("Ceto/Buoyancy/BuoyantStructure")]
	public class BuoyantStructure : MonoBehaviour 
	{

		public float maxAngularVelocity = 0.05f;

		Buoyancy[] m_buoyancy;

		void Start () 
		{
		
			m_buoyancy = GetComponentsInChildren<Buoyancy>();

            int count = m_buoyancy.Length;
            for (int i = 0; i < count; i++)
                m_buoyancy[i].PartOfStructure = true;

		}

		void FixedUpdate() 
		{

			Rigidbody body = GetComponent<Rigidbody>();
			
			if(body == null)
				body = gameObject.AddComponent<Rigidbody>();

			float mass = 0.0f;

            int count = m_buoyancy.Length;
            for(int i = 0; i < count; i++)
			{
				if(!m_buoyancy[i].enabled) continue;

                m_buoyancy[i].UpdateProperties();
				mass += m_buoyancy[i].Mass;
	
			}

			body.mass = mass;

			Vector3 pos = transform.position;
			Vector3 force = Vector3.zero;
			Vector3 torque = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
				if(!m_buoyancy[i].enabled) continue;

                m_buoyancy[i].UpdateForces(body);

				Vector3 p = m_buoyancy[i].transform.position;
				Vector3 f = m_buoyancy[i].TotalForces;
				Vector3 r = p-pos;

				force += f;
				torque += Vector3.Cross(r, f);

			}

			body.maxAngularVelocity = maxAngularVelocity;
			body.AddForce(force);
			body.AddTorque(torque);
		
		}

	}

}












