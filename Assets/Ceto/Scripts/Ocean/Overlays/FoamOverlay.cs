using UnityEngine;
using System.Collections;

namespace Ceto
{

    /// <summary>
    /// Extends the wave overlay base class.
    /// Allows the overlay to change its spin, momentum and size over time.
    /// </summary>
	public class FoamOverlay : WaveOverlay
	{
        /// <summary>
        /// How much the overlay will move each frame.
        /// </summary>
		public Vector3 Momentum { get; set; }

        /// <summary>
        /// How much the overlay will rotate each frame.
        /// </summary>
		public float Spin { get; set; }

        /// <summary>
        /// How much the overlay will grow each frame.
        /// </summary>
		public float Expansion { get; set; }

        /// <summary>
        /// The current size after expansion.
        /// </summary>
		public float Size { get; set; }

        /// <summary>
        /// 
        /// </summary>
		public FoamOverlay(Vector3 pos, float rotation, float size, float duration, Texture texture)
			: base(pos, rotation, new Vector2(size * 0.5f, size * 0.5f), duration)
		{

			Size = size;
			FoamTex.tex = texture;

		}

        /// <summary>
        /// Resets overlay as if it was created new.
        /// </summary>
        public void Reset(Vector3 pos, float rotation, float size, float duration, Texture texture)
        {
            base.Reset(pos, rotation, new Vector2(size * 0.5f, size * 0.5f), duration);

            Size = size;
            FoamTex.tex = texture;
            Momentum = Vector3.zero;
            Spin = 0.0f;
            Expansion = 0.0f;

        }

        /// <summary>
        /// Update the position, size and rotation the 
        /// update the base class.
        /// </summary>
		public override void UpdateOverlay()
		{

            //Move position over time.
			Position += Momentum * Time.deltaTime;

            //Grow in size over time.
			Size += Expansion * Time.deltaTime;

            //Rotate over time.
			Rotation += Spin * Time.deltaTime;

			HalfSize = new Vector2(Size * 0.5f, Size * 0.5f);

			base.UpdateOverlay();

		}

	}

}
