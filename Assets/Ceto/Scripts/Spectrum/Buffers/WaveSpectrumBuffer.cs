using UnityEngine;
using System;
using System.Collections;

namespace Ceto
{

    /// <summary>
    /// Base class for a spectrum buffer.
    /// Spectrum buffers are responsible for transforming
    /// the spectrum using FFT and then managing the data
    /// created. The buffer superclass can implement the 
    /// FFT using what ever method they want and generate 
    /// what ever type of data they want from the spectrum,
    /// is the displacements, slope, etc.
    /// </summary>
	public abstract class WaveSpectrumBuffer
	{

        /// <summary>
        /// Has the buffer finished creating the requested data.
        /// </summary>
		public abstract bool Done { get; }

        /// <summary>
        /// The fourier size of the buffer.
        /// </summary>
		public abstract int Size { get; }

        /// <summary>
        /// Does this buffer run on the GPU.
        /// </summary>
		public abstract bool IsGPU { get; }

        /// <summary>
        /// Get the texture at idx created by this buffer.
        /// </summary>
		public abstract Texture GetTexture(int idx);

		/// <summary>
		/// The time value used to create the data.
		/// </summary>
		public float TimeValue { get; protected set; }

		/// <summary>
		/// False until the first time run is called.
		/// </summary>
		public bool HasRun { get; protected set; }

		/// <summary>
		/// Has the data been sampled.
		/// </summary>
		public bool BeenSampled { get; set; }

        /// <summary>
        /// The material used for the initializing the fourier data.
        /// Only used by GPU buffers.
        /// </summary>
        public Material InitMaterial { get; set; }

        /// <summary>
        /// The material pass used for the initializing the fourier data.
        /// Only used by GPU buffers.
        /// </summary>
        public int InitPass { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public WaveSpectrumBuffer()
        {

        }

        /// <summary>
        /// Initialized the buffer with the spectrum for these conditions at this time.
        /// </summary>
		protected abstract void Initilize(WaveSpectrumCondition condition, float time);

        /// <summary>
        /// Run the buffer with the spectrum for these conditions at this time.
        /// </summary>
		public abstract void Run(WaveSpectrumCondition condition, float time);

        /// <summary>
        /// The buffer may generate multiple sets of data. 
        /// Enable the data in buffer idx.
        /// </summary>
		public abstract void EnableBuffer(int idx);

        /// <summary>
        /// The buffer may generate multiple sets of data. 
        /// Disable the data in buffer idx.
        /// </summary>
		public abstract void DisableBuffer(int idx);

        /// <summary>
        /// How many of the buffers are enabled.
        /// </summary>
		public abstract int EnabledBuffers();

        /// <summary>
        /// Is this buffer enabled.
        /// </summary>
        public abstract bool IsEnabledBuffer(int i);

        /// <summary>
        /// Release any resources held.
        /// </summary>
		public virtual void Release()
		{

		}

        /// <summary>
        /// Call before sampling from any of the buffer textures.
        /// </summary>
		public virtual void EnableSampling()
		{

		}

        /// <summary>
        /// Call after sampling from any of the buffer textures.
        /// </summary>
		public virtual void DisableSampling()
		{

		}


	}

}
