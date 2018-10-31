using UnityEngine;
using System.Collections.Generic;


namespace Ceto
{

    /// <summary>
    /// Adds a jet of foam in the direction game object is facing.
    /// </summary>
	[AddComponentMenu("Ceto/Overlays/AddFoamThruster")]
    public class AddFoamThruster : AddWaveOverlayBase, IOverlayParentBounds
    {

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
        public float duration = 4.0f;

        /// <summary>
        /// The size of the overlay when created.
        /// </summary>
        public float size = 2.0f;

        /// <summary>
        /// Time in milliseconds a new overlay is created.
        /// </summary>
        [Range(16.0f, 1000.0f)]
        public float rate = 128.0f;

        /// <summary>
        /// The amount the overlay will expand over time.
        /// </summary>
        public float expansion = 4.0f;

        /// <summary>
        /// The amount the overlay will move over time.
        /// </summary>
        public float momentum = 10.0f;

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
        /// 
        /// </summary>
        float m_lastTime;

        /// <summary>
        /// 
        /// </summary>
        float m_remainingTime;

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

            m_lastTime = Time.time;

        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {

            UpdateOverlays();
            AddFoam();
            RemoveOverlays();

        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_lastTime = Time.time;
        }

		/// <summary>
		/// Call to translate the overlays by this amount
		/// </summary>
		public override void Translate(Vector3 amount)
		{
			
			base.Translate(amount);

		}

        /// <summary>
        /// Returns a rotation value depending on the rotation mode.
        /// </summary>
        float Rotation()
        {

            switch (rotation)
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
                m_lastTime = Time.time;
                return;
            }

            //Clamp size.
            size = Mathf.Max(1.0f, size);

            float h = transform.position.y;
            Vector3 pos = transform.position;
            Vector3 dir = transform.forward;

            //If the overlays are only to be added if the position
            //is below the water line get the wave height.
            if (mustBeBelowWater)
                h = Ocean.Instance.QueryWaves(pos.x, pos.z);
	
            //If the waves are below the position do nothing.
            //If the forward dir is straight up or down do nothing.
            if (h < pos.y || (dir.x == 0.0f && dir.z == 0.0f))
            {
                m_lastTime = Time.time;
                return;
            }

			float delta = Time.time - m_lastTime;
            dir = dir.normalized;
            pos.y = 0.0f;

            Vector3 momentumDir = dir * momentum;

            m_remainingTime += delta;

			//rate in seconds
			float r = rate / 1000.0f;

            float d = 0.0f;
            while (m_remainingTime > r)
            {
                //Find the next overlay pos.
                Vector3 overlayPos = pos + dir * d;

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

                //Decrement remaining distance by the rate.
                m_remainingTime -= r;
                d += r;
            }

            m_lastTime = Time.time;

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

                for (int j = 0; j < 4; j++)
                {
                    if (overlay.Position.x - half < xmin) xmin = overlay.Position.x - half;
                    if (overlay.Position.x + half > xmax) xmax = overlay.Position.x + half;
                    if (overlay.Position.z - half < zmin) zmin = overlay.Position.z - half;
                    if (overlay.Position.z + half > zmax) zmax = overlay.Position.z + half;
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







