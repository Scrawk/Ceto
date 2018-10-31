using UnityEngine;
using System.Collections;

namespace Ceto
{
	/// <summary>
	/// Calculates the projection VP matrix and interpolation matrix.
	/// The projector VP matrix is used by the overlay system to project
	/// the overlays onto the ocean mesh. The interpolation matrix is
	/// used to convert a screen space mesh position into the world 
	/// position on the projection plane. 
	/// </summary>
	public interface IProjection 
	{

		/// <summary>
		/// Update the projection data for this camera.
		/// If this is the scene view camera you may not want to project 
		/// the grid from its point of view but instead from the main 
		/// cameras view so you can see how the mesh is being projected.
		/// </summary>
		void UpdateProjection(Camera cam, CameraData data);

		bool IsDouble { get; }

		bool IsFlipped { get; }

        bool TightFit { get; set; }

	}

}