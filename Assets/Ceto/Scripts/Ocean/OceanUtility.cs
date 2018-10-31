using UnityEngine;
using System.Collections.Generic;


namespace Ceto
{

    /// <summary>
    /// Utility functions that dont fit else were.
    /// </summary>
    public static class OceanUtility
    {

	    /// <summary>
	    /// Flips the bit to turn layer on.
	    /// </summary>
		public static int ShowLayer(int mask, string layer) 
		{
			return mask | 1 << LayerMask.NameToLayer(layer);
		}

        /// <summary>
        /// Flips the bit to turn layer off.
        /// </summary>
		public static int HideLayer(int mask, string layer) 
		{
			return mask & ~(1 << LayerMask.NameToLayer(layer));
		}

        /// <summary>
        /// Flips the bit toggle layer.
        /// </summary>
		public static int ToggleLayer(int mask, string layer) 
		{
			return mask ^ 1 << LayerMask.NameToLayer(layer);
		}

		/// <summary>
		/// Flips the bit to turn layer on.
		/// </summary>
		public static int ShowLayer(int mask, LayerMask layer) 
		{
			return mask | 1 << layer;
		}
		
		/// <summary>
		/// Flips the bit to turn layer off.
		/// </summary>
		public static int HideLayer(int mask, LayerMask layer) 
		{
			return mask & ~(1 << layer);
		}
		
		/// <summary>
		/// Flips the bit toggle layer.
		/// </summary>
		public static int ToggleLayer(int mask, LayerMask layer) 
		{
			return mask ^ 1 << layer;
		}

        /// <summary>
        /// Creates a quad of 4 verts.
        /// </summary>
        public static Mesh CreateQuadMesh()
        {
            Vector3[] vertices = new Vector3[4];
            Vector2[] texcoords = new Vector2[4];
            int[] indices = new int[] { 0, 2, 1, 2, 3, 1 };

			vertices[0] = new Vector3(-1, 0, -1);
			vertices[1] = new Vector3(1, 0, -1);
			vertices[2] = new Vector3(-1, 0, 1);
            vertices[3] = new Vector3(1, 0, 1);

            texcoords[0] = new Vector2(0, 0);
            texcoords[1] = new Vector2(1, 0);
            texcoords[2] = new Vector2(0, 1);
            texcoords[3] = new Vector2(1, 1);

            Mesh mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.uv = texcoords;
            mesh.triangles = indices;

            return mesh;
        }

    }

}