
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ceto
{
	
	/// <summary>
	/// Calculates the projection VP matrix and interpolation matrix.
	/// The projector VP matrix is used by the overlay system to project
	/// the overlays onto the ocean mesh. The interpolation matrix is
	/// used to convert a screen space mesh position into the world 
	/// position on the projection plane. 
	/// 
	/// NOTE - The projection math is carried out in double precision as it was found
	/// the math is quite sensitive to precision errors.
	/// 
	/// </summary>
	public class Projection3d : IProjection 
	{

        public bool IsDouble { get { return true; } }

		public bool IsFlipped { get; set; }

        public bool TightFit { get; set; }

        /// <summary>
        /// A parent ocean object.
        /// </summary>
        Ocean m_ocean;
		
		/// <summary>
		/// The projector projection, view and inverse view projection matrix.
		/// </summary>
		double[] m_projectorP, m_projectorV, m_projectorVP, m_projectorIVP;
		
		/// <summary>
		/// The projector range and interpolation matrix.
		/// </summary>
		double[] m_projectorR, m_projectorI;
		
		/// <summary>
		/// The frustum corners in world space.
		/// </summary>
		List<double[]> m_frustumCorners;
		
		/// <summary>
		/// 
		/// </summary>
		List<double[]> m_pointList;
		
		/// <summary>
		/// The corner points of a quad.
		/// </summary>
		List<double[]> m_quad;
		
		/// <summary>
		/// The corner points of a frustum box.
		/// </summary>
		List<double[]> m_corners;
		
		/// <summary>
		/// Buffers used to reduce memory allocations
		/// </summary>
		double[] MATRIX_BUFFER0, MATRIX_BUFFER1, VECTOR_BUFFER;
		double[] m_xaxis, m_yaxis, m_zaxis, m_up;
		double[] m_a, m_b, m_ab;
		double[] m_pos, m_dir, m_lookAt;
		double[] m_p, m_q, m_p0, m_p1;
		
		/// <summary>
		/// 
		/// </summary>
		public Projection3d(Ocean ocean)
		{
			
			m_ocean = ocean;
			
			m_projectorP = new double[16];
			m_projectorV = new double[16];
			m_projectorVP = new double[16];
			m_projectorIVP = new double[16];
			m_projectorR = new double[16];
			m_projectorI = new double[16];
			
			MATRIX_BUFFER0 = new double[16];
			MATRIX_BUFFER1 = new double[16];
            VECTOR_BUFFER = new double[4];
			
			m_up = new double[]{0,1,0};
			
			m_xaxis = new double[3];
			m_yaxis = new double[3];
			m_zaxis = new double[3];
			
			m_a = new double[4];
			m_b = new double[4];
			m_ab = new double[4];
			
			m_pos = new double[3];
			m_dir = new double[3];
			m_lookAt = new double[3];
			
			m_p = new double[4];
			m_q = new double[4];
			m_p0 = new double[4];
			m_p1 = new double[4];
			
			Identity(m_projectorR);
			
			m_pointList = new List<double[]>(32);
			for(int i = 0; i < 32; i++)
				m_pointList.Add(new double[3]);
			
			m_frustumCorners = new List<double[]>(8);
			for(int i = 0; i < 8; i++)
				m_frustumCorners.Add(new double[3]);
			
			m_quad = new List<double[]>(4);
			m_quad.Add(new double[]{0, 0, 0, 1});
			m_quad.Add(new double[]{1, 0, 0, 1});
			m_quad.Add(new double[]{1, 1, 0, 1});
			m_quad.Add(new double[]{0, 1, 0, 1});
			
			m_corners = new List<double[]>(8);
			// near
			m_corners.Add(new double[]{-1, -1, -1, 1}); 
			m_corners.Add(new double[]{ 1, -1, -1, 1}); 
			m_corners.Add(new double[]{ 1,  1, -1, 1});
			m_corners.Add(new double[]{-1,  1, -1, 1});
			// far
			m_corners.Add(new double[]{-1, -1, 1, 1});
			m_corners.Add(new double[]{ 1, -1, 1, 1});	
			m_corners.Add(new double[]{ 1,  1, 1, 1}); 
			m_corners.Add(new double[]{-1,  1, 1, 1});
			
		}
		
		/// <summary>
		/// Update the projection data for this camera.
		/// If this is the scene view camera you may not want to project 
		/// the grid from its point of view but instead from the main 
		/// cameras view so you can see how the mesh is being projected.
		/// </summary>
		public void UpdateProjection(Camera camera, CameraData data)
		{

            Camera cam = camera;

            if (cam == null || data == null) return;

			if(data.projection == null)
				data.projection = new ProjectionData();
			
			if(data.projection.IsViewUpdated(camera)) return;

            //Used to debug projection.
            if (Ocean.DISABLE_PROJECT_SCENE_VIEW && cam.name == "SceneCamera" && Camera.main != null)
                cam = Camera.main;

            //Aim the projector given the current camera position.
            //Find the most practical but visually pleasing projection.
            //Sets the m_projectorV and m_projectorP matrices.
            AimProjector(cam);
			
			//Create a view projection matrix.
			MulMatrixByMatrix(m_projectorVP, m_projectorP, m_projectorV);
			
			//Create the m_projectorR matrix. 
			//Finds the range the projection must fit 
			//for the projected grid to cover the screen.
			CreateRangeMatrix(cam, m_projectorVP);
			
			//Create the inverse view projection range matrix.
			Inverse(MATRIX_BUFFER0, m_projectorVP);
			MulMatrixByMatrix(m_projectorIVP, MATRIX_BUFFER0, m_projectorR);

            //Set the interpolation points based on IVP matrix.
            HProject(m_projectorIVP, m_quad[0], VECTOR_BUFFER);
            SetRow(0, m_projectorI, VECTOR_BUFFER);

            HProject(m_projectorIVP, m_quad[1], VECTOR_BUFFER);
            SetRow(1, m_projectorI, VECTOR_BUFFER);

            HProject(m_projectorIVP, m_quad[2], VECTOR_BUFFER);
            SetRow(2, m_projectorI, VECTOR_BUFFER);

            HProject(m_projectorIVP, m_quad[3], VECTOR_BUFFER);
            SetRow(3, m_projectorI, VECTOR_BUFFER);
			
			//Save a copy of the view projection range matrix and the interpolation matrix.
			Inverse(MATRIX_BUFFER0, m_projectorR);
			MulMatrixByMatrix(MATRIX_BUFFER1, MATRIX_BUFFER0, m_projectorVP);
			
			CopyMatrix(ref data.projection.projectorVP, MATRIX_BUFFER1);
			CopyMatrix(ref data.projection.interpolation, m_projectorI);

            data.projection.SetViewAsUpdated(camera);
			
		}
		
		/// <summary>
		/// The indices of each line segment in
		/// the frustum box.
		/// </summary>
		readonly static int[,] m_indices = 
		{
			{0,1}, {1,2}, {2,3}, {3,0}, 
			{4,5}, {5,6}, {6,7}, {7,4},
			{0,4}, {1,5}, {2,6}, {3,7}
		};
		
		/// <summary>
		/// The view matrix from the camera can not be used for the projection
		/// because it is possible to be in a invalid direction where the
		/// projection math does not work.
		/// Instead create a new view matrix that re-aims the view to a 
		/// direction that is always valid. I found a simple view looking down
		/// and a bit forward works best.
		/// </summary>
		void AimProjector(Camera cam)
		{
			
			//Copy camera projection into projector projection
			CopyMatrix(m_projectorP, cam.projectionMatrix);
			
			CopyVector3(m_pos, cam.transform.position);
			CopyVector3(m_dir, cam.transform.forward);

            //CopyMatrix(m_projectorV, cam.worldToCameraMatrix);
            //return;

            double level = m_ocean.level;

            double fit = (TightFit) ? 20.0 : 50.0;

			double range = Math.Max(0.0, m_ocean.FindMaxDisplacement(true)) + fit;

            if (Ocean.DISABLE_PROJECTION_FLIPPING)
            {
                //If flipping disabled keep the projection pos above the surface.
                //Make sure projection position is above the wave range.
                if (m_pos[1] < level) m_pos[1] = level;

                IsFlipped = false;
                m_pos[1] = Math.Max(m_pos[1], level + range);
            }
            else
            {
                //If the camera is below the sea level then flip the projection.
                //Make sure projection position is above or below the wave range.
                if (m_pos[1] < level)
                {
                    IsFlipped = true;
                    m_pos[1] = Math.Min(m_pos[1], level - range);
                }
                else
                {
                    IsFlipped = false;
                    m_pos[1] = Math.Max(m_pos[1], level + range);
                }
            }

			//Look a bit in front of view.
			m_lookAt[0] = m_pos[0] + m_dir[0] * 50.0;
			m_lookAt[1] = m_ocean.level;
			m_lookAt[2] = m_pos[2] + m_dir[2] * 50.0;
			
			LookAt(m_pos, m_lookAt, m_up);
			
		}
		
		/// <summary>
		/// Creates the range conversion matrix. The grid is projected
		/// onto a plane but the ocean waves are not flat. They occupy 
		/// a min/max range. This must be accounted for or else the grid 
		/// will pull away from the screen. The range matrix will then modify
		/// the projection matrix so that the projected grid always covers 
		/// the whole screen. This currently only takes the y displacement into 
		/// account, not the xz displacement. The xz is handled by another method.
		/// </summary>
		void CreateRangeMatrix(Camera cam, double[] projectorVP)
		{
			
			Identity(m_projectorR);
			
			//The inverse view projection matrix will transform
			//screen space verts to world space.
			CopyMatrix(MATRIX_BUFFER0, cam.worldToCameraMatrix);
			MulMatrixByMatrix(MATRIX_BUFFER1, m_projectorP, MATRIX_BUFFER0);
			Inverse(MATRIX_BUFFER0, MATRIX_BUFFER1);
			
			//MATRIX_BUFFER0 == ivp matrix
			
			double level = m_ocean.level;
			double range = Math.Max(1.0, m_ocean.FindMaxDisplacement(true));
			
			//Convert each screen vert to world space.
			for (int i = 0; i < 8; i++)
			{
				MulVector4ByMatrix(m_p, m_corners[i], MATRIX_BUFFER0);
				m_frustumCorners[i][0] = m_p[0] / m_p[3];
				m_frustumCorners[i][1] = m_p[1] / m_p[3];
				m_frustumCorners[i][2] = m_p[2] / m_p[3];
				
			}
			
			int count = 0;
			
			//For each corner if its world space position is  
			//between the wave range then add it to the list.
			for (int i = 0; i < 8; i++)
			{
				if (m_frustumCorners[i][1] <= level + range && m_frustumCorners[i][1] >= level - range)
				{
					m_pointList[count][0] = m_frustumCorners[i][0];
					m_pointList[count][1] = m_frustumCorners[i][1];
					m_pointList[count][2] = m_frustumCorners[i][2];
					
					count++;
				}
			}
			
			//Now take each segment in the frustum box and check
			//to see if it intersects the ocean plane on both the
			//upper and lower ranges.
			for (int i = 0; i < 12; i++)
			{
				int idx0 = m_indices[i, 0];
				m_p0[0] = m_frustumCorners[idx0][0];
				m_p0[1] = m_frustumCorners[idx0][1];
				m_p0[2] = m_frustumCorners[idx0][2];
				
				int idx1 = m_indices[i, 1];
				m_p1[0] = m_frustumCorners[idx1][0];
				m_p1[1] = m_frustumCorners[idx1][1];
				m_p1[2] = m_frustumCorners[idx1][2];
				
				if (SegmentPlaneIntersection(m_p0, m_p1, m_up, level + range, m_pointList[count]))
				{
					count++;
				}
				
				if (SegmentPlaneIntersection(m_p0, m_p1, m_up, level - range, m_pointList[count]))
				{
					count++;
				}
				
			}
			
			if(count > m_pointList.Count)
				throw new InvalidOperationException("Count can not be greater than poin list count");
			
			//If list is empty the ocean can not be seen.
			if (count == 0)
			{
				m_projectorR[0] = 1; m_projectorR[12] = 0;
				m_projectorR[5] = 1; m_projectorR[13] = 0;
				return;
			}
			
			double xmin = double.PositiveInfinity;
			double ymin = double.PositiveInfinity;
			double xmax = double.NegativeInfinity;
			double ymax = double.NegativeInfinity;
			
			//Now convert each world space position into
			//projector screen space. The min/max x/y values
			//are then used for the range conversion matrix.
			for(int i = 0; i < count; i++)
			{
				m_q[0] = m_pointList[i][0];
				m_q[1] = level;
				m_q[2] = m_pointList[i][2];
				m_q[3] = 1.0;
				
				MulVector4ByMatrix(m_p, m_q, projectorVP);
				m_p[0] /= m_p[3];
				m_p[1] /= m_p[3];
				
				if (m_p[0] < xmin) xmin = m_p[0];
				if (m_p[1] < ymin) ymin = m_p[1];
				if (m_p[0] > xmax) xmax = m_p[0];
				if (m_p[1] > ymax) ymax = m_p[1];
				
			}
			
			//Create the range conversion matrix and return it.
			m_projectorR[0] = xmax - xmin; m_projectorR[12] = xmin;
			m_projectorR[5] = ymax - ymin; m_projectorR[13] = ymin;
			
		}
		
		/// <summary>
		/// Project the corner point from projector space into 
		/// world space and find the intersection of the segment
		/// with the ocean plane in homogeneous space.
		/// The intersection is this grids corner's world pos.
		/// This is done in homogeneous space so the corners
		/// can be interpolated in the vert shader for the rest of
		/// the points. Homogeneous space is the world space but
		/// in 4D where the w value is the position on the infinity plane.
		/// </summary>
		void HProject(double[] ivp, double[] corner, double[] result)
		{
			
			corner[2] = -1;
			MulVector4ByMatrix(m_a, corner, ivp);
			
			corner[2] = 1;
			MulVector4ByMatrix(m_b, corner, ivp);
			
			double h = m_ocean.level;
			
			Sub4(m_ab, m_b, m_a);
			
			double t = (m_a[3] * h - m_a[1]) / (m_ab[1] - m_ab[3] * h);
			
			result[0] = m_a[0] + m_ab[0] * t;
			result[1] = m_a[1] + m_ab[1] * t;
			result[2] = m_a[2] + m_ab[2] * t;
			result[3] = m_a[3] + m_ab[3] * t;

		}
		
		/// <summary>
		/// Find the intersection point of a plane and segment in world space.
		/// </summary>
		bool SegmentPlaneIntersection(double[] a, double[] b, double[] n, double d, double[] q)
		{
			
			Sub3(m_ab, b, a);
			
			double t = (d - Dot3(n, a)) / Dot3(n, m_ab);
			
			if (t > -0.0 && t <= 1.0)
			{
				q[0] = a[0] + t * m_ab[0];
				q[1] = a[1] + t * m_ab[1];
				q[2] = a[2] + t * m_ab[2];
				
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Same as Unity's transform look at but in double precision.
		/// </summary>
		public void LookAt(double[] position, double[] target, double[] up)
		{
			
			Sub3(m_zaxis, position, target);
			Normalize3(m_zaxis);
			
			Cross3(m_xaxis, up, m_zaxis);
			Normalize3(m_xaxis);
			
			Cross3(m_yaxis, m_zaxis, m_xaxis);
			
			m_projectorV[0] = m_xaxis[0];
			m_projectorV[4] = m_xaxis[1];
			m_projectorV[8] = m_xaxis[2];
			m_projectorV[12] = -Dot3(m_xaxis, position);
			
			m_projectorV[1] = m_yaxis[0];
			m_projectorV[5] = m_yaxis[1];
			m_projectorV[9] = m_yaxis[2];
			m_projectorV[13] = -Dot3(m_yaxis, position);
			
			m_projectorV[2] = m_zaxis[0];
			m_projectorV[6] = m_zaxis[1];
			m_projectorV[10] = m_zaxis[2];
			m_projectorV[14] = -Dot3(m_zaxis, position);
			
			m_projectorV[3] = 0;
			m_projectorV[7] = 0;
			m_projectorV[11] = 0;
			m_projectorV[15] = 1;
			
			//Must flip to match Unity's winding order.
			m_projectorV[0] *= -1.0;
			m_projectorV[4] *= -1.0;
			m_projectorV[8] *= -1.0;
			m_projectorV[12] *= -1.0;
			
		}
		
		void MulVector4ByMatrix(double[] des, double[] src, double[] m)
		{
			des[0] = m[0] * src[0] + m[4] * src[1] + m[8] * src[2] + m[12] * src[3];
			des[1] = m[1] * src[0] + m[5] * src[1] + m[9] * src[2] + m[13] * src[3];
			des[2] = m[2] * src[0] + m[6] * src[1] + m[10] * src[2] + m[14] * src[3];
			des[3] = m[3] * src[0] + m[7] * src[1] + m[11] * src[2] + m[15] * src[3];
		}
		
		void MulMatrixByMatrix(double[] des, double[] m1, double[] m2)
		{
			
			for (int iRow = 0; iRow < 4; iRow++)
			{
				for (int iCol = 0; iCol < 4; iCol++)
				{
					
					des[iRow + iCol * 4] = m1[iRow + 0 * 4] * m2[0 + iCol * 4] +
						m1[iRow + 1 * 4] * m2[1 + iCol * 4] +
							m1[iRow + 2 * 4] * m2[2 + iCol * 4] +
							m1[iRow + 3 * 4] * m2[3 + iCol * 4];
				}
			}
		}
		
		void Identity(double[] m)
		{
			m[0] = 1.0; m[4] = 0.0; m[8] = 0.0; m[12] = 0.0;
			m[1] = 0.0; m[5] = 1.0; m[9] = 0.0; m[13] = 0.0;
			m[2] = 0.0; m[6] = 0.0; m[10] = 1.0; m[14] = 0.0;
			m[3] = 0.0; m[7] = 0.0; m[11] = 0.0; m[15] = 1.0;
		}
		
		void CopyMatrix(double[] des, Matrix4x4 m)
		{
			des[0] = m.m00; des[4] = m.m01; des[8] = m.m02; des[12] = m.m03;
			des[1] = m.m10; des[5] = m.m11; des[9] = m.m12; des[13] = m.m13;
			des[2] = m.m20; des[6] = m.m21; des[10] = m.m22; des[14] = m.m23;
			des[3] = m.m30; des[7] = m.m31; des[11] = m.m32; des[15] = m.m33;
		}
		
		void CopyMatrix(ref Matrix4x4 des, double[] m)
		{
			des.m00 = (float)m[0]; des.m01 = (float)m[4]; des.m02 = (float)m[8]; des.m03 = (float)m[12];
			des.m10 = (float)m[1]; des.m11 = (float)m[5]; des.m12 = (float)m[9]; des.m13 = (float)m[13];
			des.m20 = (float)m[2]; des.m21 = (float)m[6]; des.m22 = (float)m[10]; des.m23 = (float)m[14];
			des.m30 = (float)m[3]; des.m31 = (float)m[7]; des.m32 = (float)m[11]; des.m33 = (float)m[15];
		}
		
		void CopyVector3(double[] des, Vector3 v)
		{
			des[0] = v.x;
			des[1] = v.y;
			des[2] = v.z;
		}
		
		void Normalize3(double[] v)
		{
			
			double invLength = 1.0 / Math.Sqrt(v[0]*v[0] + v[1]*v[1] + v[2]*v[2]);
			
			v[0] *= invLength;
			v[1] *= invLength;
			v[2] *= invLength;
			
		}
		
		void Sub3(double[] des, double[] v1, double[] v2)
		{
			des[0] = v1[0] - v2[0];
			des[1] = v1[1] - v2[1];
			des[2] = v1[2] - v2[2];
		}
		
		void Sub4(double[] des, double[] v1, double[] v2)
		{
			des[0] = v1[0] - v2[0];
			des[1] = v1[1] - v2[1];
			des[2] = v1[2] - v2[2];
			des[3] = v1[3] - v2[3];
		}
		
		double Dot3(double[] v1, double[] v2)
		{
			return v1[0]*v2[0] + v1[1]*v2[1] + v1[2]*v2[2];
		}
		
		void Cross3(double[] des, double[] v1, double[] v2)
		{
			des[0] = v1[1]*v2[2] - v1[2]*v2[1];
			des[1] = v1[2]*v2[0] - v1[0]*v2[2];
			des[2] = v1[0]*v2[1] - v1[1]*v2[0];
		}
		
		/// <summary>
		/// The minor of a matrix. 
		/// </summary>
		double Minor(double[] m, int r0, int r1, int r2, int c0, int c1, int c2)
		{
			return 	m[r0 + c0 * 4] *(m[r1 + c1 * 4] * m[r2 + c2 * 4] - m[r2 + c1 * 4] * m[r1 + c2 * 4]) -
				m[r0 + c1 * 4] *(m[r1 + c0 * 4] * m[r2 + c2 * 4] - m[r2 + c0 * 4] * m[r1 + c2 * 4]) +
					m[r0 + c2 * 4] *(m[r1 + c0 * 4] * m[r2 + c1 * 4] - m[r2 + c0 * 4] * m[r1 + c1 * 4]);
		}
		
		/// <summary>
		/// The determinate of a matrix. 
		/// </summary>
		double Determinant(double[] m)
		{
			return (m[0] * Minor(m, 1, 2, 3, 1, 2, 3) -
			        m[4] * Minor(m, 1, 2, 3, 0, 2, 3) +
			        m[8] * Minor(m, 1, 2, 3, 0, 1, 3) -
			        m[12] * Minor(m, 1, 2, 3, 0, 1, 2));
		}
		
		/// <summary>
		/// The adjoint of a matrix. 
		/// </summary>
		void Adjoint(double[] des, double[] m)
		{
			
			des[0] = Minor(m, 1, 2, 3, 1, 2, 3);
			des[4] = -Minor(m, 0, 2, 3, 1, 2, 3);
			des[8] = Minor(m, 0, 1, 3, 1, 2, 3);
			des[12] = -Minor(m, 0, 1, 2, 1, 2, 3);
			
			des[1] = -Minor(m, 1, 2, 3, 0, 2, 3);
			des[5] = Minor(m, 0, 2, 3, 0, 2, 3);
			des[9] = -Minor(m, 0, 1, 3, 0, 2, 3);
			des[13] = Minor(m, 0, 1, 2, 0, 2, 3);
			
			des[2] = Minor(m, 1, 2, 3, 0, 1, 3);
			des[6] = -Minor(m, 0, 2, 3, 0, 1, 3);
			des[10] = Minor(m, 0, 1, 3, 0, 1, 3);
			des[14] = -Minor(m, 0, 1, 2, 0, 1, 3);
			
			des[3] = -Minor(m, 1, 2, 3, 0, 1, 2);
			des[7] = Minor(m, 0, 2, 3, 0, 1, 2);
			des[11] = -Minor(m, 0, 1, 3, 0, 1, 2);
			des[15] = Minor(m, 0, 1, 2, 0, 1, 2);
			
		}
		
		/// <summary>
		/// The inverse of the matrix.
		/// A matrix multiplied by its inverse is the identity.
		/// </summary>
		void Inverse(double[] des, double[] m)
		{
			
			Adjoint(des, m);
			
			double det = 1.0 / Determinant(m);
			
			for(int i = 0; i < 16; i++)
				des[i] = des[i] * det;
			
		}
		
		void SetRow(int i, double[] m, double[] v)
		{
			
			m[i + 0 * 4] = v[0];
			m[i + 1 * 4] = v[1];
			m[i + 2 * 4] = v[2];
			m[i + 3 * 4] = v[3];
			
		}
		
		
		
	}
	
}











