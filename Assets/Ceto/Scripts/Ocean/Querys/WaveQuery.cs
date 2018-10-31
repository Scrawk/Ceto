using UnityEngine;
using System.Collections.Generic;


namespace Ceto
{

	/// <summary>
	/// Query's the ocean waves for information such 
	/// as the wave height at a given location.
	/// The query class provides the settings used to 
	/// perform the query and the query stores the 
	/// information in the result struct.
	/// </summary>
	public class WaveQuery
	{

		/// <summary>
		/// Contains the result of a query.
		/// </summary>
		public struct WaveQueryResult
		{
			/// <summary>
			/// The total wave height.
			/// </summary>
			public float height;

			/// <summary>
            /// The total amount displacement on x axis.
			/// </summary>
			public float displacementX;

			/// <summary>
            /// The total amount displacement on z axis.
			/// </summary>
			public float displacementZ;

            /// <summary>
            /// The height from the overlays.
            /// </summary>
            public float overlayHeight;

            /// <summary>
            /// The raw displacement data from the spectrum grids.
            /// </summary>
            public Vector3[] displacement;

			/// <summary>
			/// If the waves have xz displacement the height
			/// has to be sampled by a series of iterations 
			/// until the correct location to sample can be 
			/// found. This is the number of iteration it 
			/// took to reach the correct position.
			/// </summary>
			public int iterations;

			/// <summary>
			/// The iterations will never end at the exact
			/// location to sample. This is the amount of
			/// error (difference) between the requested sample 
			/// position and the actual sample position.
			/// </summary>
			public float error;

			/// <summary>
			/// True if the ocean mesh is clipped at sampled position.
			/// </summary>
			public bool isClipped;

			/// <summary>
			/// This is all the overlays the have a effect on the wave 
			/// height and contain the sample point.
			/// Will be null or empty if no overlays contain point.
			/// NOT CURRENTLY SET, ALWAYS NULL.
			/// </summary>
			public IEnumerable<QueryableOverlayResult> overlays;

			/// <summary>
			/// Clear this instance.
			/// </summary>
			public void Clear()
			{
				height = 0.0f;
                overlayHeight = 0.0f;
				displacementX = 0.0f;
				displacementZ = 0.0f;
				iterations = 0;
				error = 0.0f;
				isClipped = false;
				overlays = null;

                displacement[0].x = 0.0f;
                displacement[0].y = 0.0f;
                displacement[0].z = 0.0f;

                displacement[1].x = 0.0f;
                displacement[1].y = 0.0f;
                displacement[1].z = 0.0f;

                displacement[2].x = 0.0f;
                displacement[2].y = 0.0f;
                displacement[2].z = 0.0f;

                displacement[3].x = 0.0f;
                displacement[3].y = 0.0f;
                displacement[3].z = 0.0f;
			}
			
		}

		/// <summary>
		/// The minimum allowable value for minError.
		/// </summary>
		public static readonly float MIN_ERROR = 0.01f;

		/// <summary>
		/// The max iterations before the query will give up
		/// trying to find the correct location to sample.
		/// This is just a fail safe and the iterations 
		/// only reach 3-4 at most.
		/// </summary>
		public static readonly int MAX_ITERATIONS = 20;

		/// <summary>
		/// Once the iterations find a position
		/// that is at least this close to requested 
		/// position the iteration will stop and use 
		/// that position to sample from. 
		/// lower value means more accurate query but
		/// slower to calculate.
		/// </summary>
		public float minError;

		/// <summary>
		/// The requested position to sample from.
		/// </summary>
		public float posX, posZ;

		/// <summary>
		/// which spectrum grids to sample.
		/// </summary>
		public readonly bool[] sampleSpectrum;

		/// <summary>
		/// Should the overlays be sampled.
		/// </summary>
		public bool sampleOverlay;

		/// <summary>
		/// The overlays can choose to ignore the
		/// query. This will override this and 
		/// sample them anyway.
		/// </summary>
		public bool overrideIgnoreQuerys;

		/// <summary>
		/// The sample mode.
		/// </summary>
        public QUERY_MODE mode;

        /// <summary>
        /// This is not used by Ceto and will never be changed.
        /// Use it to give your query a unique id(like its index in a array)
        /// if you need to.
        /// </summary>
        public int tag;

		/// <summary>
		/// The query result.
		/// </summary>
		public WaveQueryResult result;

        public WaveQuery()
        {

            posX = 0.0f;
            posZ = 0.0f;
            minError = 0.1f;
            sampleSpectrum = new bool[] { true, true, false, false };
            sampleOverlay = true;
            mode = QUERY_MODE.POSITION;
            result.displacement = new Vector3[4];

        }

        public WaveQuery(Vector3 worldPos)
		{

			posX = worldPos.x;
			posZ = worldPos.z;
			minError = 0.1f;
			sampleSpectrum = new bool[]{true, true, false, false};
			sampleOverlay = true;
            mode = QUERY_MODE.POSITION;
            result.displacement = new Vector3[4];

        }

		public WaveQuery(float x, float z)
		{
			
			posX = x;
			posZ = z;
			minError = 0.1f;
			sampleSpectrum = new bool[]{true, true, false, false};
			sampleOverlay = true;
            mode = QUERY_MODE.POSITION;
            result.displacement = new Vector3[4];

        }

        public bool SamplesSpectrum
        {
            get { return sampleSpectrum[0] || sampleSpectrum[1] || sampleSpectrum[2] || sampleSpectrum[3]; }
        }

	}


}











