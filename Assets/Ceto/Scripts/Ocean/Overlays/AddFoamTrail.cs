using UnityEngine;
using System.Collections.Generic;


namespace Ceto
{

    /// <summary>
    /// Adds a trail of foam overlays behind a moving object
    /// </summary>
	[AddComponentMenu("Ceto/Overlays/AddFoamTrail")]
    public class AddFoamTrail : AddWaveOverlayBase, IOverlayParentBounds
    {


        /// <summary>
        /// The minimum amount the ship has not move
        /// before a overlay is created.
        /// </summary>
        readonly float MIN_MOVEMENT = 0.1f;

        /// <summary>
        /// The maximum amount of movement 
        /// used between frames. Used to prevent 
        /// a excessive number of overlays being created
        /// if the object is moved by a large amount.
        /// </summary>
        readonly float MAX_MOVEMENT = 100.0f;

        /// <summary>
        /// Rotation mode to apply to the overlays.
        /// NONE - no rotation.
        /// RANDOM - a random rotation 
        /// RELATIVE - The rotation will match the parents 
        /// current y axis rotation when created.
        /// </summary>
		public enum ROTATION { NONE, RANDOM, RELATIVE };
	    
        /// <summary>
        /// The texture to use for the foam.
        /// </summary>
		public Texture foamTexture;

        /// <summary>
        /// Should the global foam texture be applied to
        /// the foam overlays.
        /// </summary>
        public bool textureFoam = true;

        /// <summary>
        /// Rotation mode.
        /// </summary>
		public ROTATION rotation = ROTATION.RANDOM;

        /// <summary>
        /// The curve controls that overlays alpha over
        /// its life time. Allows the overlays to fade in 
        /// when created and then fade out over time.
        /// </summary>
		public AnimationCurve timeLine = DefaultCurve();

        /// <summary>
        /// How long in seconds the overlay will last.
        /// </summary>
		public float duration = 10.0f;

        /// <summary>
        /// The size of the overlay when created.
        /// </summary>
		public float size = 10.0f;

        /// <summary>
        /// The amount of spacing between each overlay.
        /// A low spacing can result in a excessive number of
        /// overlays being created. 
        /// </summary>
		public float spacing = 4.0f;

        /// <summary>
        /// The amount the overlay will expand over time.
        /// </summary>
		public float expansion = 1.0f;

        /// <summary>
        /// The amount the overlay will move over time.
        /// </summary>
		public float momentum = 1.0f;

        /// <summary>
        /// The amount the overlay will rotate over time.
        /// </summary>
		public float spin = 10.0f;

        /// <summary>
        /// If true the overlays will not be created when
        /// this objects position is above the water level.
        /// </summary>
		public bool mustBeBelowWater = true;

        /// <summary>
        /// The overlays alpha.
        /// </summary>
		[Range(0.0f, 2.0f)]
		public float alpha = 0.8f;

        /// <summary>
        /// Randomizes the spin and expansion values
        /// for each overlay created.
        /// </summary>
		[Range(0.0f, 1.0f)]
		public float jitter = 0.2f;

        /// <summary>
        /// The distance the overlay needs to be from the camera to be drawn.
        /// </summary>
        public float drawDistance = 200.0f;

        /// <summary>
        /// The bounding box in world space of all the overlays.
        /// </summary>
        public Bounds BoundingBox { get; private set; }
        public bool BoundsChecked { get; set; }
        public bool BoundsVisible { get; set; }

        /// <summary>
        /// This objects last position.
        /// </summary>
		Vector3 m_lastPosition;

        /// <summary>
        /// Each frame new overlays or added between the 
        /// current position and the last position.
        /// This that distance and if less than the spacing
        /// value its will be added to next frames distance value.
        /// </summary>
		float m_remainingDistance;

        /// <summary>
        /// Tmp buffer to hold removed overlays
        /// </summary>
        List<FoamOverlay> m_remove = new List<FoamOverlay>();

        /// <summary>
        /// Buffer to recycle overlays.
        /// </summary>
        LinkedList<FoamOverlay> m_pool = new LinkedList<FoamOverlay>();

        /// <summary>
        /// 
        /// </summary>
        protected override void Start()
        {

            m_lastPosition = transform.position;

        }

        /// <summary>
        /// 
        /// </summary>
		protected override void Update () 
		{

			UpdateOverlays();
			AddFoam();
			RemoveOverlays();
		
		}

        protected override void OnEnable()
        {
            base.OnEnable();

            m_lastPosition = transform.position;
        }

		/// <summary>
		/// Call to translate the overlays by this amount
		/// </summary>
		public override void Translate(Vector3 amount)
		{

			base.Translate(amount);

			m_lastPosition += amount;

		}

        /// <summary>
        /// Returns a rotation value depending on the rotation mode.
        /// </summary>
		float Rotation()
		{

			switch(rotation)
			{
			case ROTATION.NONE:
				return 0.0f;

			case ROTATION.RANDOM:
				return Random.Range(0.0f, 360.0f);

			case ROTATION.RELATIVE:
				return transform.eulerAngles.y;
			}

			return 0.0f;

		}

        /// <summary>
        /// Returans a new FoamOverlay object. Will take from the pool
        /// if not empty and reset to new values. If pool empty it will
        /// create a new object. Used to reduce memory allocations.
        /// </summary>
        FoamOverlay NewFoamOverlay(Vector3 pos, float rotation, float size, float duration, Texture texture)
        {
            FoamOverlay overlay = null;

            if (m_pool.Count > 0)
            {
                overlay = m_pool.First.Value;
                overlay.Reset(pos, Rotation(), size, duration, foamTexture);
                m_pool.RemoveFirst();
            }
            else
            {
                overlay = new FoamOverlay(pos, Rotation(), size, duration, foamTexture);
                overlay.ParentBounds = this;
            }

            return overlay;
        }

        /// <summary>
        /// Creates new overlays based on the movement from the last position.
        /// </summary>
		void AddFoam()
		{

            //If there is no ocean in scene or if the overlays 
            //duration is less than 0 dont do anything
            if (duration <= 0.0f || Ocean.Instance == null)
            {
                m_lastPosition = transform.position;
                return;
            }

            //Clamp spacing and size to 1.
			spacing = Mathf.Max(1.0f, spacing);
			size = Mathf.Max(1.0f, size);

			Vector3 pos = transform.position;
			float h = pos.y;

            //If the overlays are only to be added if the position
            //is below the water line get the wave height.
			if(mustBeBelowWater)
				h = Ocean.Instance.QueryWaves(pos.x, pos.z);

            //If the waves are below the position do nothing.
            if (h < pos.y)
			{
				m_lastPosition = pos;
				return;
			}

            //From here on the position is presumed to be on a flat plane.
			pos.y = 0.0f;
			m_lastPosition.y = 0.0f;

            //Find the distance travel and the direction
			Vector3 distance = m_lastPosition - pos;
			Vector3 direction = distance.normalized;
			float length = distance.magnitude;

            //Only start adding new overlays if there has 
            //been a minimum amount of movement.
			if(length < MIN_MOVEMENT) return;

            length = Mathf.Min(MAX_MOVEMENT, length);

            Vector3 momentumDir = direction * momentum;

            //Append the amount move to remaining
            //distance from last frame
			m_remainingDistance += length;

			float d = 0.0f;
			while(m_remainingDistance > spacing)
			{
                //Find the next overlay pos.
				Vector3 overlayPos = pos + direction * d;

                //Create a new overlay and set is starting values.
				FoamOverlay overlay = NewFoamOverlay(overlayPos, Rotation(), size, duration, foamTexture);

				overlay.FoamTex.alpha = 0.0f;
                overlay.FoamTex.textureFoam = textureFoam;
				overlay.Momentum = momentumDir;
				overlay.Spin = (Random.value > 0.5f) ? -spin : spin;
				overlay.Expansion = expansion;

                if (jitter > 0.0f)
                {
                    overlay.Spin *= 1.0f + Random.Range(-1.0f, 1.0f) * jitter;
                    overlay.Expansion *= 1.0f + Random.Range(-1.0f, 1.0f) * jitter;
                }

                //Add to list and add to ocean overlay manager.
                //The overlay manager will render the overlay into the buffer.
				m_overlays.Add(overlay);
				Ocean.Instance.OverlayManager.Add(overlay);

                //Decrement remaining distance by the spacing.
				m_remainingDistance -= spacing;
				d += spacing;
			}

			m_lastPosition = pos;

		}

        /// <summary>
        /// Need to update the overlays each frame.
        /// </summary>
        void UpdateOverlays()
        {

            float xmin = float.PositiveInfinity;
            float zmin = float.PositiveInfinity;
            float xmax = float.NegativeInfinity;
            float zmax = float.NegativeInfinity;

            for (int i = 0; i < m_overlays.Count; i++)
            {
                WaveOverlay overlay = m_overlays[i];
      
                //Set the alpha based on the its age and curve
                float a = overlay.NormalizedAge;
                overlay.FoamTex.alpha = timeLine.Evaluate(a) * alpha;
                overlay.FoamTex.textureFoam = textureFoam;
                overlay.DrawDistance = drawDistance;
                //Updates the overlays position, rotation, expansion and its bounding box.
                overlay.UpdateOverlay();

                float half = Mathf.Max(overlay.HalfSize.x, overlay.HalfSize.y);

                for(int j = 0; j < 4; j++)
                {
                    if (overlay.Position.x - half < xmin) xmin = overlay.Position.x - half;
                    if (overlay.Position.x + half> xmax) xmax = overlay.Position.x + half;
                    if (overlay.Position.z - half < zmin) zmin = overlay.Position.z - half;
                    if (overlay.Position.z + half> zmax) zmax = overlay.Position.z + half;
                }

            }

            //Caculate the bounds of all the overlays.
            //This will be the overlays parents bounds.

            float range = 0.0f;
            float level = 0.0f;
    
            if (Ocean.Instance != null)
            {
                level = Ocean.Instance.level;
                range = Ocean.Instance.FindMaxDisplacement(true);
            }

            Vector3 size;
            size.x = xmax - xmin;
            size.y = range * 2.0f;
            size.z = zmax - zmin;

            Vector3 center;
            center.x = (xmax + xmin) * 0.5f;
            center.y = level;
            center.z = (zmax + zmin) * 0.5f;

            if (float.IsInfinity(size.x) || float.IsInfinity(size.y) || float.IsInfinity(size.z))
                BoundingBox = new Bounds();
            else
                BoundingBox = new Bounds(center, size);

        }

        /// <summary>
        /// Remove any overlays that have a age longer that there duration.
        /// </summary>
        void RemoveOverlays()
        {

            m_remove.Clear();
           
            for (int i = 0; i < m_overlays.Count; i++)
            {

                FoamOverlay overlay = m_overlays[i] as FoamOverlay;

                if (overlay.Age >= overlay.Duration)
                {
                    m_remove.Add(overlay);
                    //Set kill to true to remove from oceans overlay manager.
                    overlay.Kill = true;
                }
            }

            for (int i = 0; i < m_remove.Count; i++)
            {
                m_overlays.Remove(m_remove[i]);
                m_pool.AddLast(m_remove[i]);
            }

        }

        void OnDrawGizmos()
        {
            if (!enabled) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2.0f);

            /*
            for (int i = 0; i < m_overlays.Count; i++)
            {
                Gizmos.DrawLine(m_overlays[i].Corners[0], m_overlays[i].Corners[1]);
                Gizmos.DrawLine(m_overlays[i].Corners[1], m_overlays[i].Corners[2]);
                Gizmos.DrawLine(m_overlays[i].Corners[2], m_overlays[i].Corners[3]);
                Gizmos.DrawLine(m_overlays[i].Corners[3], m_overlays[i].Corners[0]);
            }
            */

        }

	}

}







