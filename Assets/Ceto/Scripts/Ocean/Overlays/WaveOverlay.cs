using UnityEngine;
using System.Collections;

namespace Ceto
{

    public interface IOverlayParentBounds
    {
        Bounds BoundingBox { get; }

        bool BoundsChecked { get; set; }

        bool BoundsVisible { get; set; }
    }
	
	public class WaveOverlay
	{

        /// <summary>
        /// Set to true to have the overlay automatically removed by the manager.
        /// </summary>
        public bool Kill { get; set; }

		/// <summary>
		/// Set to true to not render overlay.
		/// </summary>
		public bool Hide { get; set; }

        /// <summary>
        /// The overlays centre position in world space.
        /// </summary>
		public Vector3 Position { get; set; }

        /// <summary>
        /// The overlays half size.
        /// Uses the half size as its easier to set the bounding box.
        /// </summary>
        public Vector2 HalfSize { get; set; }

        /// <summary>
        /// The rotation on the up axis in degrees.
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// The time in seconds that the overlay was created.
        /// </summary>
		public float Creation { get; protected set; }

        /// <summary>
        /// The age of the overlay in seconds.
        /// </summary>
		public float Age { get { return OceanTime() - Creation; } }

        /// <summary>
        /// The duration in seconds the overlay will last for.
        /// </summary>
		public float Duration { get; protected set; }

        /// <summary>
        /// The overlays age from 0 to 1.
        /// Where 0 would be its creation and 1 would
        /// be when its age is past the duration.
        /// </summary>
		public float NormalizedAge { get { return (Duration != 0.0) ? Mathf.Clamp01(Age / Duration) : 1.0f; } }

		/// <summary>
		/// The overlay quads corner positions in world space.
		/// </summary>
		public Vector4[] Corners { get; private set; }

        /// <summary>
        /// Allows the overlay to render heights onto the oceans.
        /// </summary>
		public OverlayHeightTexture HeightTex { get; set; }

        /// <summary>
        /// Allows the overlay to render normals onto the oceans.
        /// </summary>
		public OverlayNormalTexture NormalTex { get; set; }

        /// <summary>
        /// Allows the overlay to render foam onto the oceans.
        /// </summary>
		public OverlayFoamTexture FoamTex { get; set; }

		/// <summary>
		/// Allows the overlay clip areas of the oceans.
		/// </summary>
		public OverlayClipTexture ClipTex { get; set; }

        /// <summary>
        /// A matrix to transform the overlay
        /// from local to world space.
        /// </summary>
		public Matrix4x4 LocalToWorld { get; protected set; }

        /// <summary>
        /// The overlays bounding box in world space.
        /// </summary>
		public Bounds BoundingBox { get; protected set; }

        /// <summary>
        /// Stamp used to tell if bounds have been updated this frame.
        /// </summary>
        public int BoundsUpdateStamp { get; set; }

        /// <summary>
        /// The bounds of the parent object the overlay is in (ie a foam trail).
        /// </summary>
        public IOverlayParentBounds ParentBounds { get; set; }

        /// <summary>
        /// The distance the camera has to be to overlay for it to be drawn.
        /// </summary>
        public float DrawDistance { get; set; }

        /// <summary>
        /// Used to fade in the overlay as it the camera enters the draw distance.
        /// Stops it popping in. 
        /// This is set by the overlay manager and only used for the foam.
        /// </summary>
        public float DistanceAlpha { get; set; }

        /// <summary>
        /// New overlay at this position rotation and size.
        /// </summary>
		public WaveOverlay(Vector3 pos, float rotation, Vector2 halfSize, float duration)
		{
		
			Position = pos;
			HalfSize = halfSize;
			Rotation = rotation;
			Creation = OceanTime();
			Duration = Mathf.Max(duration, 0.001f);
            BoundsUpdateStamp = -1;
            DrawDistance = float.PositiveInfinity;

            Corners = new Vector4[4];

			HeightTex = new OverlayHeightTexture();
			NormalTex = new OverlayNormalTexture();
			FoamTex = new OverlayFoamTexture();
			ClipTex = new OverlayClipTexture();

			CalculateLocalToWorld();
			CalculateBounds();

		}

		/// <summary>
		/// 
		/// </summary>
		public WaveOverlay()
		{

			HalfSize = Vector2.one;
			Rotation = 0.0f;
			Creation = OceanTime();
			Duration = 0.001f;
            BoundsUpdateStamp = -1;
            DrawDistance = float.PositiveInfinity;

            Corners = new Vector4[4];
			
			HeightTex = new OverlayHeightTexture();
			NormalTex = new OverlayNormalTexture();
			FoamTex = new OverlayFoamTexture();
			ClipTex = new OverlayClipTexture();
			
			CalculateLocalToWorld();
			CalculateBounds();
			
		}

		float OceanTime()
		{
			if(Ocean.Instance == null) return 0.0f;
			return Ocean.Instance.OceanTime.Now;
		}

        /// <summary>
        /// Resets overlay as if it was created new.
        /// </summary>
        public void Reset(Vector3 pos, float rotation, Vector2 halfSize, float duration)
        {

            Position = pos;
            HalfSize = halfSize;
            Rotation = rotation;
            Creation = OceanTime();
            Duration = Mathf.Max(duration, 0.001f);
            Kill = false;
            Hide = false;

            CalculateLocalToWorld();
            CalculateBounds();

        }

        /// <summary>
        /// Update the overlay.
        /// Calculates the local to world matrix.
        /// </summary>
		public virtual void UpdateOverlay()
		{

			CalculateLocalToWorld();

		}

        /// <summary>
        /// Call to calculate the matrix
        /// from the position, rotation and size.
        /// </summary>
		public virtual void CalculateLocalToWorld()
		{

			Vector3 hs = new Vector3(HalfSize.x, 1.0f, HalfSize.y);

			LocalToWorld = Matrix4x4.TRS(new Vector3(Position.x, 0.0f, Position.z), Quaternion.Euler(0, Rotation, 0), hs);

		}

		static readonly Vector4[] CORNERS =
		{
			new Vector4(-1, 0, -1, 1), 
			new Vector4( 1, 0, -1, 1), 
			new Vector4( 1,  0, 1, 1),  
			new Vector4(-1,  0, 1, 1)
		};

        /// <summary>
        /// Calculates the bounding box
        /// from the position, rotation and size.
		/// Note - this is a AABB not OBB
        /// </summary>
		public virtual  void CalculateBounds()
		{

			
			Vector3 hs = new Vector3(HalfSize.x, 1.0f, HalfSize.y);
			float level = 0.0f;

			//Set the bounds height to the 
			//max possible wave height.
			if(Ocean.Instance != null)
			{
				level = Ocean.Instance.level;
				hs.y = Ocean.Instance.FindMaxDisplacement(true);
			}
			
			float xmin = float.PositiveInfinity;
			float zmin = float.PositiveInfinity;
			float xmax = float.NegativeInfinity;
			float zmax = float.NegativeInfinity;

			//Find the world space position of each corner.
			//Then find the min/max x and z for a AABB.
			for(int i = 0; i < 4; i++)
			{
				Corners[i] = LocalToWorld * CORNERS[i];
				Corners[i].y = level;
				
				if (Corners[i].x < xmin) xmin = Corners[i].x;
				if (Corners[i].z < zmin) zmin = Corners[i].z;
				
				if (Corners[i].x > xmax) xmax = Corners[i].x;
				if (Corners[i].z > zmax) zmax = Corners[i].z;
				
			}
			
			Vector3 c = new Vector3(Position.x, level, Position.z);

			Vector3 s = new Vector3(xmax-xmin, hs.y, zmax-zmin);

			//This bounding box contains the overlays corners.
			BoundingBox = new Bounds(c, s);

		}

		/// <summary>
		/// Does the overlay contain the xz point.
		/// </summary>
		public bool Contains(float x, float z)
		{
			float u, v;
			return Contains(x, z, out u, out v);
		}

		/// <summary>
		/// Does the overlay contain the xz point.
		/// If it does uv will be the texture coordinates 
		/// for the points position in overlay.
		/// If not in overlay uv will be set to 0.
		/// </summary>
		public bool Contains(float x, float z, out float u, out float v)
		{

			u = 0.0f;
			v = 0.0f;

			//Box is not a AABB so need to translate  
			//point to boxes local space.

			//Translate to local space.
			float px = x - Position.x;
			float pz = z - Position.z;
			float r = Rotation;

			//Rotate to local space.
			float ca = Mathf.Cos(r * Mathf.PI / 180.0f);
			float sa = Mathf.Sin(r * Mathf.PI / 180.0f);

			u = px * ca - pz * sa;
			v = px * sa + pz * ca;

			//Can now check intersection in local space.
			if(u > -HalfSize.x &&
			   u < HalfSize.x &&
			   v > -HalfSize.y &&
			   v < HalfSize.y)
			{
				//Convert to texture coords.
				u /= HalfSize.x;
				v /= HalfSize.y;
				u = u * 0.5f + 0.5f;
				v = v * 0.5f + 0.5f;

				return true;
			}
			else
			{
				return false;
			}
		}
		
	}
	
}











