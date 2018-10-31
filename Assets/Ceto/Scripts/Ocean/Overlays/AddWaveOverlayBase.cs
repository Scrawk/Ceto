using UnityEngine;
using System.Collections.Generic;

#pragma warning disable 219

namespace Ceto
{

    /// <summary>
    /// Base class for the add overlay scripts.
    /// Provides some commonly used functions.
    /// </summary>
    public abstract class AddWaveOverlayBase : MonoBehaviour
    {

        /// <summary>
        /// A list of all the overlays created.
        /// </summary>
        protected List<WaveOverlay> m_overlays = new List<WaveOverlay>();

        /// <summary>
        /// If multiple overlays are used allow enumeration.
        /// </summary>
        public IEnumerable<WaveOverlay> Overlays {  get { return m_overlays; } }

        /// <summary>
        /// If a single overlay used it will be in the first index of list.
        /// </summary>
        public WaveOverlay Overlay { get { return m_overlays[0]; } }

        protected virtual void Start()
        {

        }

		/// <summary>
		/// Call to translate the overlays by this amount
		/// </summary>
        public virtual void Translate(Vector3 amount)
        {
            if (m_overlays != null)
            {
                //unhide all the overlays on enable.
                for (int i = 0; i < m_overlays.Count; i++)
                {
                    m_overlays[i].Position = m_overlays[i].Position + amount;
                }
            }
        }

        /// <summary>
        /// Each overlay needs to be updated each frame.
        /// TODO - only updates overlays that have changed.
        /// </summary>
        protected virtual void Update()
        {

            if (m_overlays != null)
            {
                //unhide all the overlays on enable.
                for(int i = 0; i < m_overlays.Count; i++)
                {
                    m_overlays[i].UpdateOverlay();
                }
            }

        }

        /// <summary>
        /// On enable unhide all the overlays.
        /// </summary>
        protected virtual void OnEnable()
        {

            if (m_overlays != null)
            {
                //unhide all the overlays on enable.
                for (int i = 0; i < m_overlays.Count; i++)
                {
                    m_overlays[i].Hide = false;
                }
            }
        }

        /// <summary>
        /// On disable hide all the overlays.
        /// </summary>
        protected virtual void OnDisable()
        {

            if (m_overlays != null)
            {
                //unhide all the overlays on enable.
                for (int i = 0; i < m_overlays.Count; i++)
                {
                    m_overlays[i].Hide = true;
                }
            }
        }

        /// <summary>
        /// On destroy kill all the overlays.
        /// </summary>
        protected virtual void OnDestroy()
        {

            if (m_overlays != null)
            {
                //kill all the overlays on destroy
                for (int i = 0; i < m_overlays.Count; i++)
                {
                    m_overlays[i].Kill = true;
                }
            }
        }

        /// <summary>
        /// The default curve for the time line.
        /// </summary>
        protected static AnimationCurve DefaultCurve()
        {

            Keyframe[] keys = new Keyframe[]
			{
				new Keyframe(0.0f, 0.0f),
				new Keyframe(0.012f, 0.98f),
				new Keyframe(0.026f, 1.0f),
				new Keyframe(1.0f, 0.0f)
			};

            return new AnimationCurve(keys);
        }

        /// <summary>
        /// For textures that require their contents to be
        /// sampled check to see if read/write is enabled.
        /// </summary>
        protected void CheckCanSampleTex(Texture tex, string name)
        {

            if (tex == null) return;

            if (!(tex is Texture2D))
            {
                Ocean.LogWarning("Can not query overlays " + name + " if texture is not Texture2D");
                return;
            }

            Texture2D t = tex as Texture2D;

            //Is there a better way to do this?
            try
            {
                Color c = t.GetPixel(0, 0);
            }
            catch
            {
                Ocean.LogWarning("Can not query overlays " + name + " if read/write is not enabled");
            }
        }

    }

}







