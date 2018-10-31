using System;

namespace Ceto.Common.Containers.Interpolation
{
    /// <summary>
	/// Abstract class providing common functions for Interpolated array.
    /// A filtered array allows the bilinear filtering of its contained data.
    /// </summary>
	public abstract class InterpolatedArray
    {
        /// <summary>
        /// Should the sampling of the array be wrapped or clamped.
        /// </summary>
        public bool Wrap { get { return m_wrap; } set { m_wrap = value; } }
        bool m_wrap;

        /// <summary>
        /// Should the interpolation be done with a 
        /// half pixel offset.
        /// </summary>
        public bool HalfPixelOffset { get; set; }

		public InterpolatedArray(bool wrap)
        {
            m_wrap = wrap;
			HalfPixelOffset = true;
        }

		/// <summary>
		/// Get the index that needs to be sampled for point filtering.
		/// </summary>
		public void Index(ref int x, int sx)
		{

			if(m_wrap)
			{
				if(x >= sx || x <= -sx) x = x % sx;
				if(x < 0) x = sx - -x;
			}
			else
			{
				if(x < 0) x = 0;
				else if(x >= sx) x = sx-1;
			}
			
		}

		/// <summary>
		/// Get the two indices that need to be sampled for bilinear filtering.
		/// </summary>
		public void Index(double x, int sx, out int ix0, out int ix1)
		{
			
			ix0 = (int)x;
			ix1 = (int)x + (int)Math.Sign(x);
			
			if(m_wrap)
			{
				if(ix0 >= sx || ix0 <= -sx) ix0 = ix0 % sx;
				if(ix0 < 0) ix0 = sx - -ix0;
				
				if(ix1 >= sx || ix1 <= -sx) ix1 = ix1 % sx;
				if(ix1 < 0) ix1 = sx - -ix1;
			}
			else
			{
				if(ix0 < 0) ix0 = 0;
				else if(ix0 >= sx) ix0 = sx-1;

				if(ix1 < 0) ix1 = 0;
				else if(ix1 >= sx) ix1 = sx-1;
			}
			
		}

    }


}





