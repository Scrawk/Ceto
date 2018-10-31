using UnityEngine;
using System.Collections.Generic;

namespace Ceto
{

	[AddComponentMenu("Ceto/Buoyancy/Buoyancy")]
	public class Buoyancy : MonoBehaviour 
	{

		public enum MASS_UNIT { KILOGRAMS, TENS_OF_KILOGRAMS, TONNES, TENS_OF_TONNES };

		float DENSITY_WATER = 999.97f;

		public float radius = 0.5f;

		[Range(100.0f, 10000.0f)]
		public float density = 400.0f;

		[Range(0.0f, 100.0f)]
		public float stickyness = 0.1f;

		public MASS_UNIT unit = MASS_UNIT.TENS_OF_TONNES;

		public float dragCoefficient = 0.3f;

		public bool PartOfStructure { get; set; }

		public float Volume { get; private set; }

		public float SubmergedVolume { get; private set; }

		public float PercentageSubmerged { get { return SubmergedVolume / Volume; } }

		public float SurfaceArea { get; private set; }

		public float Mass { get; private set; }

		public float WaterHeight { get;  private set; }

		public Vector3 BuoyantForce { get; private set; }

		public Vector3 DragForce { get; private set; }

		public Vector3 Stickyness { get; private set; }

		public Vector3 TotalForces { get { return BuoyantForce + DragForce + Stickyness; } }

		void Start () 
		{
		
			UpdateProperties();
			
		}

		void FixedUpdate()
		{

			if(PartOfStructure) return;

			Rigidbody body = GetComponent<Rigidbody>();

			if(body == null)
				body = gameObject.AddComponent<Rigidbody>();

			body.mass = Mass;

			UpdateProperties();
			UpdateForces(body);

			body.AddForce(TotalForces);

		}

		public void UpdateProperties()
		{

			Volume = (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(radius, 3);
			
			Mass = Volume * density * GetUnitScale();
			
			SurfaceArea = 4.0f * Mathf.PI * Mathf.Pow(radius, 2);

		}
		
		public void UpdateForces(Rigidbody body) 
		{

			if(Ocean.Instance == null)
			{
				BuoyantForce = Vector3.zero;
				DragForce = Vector3.zero;
				Stickyness = Vector3.zero;
				return;
			}

			Vector3 pos = transform.position;

			WaterHeight = Ocean.Instance.QueryWaves(pos.x, pos.z);

			CalculateSubmersion(radius, pos.y);

			float unitScale = GetUnitScale();

			float Fb = DENSITY_WATER * unitScale * SubmergedVolume;

			BuoyantForce = Physics.gravity * -Fb;

			Vector3 velocity = body.velocity;

			float vm = velocity.magnitude;
			velocity = velocity.normalized * vm * vm * -1.0f;

			DragForce = 0.5f * dragCoefficient * DENSITY_WATER * unitScale * SubmergedVolume * velocity;

			Stickyness = Vector3.up * (WaterHeight-pos.y) * Mass * stickyness;

		}

		void CalculateSubmersion(float r, float y)
		{

			float h = WaterHeight - (y - radius);

			float d = 2.0f * r - h;

			if(d <= 0.0f)
			{
				SubmergedVolume = Volume;
				return;
			}
			else if(d > 2.0f * r)
			{
				SubmergedVolume = 0.0f;
				return;
			}

			float c = Mathf.Sqrt(h * d);

			SubmergedVolume = Mathf.PI / 6.0f * h * ((3.0f * c * c) + (h * h));

		}

		float GetUnitScale()
		{

			switch((int)unit)
			{
			case (int)MASS_UNIT.KILOGRAMS:
				return 1.0f;

			case (int)MASS_UNIT.TENS_OF_KILOGRAMS:
				return 0.1f;

			case (int)MASS_UNIT.TONNES:
				return 0.001f;

			case (int)MASS_UNIT.TENS_OF_TONNES:
				return 0.0001f;

			}

			return 1.0f;

		}

		void OnDrawGizmos() 
		{
			if(!enabled) return;

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, radius);
		}

		
	}
	
}













