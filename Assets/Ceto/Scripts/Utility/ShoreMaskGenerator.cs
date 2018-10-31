using UnityEngine;
using System.Collections;

using Ceto.Common.Containers.Interpolation;

namespace Ceto
{

	public static class ShoreMaskGenerator 
	{

		public static float[] CreateHeightMap(Terrain terrain)
		{
			
			TerrainData data = terrain.terrainData;

			int resolution = data.heightmapResolution;

			Vector3 scale = data.heightmapScale;
			
			float[,] heights = data.GetHeights(0, 0, resolution, resolution);
			
			float[] map = new float[resolution * resolution];
			
			for(int y = 0; y < resolution; y++)
			{
				for(int x = 0; x < resolution; x++)
				{
					map[x + y * resolution] = heights[y,x] * scale.y + terrain.transform.position.y;
				}
			}
			
			return map;
			
		}


		public static Texture2D CreateMask(float[] heightMap, int size, float shoreLevel, float spread, TextureFormat format)
		{

			Texture2D mask = new Texture2D(size, size, format, false, true);
            mask.filterMode = FilterMode.Bilinear;

            int s2 = size * size;

            Color[] colors = new Color[s2];
			
			for(int i = 0; i < s2; i++)
			{
			    float h = Mathf.Clamp(shoreLevel - heightMap[i], 0.0f, spread);

                h = 1.0f - h / spread;

                colors[i].r = h;
                colors[i].g = h;
                colors[i].b = h;
                colors[i].a = h;
			}

            mask.SetPixels(colors);

			mask.Apply();
			
			return mask;
			
		}

        public static Texture2D CreateMask(InterpolatedArray2f heightMap, int width, int height, float shoreLevel, float spread, TextureFormat format)
        {

            Texture2D mask = new Texture2D(width, height, format, false, true);
            mask.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[width * height];

            bool matches = width == heightMap.SX && height == heightMap.SY;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = x + y * height;

                    float h = 0.0f;

                    if (matches)
                    {
                        h = Mathf.Clamp(shoreLevel - heightMap.Data[i], 0.0f, spread);
                    }
                    else
                    {
                        float fx = x / (width - 1.0f);
                        float fy = y / (height - 1.0f);
                        h = Mathf.Clamp(shoreLevel - heightMap.Get(fx, fy, 0), 0.0f, spread);
                    }

                    h = 1.0f - h / spread;

                    colors[i].r = h;
                    colors[i].g = h;
                    colors[i].b = h;
                    colors[i].a = h;
                }
            }

            mask.SetPixels(colors);

            mask.Apply();

            return mask;

        }

        public static Texture2D CreateClipMask(float[] heightMap, int size, float shoreLevel, TextureFormat format)
        {

            Texture2D mask = new Texture2D(size, size, format, false, true);
            mask.filterMode = FilterMode.Bilinear;

            int s2 = size * size;

            Color[] colors = new Color[s2];

            for (int i = 0; i < s2; i++)
            {
                float h = Mathf.Clamp(heightMap[i] - shoreLevel, 0.0f, 1.0f);

                if (h > 0.0f) h = 1.0f;

                colors[i].r = h;
                colors[i].g = h;
                colors[i].b = h;
                colors[i].a = h;
            }

            mask.SetPixels(colors);

            mask.Apply();

            return mask;

        }

        public static Texture2D CreateClipMask(InterpolatedArray2f heightMap, int width, int height, float shoreLevel, TextureFormat format)
        {

            Texture2D mask = new Texture2D(width, height, format, false, true);
            mask.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[width * height];

            bool matches = width == heightMap.SX && height == heightMap.SY;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = x + y * height;

                    float h = 0.0f;

                    if (matches)
                    {
                        h = Mathf.Clamp(heightMap.Data[i] - shoreLevel, 0.0f, 1.0f);
                    }
                    else
                    {
                        float fx = x / (width - 1.0f);
                        float fy = y / (height - 1.0f);
                        h = Mathf.Clamp(heightMap.Get(fx, fy, 0) - shoreLevel, 0.0f, 1.0f);
                    }

                    if (h > 0.0f) h = 1.0f;

                    colors[i].r = h;
                    colors[i].g = h;
                    colors[i].b = h;
                    colors[i].a = h;
                }
            }

            mask.SetPixels(colors);

            mask.Apply();

            return mask;

        }

    }

}
