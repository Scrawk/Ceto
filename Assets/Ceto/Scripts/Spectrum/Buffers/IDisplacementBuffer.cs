using UnityEngine;
using System.Collections.Generic;

using Ceto.Common.Containers.Interpolation;

namespace Ceto
{

	public interface IDisplacementBuffer
	{

		bool IsGPU { get; }

        int Size { get; }

        InterpolatedArray2f[] GetReadDisplacements();

		void CopyAndCreateDisplacements(out IList<InterpolatedArray2f> displacements);

		void CopyDisplacements(IList<InterpolatedArray2f> des);

		Vector4 MaxRange(Vector4 choppyness, Vector2 gridScale);

		void QueryWaves(WaveQuery query, QueryGridScaling scaling);

		int EnabledBuffers();


	}

}
