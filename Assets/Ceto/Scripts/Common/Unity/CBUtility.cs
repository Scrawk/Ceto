using UnityEngine;
using System.Collections;
using System.IO;

namespace Ceto.Common.Unity.Utility
{

	static public class CBUtility
	{

		static string[,] readNames2D = new string[,]
		{
			{"read2DC1", "_Tex2D", "_Buffer2DC1"},
			{"read2DC2", "_Tex2D", "_Buffer2DC2"},
			{"read2DC3", "_Tex2D", "_Buffer2DC3"},
			{"read2DC4", "_Tex2D", "_Buffer2DC4"}
		};

        /*
		static string[,] readNames3D = new string[,]
		{
			{"read3DC1", "_Tex3D", "_Buffer3DC1"},
			{"read3DC2", "_Tex3D", "_Buffer3DC2"},
			{"read3DC3", "_Tex3D", "_Buffer3DC3"},
			{"read3DC4", "_Tex3D", "_Buffer3DC4"}
		};
        */

		static public void ReadFromRenderTexture(RenderTexture tex, int channels, ComputeBuffer buffer, ComputeShader readData)
		{
			if(tex == null)
			{
				Debug.Log("CBUtility::ReadFromRenderTexture - RenderTexture is null");
				return;
			}
			
			if(buffer == null)
			{
				Debug.Log("CBUtility::ReadFromRenderTexture - buffer is null");
				return;
			}
			
			if(readData == null)
			{
				Debug.Log("CBUtility::ReadFromRenderTexture - Computer shader is null");
				return;
			}
			
			if(channels < 1 || channels > 4)
			{
				Debug.Log("CBUtility::ReadFromRenderTexture - Channels must be 1, 2, 3, or 4");
				return;
			}

			if(!tex.IsCreated())
			{
				Debug.Log("CBUtility::ReadFromRenderTexture - tex has not been created (Call Create() on tex)");
				return;
			}
			
			int kernel = -1;
			int depth = 1;
			
			//if(tex.isVolume)
			//{
			//	depth = tex.volumeDepth;
			//	kernel = readData.FindKernel(readNames3D[channels - 1, 0]);
			//	readData.SetTexture(kernel, readNames3D[channels - 1, 1], tex);
			//	readData.SetBuffer(kernel, readNames3D[channels - 1, 2], buffer);
			//}
			//else
			//{
				kernel = readData.FindKernel(readNames2D[channels - 1, 0]);
				readData.SetTexture(kernel, readNames2D[channels - 1, 1], tex);
				readData.SetBuffer(kernel, readNames2D[channels - 1, 2], buffer);
			//}
			
			if(kernel == -1)
			{
				Debug.Log("CBUtility::ReadFromRenderTexture - could not find kernels");
				return;
			}
			
			int width = tex.width;
			int height = tex.height;
			
			//set the compute shader uniforms
			readData.SetInt("_Width", width);
			readData.SetInt("_Height", height);
			readData.SetInt("_Depth", depth);
			//run the  compute shader. Runs in threads of 8 so non divisible by 8 numbers will need
			//some extra threadBlocks. This will result in some unneeded threads running 
			int padX = (width%8 == 0) ? 0 : 1;
			int padY = (height%8 == 0) ? 0 : 1;
			int padZ = (depth%8 == 0) ? 0 : 1;

			readData.Dispatch(kernel, Mathf.Max(1,width/8 + padX), Mathf.Max(1, height/8 + padY), Mathf.Max(1, depth/8 + padZ));

		}

		static public void ReadSingleFromRenderTexture(RenderTexture tex, float x, float y, float z, ComputeBuffer buffer, ComputeShader readData, bool useBilinear)
		{
			if(tex == null)
			{
				Debug.Log("CBUtility::ReadSingleFromRenderTexture - RenderTexture is null");
				return;
			}
			
			if(buffer == null)
			{
				Debug.Log("CBUtility::ReadSingleFromRenderTexture - buffer is null");
				return;
			}
			
			if(readData == null)
			{
				Debug.Log("CBUtility::ReadSingleFromRenderTexture - Computer shader is null");
				return;
			}
			
			if(!tex.IsCreated())
			{
				Debug.Log("CBUtility::ReadSingleFromRenderTexture - tex has not been created (Call Create() on tex)");
				return;
			}
			
			int kernel = -1;
			int depth = 1;

			//if(tex.isVolume)
			//{
			//	depth = tex.volumeDepth;
            //
			//	if(useBilinear)
			//		kernel = readData.FindKernel("readSingle3D");
			//	else
			//		kernel = readData.FindKernel("readSingleBilinear3D");
            //
			//	readData.SetTexture(kernel, "_Tex3D", tex);
			//	readData.SetBuffer(kernel, "BufferSingle3D", buffer);
			//}
			//else
			//{
				if(useBilinear)
					kernel = readData.FindKernel("readSingle2D");
				else
					kernel = readData.FindKernel("readSingleBilinear2D");
				
				readData.SetTexture(kernel, "_Tex2D", tex);
				readData.SetBuffer(kernel, "BufferSingle2D", buffer);
			//}
			
			if(kernel == -1)
			{
				Debug.Log("CBUtility::ReadSingleFromRenderTexture - could not find kernels");
				return;
			}
			
			int width = tex.width;
			int height = tex.height;

			//used for point sampling
			readData.SetInt("_IdxX", (int)x);
			readData.SetInt("_IdxY", (int)y);
			readData.SetInt("_IdxZ", (int)z);
			//used for bilinear sampling
			readData.SetVector("_UV", new Vector4( x / (float)(width-1), y / (float)(height-1), z / (float)(depth-1), 0.0f));

			readData.Dispatch(kernel, 1, 1, 1);
			
		}

	}

}




















