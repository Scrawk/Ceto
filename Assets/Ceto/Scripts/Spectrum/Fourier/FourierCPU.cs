using UnityEngine;
using System;
using System.Collections.Generic;

using Ceto.Common.Threading.Tasks;

namespace Ceto
{
	
    public class FourierCPU
    {

		struct LookUp
		{
			public int j1, j2;
			public float wr, wi;
		}

		public int size { get { return m_size; } }
		int m_size;
		float m_fsize;
		
		public int passes { get { return m_passes; } }
		int m_passes;

		LookUp[] m_butterflyLookupTable = null;

        public FourierCPU(int size)
        {

			if (!Mathf.IsPowerOfTwo(size))
				throw new ArgumentException("Fourier grid size must be pow2 number");

            m_size = size;
            m_fsize = (float)m_size;
            m_passes = (int)(Mathf.Log(m_fsize) / Mathf.Log(2.0f));
            ComputeButterflyLookupTable();
        }


        int BitReverse(int i)
        {
            int j = i;
            int Sum = 0;
            int W = 1;
            int M = m_size / 2;
            while (M != 0)
            {
                j = ((i & M) > M - 1) ? 1 : 0;
                Sum += j * W;
                W *= 2;
                M /= 2;
            }
            return Sum;
        }

        void ComputeButterflyLookupTable()
        {
			m_butterflyLookupTable = new LookUp[m_size * m_passes];

            for (int i = 0; i < m_passes; i++)
            {
                int nBlocks = (int)Mathf.Pow(2, m_passes - 1 - i);
                int nHInputs = (int)Mathf.Pow(2, i);

                for (int j = 0; j < nBlocks; j++)
                {
                    for (int k = 0; k < nHInputs; k++)
                    {
                        int i1, i2, j1, j2;
                        if (i == 0)
                        {
                            i1 = j * nHInputs * 2 + k;
                            i2 = j * nHInputs * 2 + nHInputs + k;
                            j1 = BitReverse(i1);
                            j2 = BitReverse(i2);
                        }
                        else
                        {
                            i1 = j * nHInputs * 2 + k;
                            i2 = j * nHInputs * 2 + nHInputs + k;
                            j1 = i1;
                            j2 = i2;
                        }

                        float wr = Mathf.Cos(2.0f * Mathf.PI * (float)(k * nBlocks) / m_fsize);
                        float wi = Mathf.Sin(2.0f * Mathf.PI * (float)(k * nBlocks) / m_fsize);

                        int offset1 = (i1 + i * m_size);
                        m_butterflyLookupTable[offset1].j1 = j1;
                        m_butterflyLookupTable[offset1].j2 = j2;
                        m_butterflyLookupTable[offset1].wr = wr;
                        m_butterflyLookupTable[offset1].wi = wi;

                        int offset2 = (i2 + i * m_size);
                        m_butterflyLookupTable[offset2].j1 = j1;
                        m_butterflyLookupTable[offset2].j2 = j2;
                        m_butterflyLookupTable[offset2].wr = -wr;
                        m_butterflyLookupTable[offset2].wi = -wi;

                    }
                }
            }
        }

        //Performs two FFTs on two complex numbers packed in a vector4
        Vector4 FFT(Vector2 w, Vector4 input1, Vector4 input2)
        {
            input1.x += w.x * input2.x - w.y * input2.y;
            input1.y += w.y * input2.x + w.x * input2.y;
            input1.z += w.x * input2.z - w.y * input2.w;
            input1.w += w.y * input2.z + w.x * input2.w;

            return input1;
        }

        //Performs one FFT on a complex number
        Vector2 FFT(Vector2 w, Vector2 input1, Vector2 input2)
        {
            input1.x += w.x * input2.x - w.y * input2.y;
            input1.y += w.y * input2.x + w.x * input2.y;

            return input1;
        }

		public int PeformFFT_SinglePacked(int startIdx, IList<Vector4[]> data0, ICancelToken token)
		{
			
			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi, si, sy;
			
			int j = startIdx;
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];

				Vector4[] read0 = data0[idx1];
				
				si = i * m_size;
				
				for (x = 0; x < m_size; x++)
				{
					
					bftIdx = x + si;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;
					
					for (y = 0; y < m_size; y++)
					{
                        if (token.Cancelled) return -1;

						sy = y * m_size;
						
						ii = x + sy;
						xi = X + sy;
						yi = Y + sy;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;

					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				
				Vector4[] read0 = data0[idx1];
				
				si = i * m_size;
				
				for (y = 0; y < m_size; y++)
				{
					
					bftIdx = y + si;
					
					X = m_butterflyLookupTable[bftIdx].j1 * m_size;
					Y = m_butterflyLookupTable[bftIdx].j2 * m_size;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;
					
					for (x = 0; x < m_size; x++)
					{
                        if (token.Cancelled) return -1;

                        ii = x + y * m_size;
						xi = x + X;
						yi = x + Y;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
					}
				}
			}
			
			return idx;
		}

		public int PeformFFT_DoublePacked(int startIdx, IList<Vector4[]> data0, ICancelToken token)
		{

			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi, si, sy;
			
			int j = startIdx;
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];

				Vector4[] read0 = data0[idx1];

				si = i * m_size;

				for (x = 0; x < m_size; x++)
				{

					bftIdx = x + si;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (y = 0; y < m_size; y++)
					{
                        if (token.Cancelled) return -1;

                        sy = y * m_size;

						ii = x + sy;
						xi = X + sy;
						yi = Y + sy;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						
					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];

				Vector4[] read0 = data0[idx1];

				si = i * m_size;

				for (y = 0; y < m_size; y++)
				{

					bftIdx = y + si;
					
					X = m_butterflyLookupTable[bftIdx].j1 * m_size;
					Y = m_butterflyLookupTable[bftIdx].j2 * m_size;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (x = 0; x < m_size; x++)
					{
                        if (token.Cancelled) return -1;

                        ii = x + y * m_size;
						xi = x + X;
						yi = x + Y;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
					}
				}
			}
			
			return idx;
		}

        /*
		public int PeformFFT(int startIdx, IList<Vector4[]> data0, IList<Vector4[]> data1)
		{
			
			
			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi;
			
			int j = startIdx;
			
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];

				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				
				for (x = 0; x < m_size; x++)
				{

					bftIdx =  x + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (y = 0; y < m_size; y++)
					{

						ii = x + y * m_size;
						xi = X + y * m_size;
						yi = Y + y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];

				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];

				for (y = 0; y < m_size; y++)
				{
					bftIdx = y + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (x = 0; x < m_size; x++)
					{

						ii = x + y * m_size;
						xi = x + X * m_size;
						yi = x + Y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
					}
				}
			}
			
			return idx;
		}

		public int PeformFFT(int startIdx, IList<Vector4[]> data0, IList<Vector4[]> data1, IList<Vector4[]> data2)
        {


            int x; int y; int i;
            int idx = 0; int idx1; int bftIdx;
            int X; int Y;
            float wx, wy;
			int ii, xi, yi;

            int j = startIdx;

            for (i = 0; i < m_passes; i++, j++)
            {
                idx = j % 2;
                idx1 = (j + 1) % 2;

				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];

				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];

                for (x = 0; x < m_size; x++)
                {

					bftIdx = x + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

                    for (y = 0; y < m_size; y++)
                    {

						ii = x + y * m_size;
						xi = X + y * m_size;
						yi = Y + y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;

						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;

						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

                    }
                }
            }

            for (i = 0; i < m_passes; i++, j++)
            {
                idx = j % 2;
                idx1 = (j + 1) % 2;

				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];
				
				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];

                for (y = 0; y < m_size; y++)
                {

					bftIdx = y + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

                    for (x = 0; x < m_size; x++)
                    {
   
						ii = x + y * m_size;
						xi = x + X * m_size;
						yi = x + Y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

                    }
                }
            }

            return idx;
        }

		public int PeformFFT(int startIdx, IList<Vector4[]> data0, IList<Vector4[]> data1, IList<Vector4[]> data2, IList<Vector4[]> data3)
		{
			
			
			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi;
			
			int j = startIdx;
			
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];
				Vector4[] write3 = data3[idx];
				
				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];
				Vector4[] read3 = data3[idx1];
				
				for (x = 0; x < m_size; x++)
				{

					bftIdx = x + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (y = 0; y < m_size; y++)
					{

						ii = x + y * m_size;
						xi = X + y * m_size;
						yi = Y + y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

						write3[ii].x = read3[xi].x + wx * read3[yi].x - wy * read3[yi].y;
						write3[ii].y = read3[xi].y + wy * read3[yi].x + wx * read3[yi].y;
						write3[ii].z = read3[xi].z + wx * read3[yi].z - wy * read3[yi].w;
						write3[ii].w = read3[xi].w + wy * read3[yi].z + wx * read3[yi].w;
						
					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];
				Vector4[] write3 = data3[idx];
				
				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];
				Vector4[] read3 = data3[idx1];
				
				for (y = 0; y < m_size; y++)
				{
					for (x = 0; x < m_size; x++)
					{
						bftIdx = y + i * m_size;
						
						X = m_butterflyLookupTable[bftIdx].j1;
						Y = m_butterflyLookupTable[bftIdx].j2;
						wx = m_butterflyLookupTable[bftIdx].wr;
						wy = m_butterflyLookupTable[bftIdx].wi;
						
						ii = x + y * m_size;
						xi = x + X * m_size;
						yi = x + Y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

						write3[ii].x = read3[xi].x + wx * read3[yi].x - wy * read3[yi].y;
						write3[ii].y = read3[xi].y + wy * read3[yi].x + wx * read3[yi].y;
						write3[ii].z = read3[xi].z + wx * read3[yi].z - wy * read3[yi].w;
						write3[ii].w = read3[xi].w + wy * read3[yi].z + wx * read3[yi].w;
						
					}
				}
			}
			
			return idx;
		}
        */

    }

}

















