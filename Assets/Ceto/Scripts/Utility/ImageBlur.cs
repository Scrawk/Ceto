using UnityEngine;
using System.Collections;

namespace Ceto
{

	public class ImageBlur 
	{

		public enum BLUR_MODE { OFF = 0, NO_DOWNSAMPLE = 1, DOWNSAMPLE_2 = 2, DOWNSAMPLE_4 = 4 };

		public BLUR_MODE BlurMode { get; set; }
		
		/// Blur iterations - larger number means more blur.
		public int BlurIterations { get; set; }
		
		/// Blur spread for each iteration. Lower values
		/// give better looking blur, but require more iterations to
		/// get large blurs. Value is usually between 0.5 and 1.0.
		public float BlurSpread { get; set; }

        Vector2[] m_offsets = new Vector2[4];

        public Material m_blurMaterial;

		public ImageBlur(Shader blurShader)
		{

			BlurIterations = 1;
			BlurSpread = 0.6f;
			BlurMode = BLUR_MODE.DOWNSAMPLE_2;

			if(blurShader != null)
				m_blurMaterial = new Material(blurShader);

		}

		public void Blur(RenderTexture source)
		{

			int blurDownSample = (int)BlurMode;

			if(BlurIterations > 0 && m_blurMaterial != null && blurDownSample > 0)
			{
				int rtW = source.width / blurDownSample;
				int rtH = source.height / blurDownSample;

				RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0, source.format, RenderTextureReadWrite.Default);
	
				// Copy source to the smaller texture.
				DownSample(source, buffer);
				
				// Blur the small texture
				for (int i = 0; i < BlurIterations; i++)
				{
					RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format, RenderTextureReadWrite.Default);
					FourTapCone(buffer, buffer2, i);
					RenderTexture.ReleaseTemporary(buffer);
					buffer = buffer2;
				}
				
				Graphics.Blit(buffer, source);
				RenderTexture.ReleaseTemporary(buffer);
			}

		}

		// Performs one blur iteration.
		void FourTapCone (RenderTexture source, RenderTexture dest, int iteration)
		{
			float off = 0.5f + iteration*BlurSpread;

            m_offsets[0].x = -off;
            m_offsets[0].y = -off;

            m_offsets[1].x = -off;
            m_offsets[1].y = off;

            m_offsets[2].x = off;
            m_offsets[2].y = off;

            m_offsets[3].x = off;
            m_offsets[3].y = -off;

            Graphics.BlitMultiTap(source, dest, m_blurMaterial, m_offsets);
		}
		
		// Downsamples the texture to a quarter resolution.
		void DownSample(RenderTexture source, RenderTexture dest)
		{
			float off = 1.0f;

            m_offsets[0].x = -off;
            m_offsets[0].y = -off;

            m_offsets[1].x = -off;
            m_offsets[1].y = off;

            m_offsets[2].x = off;
            m_offsets[2].y = off;

            m_offsets[3].x = off;
            m_offsets[3].y = -off;

            Graphics.BlitMultiTap (source, dest, m_blurMaterial, m_offsets);
		}

	}

}
