using UnityEngine;
using System;
using System.Collections.Generic;

using Ceto.Common.Containers.Interpolation;
using Ceto.Common.Threading.Tasks;

namespace Ceto
{
	public static class QueryDisplacements
	{

		public readonly static int CHANNELS = 3;
		public readonly static int GRIDS = 4;

        public static void QueryWaves(WaveQuery query, int enabled, IList<InterpolatedArray2f> displacements, QueryGridScaling scaling)
		{

			if(displacements.Count != GRIDS)
				throw new InvalidOperationException("Query Displacements requires a displacement buffer for each of the " + GRIDS + " grids.");

			if(displacements[0].Channels != CHANNELS)
				throw new InvalidOperationException("Query Displacements requires displacement buffers have " + CHANNELS + " channels.");

            //Only these modes are relevant to this code.
            if (query.mode != QUERY_MODE.DISPLACEMENT && query.mode != QUERY_MODE.POSITION) return;

			float x = query.posX + scaling.offset.x;
            float z = query.posZ + scaling.offset.z;

            if (scaling.result == null)
                scaling.result = new float[CHANNELS];

            if (enabled  == 0)
			{
				return;
			}
			else if(enabled  == 1 || query.mode == QUERY_MODE.DISPLACEMENT)
			{
 
                SampleHeightOnly(query.result.displacement, displacements, query.sampleSpectrum, x, z, scaling);

                query.result.height = query.result.displacement[0].y + query.result.displacement[1].y + query.result.displacement[2].y + query.result.displacement[3].y;
				query.result.displacementX = 0.0f;
				query.result.displacementZ = 0.0f;
				query.result.iterations = 0;
				query.result.error = 0.0f;

				query.result.height = Mathf.Clamp(query.result.height, -Ocean.MAX_SPECTRUM_WAVE_HEIGHT, Ocean.MAX_SPECTRUM_WAVE_HEIGHT);
			}
			else
			{
                float lx, lz;
				float dx = x;
				float dz = z;
				float u = x;
				float v = z;

                float displacementX;
                float displacementZ;
	
				float minError2 = query.minError;
				if(minError2 < WaveQuery.MIN_ERROR) minError2 = WaveQuery.MIN_ERROR;
				minError2 = minError2*minError2;
				
				float error;
				int i = 0;
				
				do
				{
					u += x - dx;
					v += z - dz;
					
					Sample(query.result.displacement, displacements, query.sampleSpectrum, u, v, scaling);

                    displacementX = query.result.displacement[0].x + query.result.displacement[1].x + query.result.displacement[2].x + query.result.displacement[3].x;
                    displacementZ = query.result.displacement[0].z + query.result.displacement[1].z + query.result.displacement[2].z + query.result.displacement[3].z;

					dx = u + displacementX;
					dz = v + displacementZ;
					
					i++;
					
					lx = x-dx;
					lz = z-dz;
					
					error = lx*lx + lz*lz;
				}
				while (error > minError2 && i <= WaveQuery.MAX_ITERATIONS);

                query.result.height = query.result.displacement[0].y + query.result.displacement[1].y + query.result.displacement[2].y + query.result.displacement[3].y;
				query.result.displacementX = displacementX;
				query.result.displacementZ = displacementZ;
				query.result.iterations = i;
				query.result.error = error;

				query.result.height = Mathf.Clamp(query.result.height, -Ocean.MAX_SPECTRUM_WAVE_HEIGHT, Ocean.MAX_SPECTRUM_WAVE_HEIGHT);
				
			}
			
		}

        static void SampleHeightOnly(Vector3[] d, IList<InterpolatedArray2f> displacements, bool[] sample, float x, float z, QueryGridScaling scaling)
		{
			
			float u, v;
            //The array needed to sample the results is kept in the scaling
            //class to reduce memory allocations.
            float[] result = scaling.result;

            if (sample[0] && scaling.numGrids > 0)
			{
                u = x * scaling.invGridSizes.x;
                v = z * scaling.invGridSizes.x;

                displacements[0].Get(u, v, result);
				d[0].y = result[1] * scaling.scaleY;
            }
			
			if(sample[1] && scaling.numGrids > 1)
			{
                u = x * scaling.invGridSizes.y;
                v = z * scaling.invGridSizes.y;

                displacements[1].Get(u, v, result);
                d[1].y = result[1] * scaling.scaleY;
            }
			
			if(sample[2] && scaling.numGrids > 2)
			{
                u = x * scaling.invGridSizes.z;
                v = z * scaling.invGridSizes.z;

                displacements[2].Get(u, v, result);
                d[2].y = result[1] * scaling.scaleY;
            }
			
			if(sample[3] && scaling.numGrids > 3)
			{
                u = x * scaling.invGridSizes.w;
                v = z * scaling.invGridSizes.w;

                displacements[3].Get(u, v, result);
                d[3].y = result[1] * scaling.scaleY;
            }
			
		}
		
		static void Sample(Vector3[] d, IList<InterpolatedArray2f> displacements, bool[] sample, float x, float z, QueryGridScaling scaling)
		{
			
			float u, v;
            //The array needed to sample the results is kept in the scaling
            //class to reduce memory allocations.
            float[] result = scaling.result;

            if (sample[0] && scaling.numGrids > 0)
			{
                u = x * scaling.invGridSizes.x;
                v = z * scaling.invGridSizes.x;

                displacements[0].Get(u, v, result);
				
				d[0].x = result[0] * scaling.choppyness.x;
                d[0].y = result[1] * scaling.scaleY;
                d[0].z = result[2] * scaling.choppyness.x;
			}
			
			if(sample[1] && scaling.numGrids > 1)
			{
                u = x * scaling.invGridSizes.y;
                v = z * scaling.invGridSizes.y;

                displacements[1].Get(u, v, result);

				d[1].x = result[0] * scaling.choppyness.y;
                d[1].y = result[1] * scaling.scaleY;
                d[1].z = result[2] * scaling.choppyness.y;
			}
			
			if(sample[2] && scaling.numGrids > 2)
			{
                u = x * scaling.invGridSizes.z;
                v = z * scaling.invGridSizes.z;

                displacements[2].Get(u, v, result);

				d[2].x = result[0] * scaling.choppyness.z;
                d[2].y = result[1] * scaling.scaleY;
                d[2].z = result[2] * scaling.choppyness.z;
			}
			
			if(sample[3] && scaling.numGrids > 3)
			{
                u = x * scaling.invGridSizes.w;
                v = z * scaling.invGridSizes.w;

                displacements[3].Get(u, v, result);

				d[3].x = result[0] * scaling.choppyness.w;
                d[3].y = result[1] * scaling.scaleY;
                d[3].z = result[2] * scaling.choppyness.w;
			}
			
		}
		
		public static Vector4 MaxRange(IList<InterpolatedArray2f> displacements, Vector4 choppyness, Vector2 gridScale, ICancelToken token)
		{

			if(displacements.Count != GRIDS)
				throw new InvalidOperationException("Query Displacements requires a displacement buffer for each of the " + GRIDS + " grids.");
			
			if(displacements[0].Channels != CHANNELS)
				throw new InvalidOperationException("Query Displacements requires displacement buffers have " + CHANNELS + " channels.");

			int size = displacements[0].SX;

			Vector3 ninf = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			Vector3 pinf = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

			Vector3[] max = new Vector3[] { ninf, ninf, ninf, ninf };
			Vector3[] min = new Vector3[] { pinf, pinf, pinf, pinf };

			float[] h = new float[CHANNELS];

			int grids = GRIDS;

            //There are 4 grids but the last ones waves are to small to really
            //count towards the range so dont sample.
			grids = 3;

			for(int i = 0; i < grids; i++)
			{

				float[] data = displacements[i].Data;

				for(int y = 0; y < size; y++)
				{
					for(int x = 0; x < size; x++)
					{

                        if (token != null && token.Cancelled) return Vector4.zero;

						int idx = (x+y*size)*CHANNELS;

						h[0] = data[idx + 0];
						h[1] = data[idx + 1];
						h[2] = data[idx + 2];
						
						if(h[0] < min[i].x) min[i].x = h[0];
						if(h[0] > max[i].x) max[i].x = h[0];

						if(h[1] < min[i].y) min[i].y = h[1];
						if(h[1] > max[i].y) max[i].y = h[1];

						if(h[2] < min[i].z) min[i].z = h[2];
						if(h[2] > max[i].z) max[i].z = h[2];

					}
				}
			}

			Vector4 result = Vector4.zero;

			for(int i = 0; i < grids; i++)
			{
				result.x += Mathf.Max(max[i].x, Mathf.Abs(min[i].x)) * choppyness[i];
				result.y += Mathf.Max(max[i].y, Mathf.Abs(min[i].y));
				result.z += Mathf.Max(max[i].z, Mathf.Abs(min[i].z)) * choppyness[i];
			}

			result.x *= gridScale.x;
			result.y *= gridScale.y;
			result.z *= gridScale.x;
			
			return result;
			
		}


		public static void CopyAndCreateDisplacements(IList<InterpolatedArray2f> source, out IList<InterpolatedArray2f> des)
		{

			if(source.Count != GRIDS)
				throw new InvalidOperationException("Query Displacements requires a displacement buffer for each of the " + GRIDS + " grids.");
			
			if(source[0].Channels != CHANNELS)
				throw new InvalidOperationException("Query Displacements requires displacement buffers have " + CHANNELS + " channels.");

			int size = source[0].SX;

			des = new InterpolatedArray2f[GRIDS];

			des[0] = new InterpolatedArray2f(source[0].Data, size, size, CHANNELS, true);
			des[1] = new InterpolatedArray2f(source[1].Data, size, size, CHANNELS, true);
			des[2] = new InterpolatedArray2f(source[2].Data, size, size, CHANNELS, true);
			des[3] = new InterpolatedArray2f(source[3].Data, size, size, CHANNELS, true);
			
		}

		public static void CopyDisplacements(IList<InterpolatedArray2f> source, IList<InterpolatedArray2f> des)
		{
			
			if(source.Count != GRIDS)
				throw new InvalidOperationException("Query Displacements requires a displacement buffer for each of the " + GRIDS + " grids.");
			
			if(source[0].Channels != CHANNELS)
				throw new InvalidOperationException("Query Displacements requires displacement buffers have " + CHANNELS + " channels.");

			des[0].Copy(source[0].Data);
			des[1].Copy(source[1].Data);
			des[2].Copy(source[2].Data);
			des[3].Copy(source[3].Data);
			
		}

	}
}
























