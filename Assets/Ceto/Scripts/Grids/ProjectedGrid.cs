using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

using Ceto.Common.Unity.Utility;

namespace Ceto
{

	/// <summary>
	/// Provides a mesh for the ocean using the projected grid method.
	/// This works by creating a grid in screen space and then projecting the mesh vertices
	/// into world space using the projector view matrix. This method has many advantages
	/// over other methods. The mesh is always where the camera looks so is infinite in size
	/// and the projection naturally places more verts close to the camera and less further 
	/// away. The method does have some downsides. The 'swimming' vertices are a issue and 
	/// the projection can pull away from the screen.
	/// 
	/// NOTE - The actual projection math is performed by the Projection object
	/// handled by the ocean script as the projection is also used for the
	/// overlay system.
	/// 
	/// </summary>
	[AddComponentMenu("Ceto/Components/ProjectedGrid")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Ocean))]
	public class ProjectedGrid : OceanComponent
    {

		static readonly int MAX_SCREEN_WIDTH = 2048;
		static readonly int MAX_SCREEN_HEIGHT = 2048;

		/// <summary>
		/// The resolution of the mesh.
		/// </summary>
		public MESH_RESOLUTION resolution = MESH_RESOLUTION.HIGH;

		/// <summary>
		/// The number of meshes the projected grid is split into.
		/// A higher density means more meshes and draw calls but
		/// since they are smaller they can the culled more easily.
		/// What ever the density the actual number of verts is the same
		/// for a given mesh resolution.
		/// </summary>
		/*public*/ GRID_GROUPS gridGroups = GRID_GROUPS.SINGLE;

		/// <summary>
		/// Does the mesh receive shadows.
		/// </summary>
		public bool receiveShadows = false;

		/// <summary>
		/// The length of the border added to edge of grid 
		/// to prevent it from pulling away from screen.
		/// Length is in world space.
		/// </summary>
		float borderLength = 200.0f;

		/// <summary>
		/// The mesh reflection probes usage.
		/// Currently disabled as untested. 
		/// </summary>
		ReflectionProbeUsage reflectionProbes = ReflectionProbeUsage.Off;

		/// <summary>
		/// The materials for the top of the ocean and the under side of the ocean.
		/// </summary>
		public Material oceanTopSideMat, oceanUnderSideMat;

		/// <summary>
		/// Holds the meshes created for each resolution. 
		/// Allows the mesh to be saved when the resolution changes
		/// so it does not need to be created if changed back.
		/// </summary>
		Dictionary<int, Grid> m_grids = new Dictionary<int, Grid>();

		/// <summary>
	 	/// A helper class to hold the data generated for each resolution used.
		/// </summary>
        public class Grid
        {
			// For each resolution the grid may be split up between 
			// multiple meshes as the mesh size maybe larger than Unitys 
			// max size for the higher resolutions.
			public int screenWidth;
			public int screenHeight;
			public int resolution;
			public int groups;
			public IList<MeshFilter> topFilters = new List<MeshFilter>();
			public IList<Renderer> topRenderer = new List<Renderer>();
            public IList<GameObject> top = new List<GameObject>();
            public IList<MeshFilter> underFilters = new List<MeshFilter>();
            public IList<Renderer> underRenderer = new List<Renderer>();
            public IList<GameObject> under = new List<GameObject>();
        }

		void Start()
		{

			try
			{

				//Needs at least SM3 for vertex texture fetches.
				if (SystemInfo.graphicsShaderLevel < 30)
					throw new InvalidOperationException("The projected grids needs at least SM3 to render.");

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

		}

		protected override void OnEnable()
		{

			base.OnEnable();

			if(WasError) return;

			try
			{
				// If enabled activate all meshes.
                var e = m_grids.GetEnumerator();
				while(e.MoveNext())
				{
                    Grid grid = e.Current.Value;
					Activate(grid.top, true);
					Activate(grid.under, true);
				}
			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

		}
		
		protected override void OnDisable()
		{

			base.OnDisable();

			try
			{
				// If disabled deactivate all meshes.
                var e = m_grids.GetEnumerator();
                while (e.MoveNext())
                {
                    Grid grid = e.Current.Value;
					Activate(grid.top, false);
					Activate(grid.under, false);
				}
			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
			
		}

        protected override void OnDestroy()
        {

            try
            {

                var e = m_grids.GetEnumerator();
                while (e.MoveNext())
                {
                    Grid grid = e.Current.Value;
                    ClearGrid(grid);
                }

                m_grids.Clear();
                m_grids = null;

            }
            catch (Exception e)
            {
                Ocean.LogError(e.ToString());
                WasError = true;
                enabled = false;
            }

        }

        void Update()
        {

			try
			{

				Shader.SetGlobalFloat("Ceto_GridEdgeBorder", Mathf.Max (0.0f, borderLength));

				int r = ResolutionToNumber(resolution);
				Vector2 screenGridSize = new Vector2(r / (float)ScreenWidth(), r / (float)ScreenHeight());
				Shader.SetGlobalVector("Ceto_ScreenGridSize", screenGridSize);

				//Check to see if the mesh has been created for this resolution setting.
				//If not create it.
	            CreateGrid(resolution);

                var e = m_grids.GetEnumerator();
                while (e.MoveNext())
                {
                    Grid grid = e.Current.Value;
					//If this mesh is the one for the current resolution setting.
                    bool active = e.Current.Key == (int)resolution;

					if(active)
					{
                        UpdateGrid(grid);
                        Activate(grid.top, true);
                        Activate(grid.under, true);
					}
					else
					{
						//Else it is not being rendered, disable it.
                        Activate(grid.top, false);
                        Activate(grid.under, false);
					}

	            }

				//If the underside not needed disable it.
				if(!UnderSideNeeded())
				{
                    e = m_grids.GetEnumerator();
                    while (e.MoveNext())
                    {
						Activate(e.Current.Value.under, false);
					}
				}

                //If the topside not needed disable it.
                if (!TopSideNeeded())
                {
                    e = m_grids.GetEnumerator();
                    while (e.MoveNext())
                    {
                        Activate(e.Current.Value.top, false);
                    }
                }

                //Check to see if the correct depth mode is being used for the opaque material.
                if (oceanTopSideMat != null && m_ocean.UnderWater != null && m_ocean.UnderWater.depthMode == DEPTH_MODE.USE_DEPTH_BUFFER)
                {
                    if(oceanTopSideMat.shader.isSupported && oceanTopSideMat.renderQueue <= 2500)
                    {
                        Ocean.LogWarning("Underwater depth mode must be USE_OCEAN_DEPTH_PASS if using opaque material. Underwater effect will not look correct.");
                    }
                }

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}
           
        }

        /// <summary>
        /// Called after this camera has rendered the ocean.
        /// </summary>
        public override void OceanOnPostRender(Camera cam, CameraData data)
		{

            if (!enabled || cam == null || data == null) return;

			Grid grid = null;
			m_grids.TryGetValue((int)resolution, out grid);

			if(grid == null) return;

			//Need to reset bounds after rendering so the next
			//camera to render grid will not cull it if the
			//bounds are not visible to it.
			ResetBounds(grid);
       
		}

        /// <summary>
        /// Activate/deactivate the these gameobjects.
        /// </summary>
        void Activate(IList<GameObject> list, bool active)
        {

            int count = list.Count;
            for(int i = 0; i < count; i++)
            {
                if(list[i] != null)
                    list[i].SetActive(active);
            }

        }

		/// <summary>
		/// Updates the grid. 
		/// Is called on the mesh each update.
		/// </summary>
		void UpdateGrid(Grid grid)
		{

			ResetBounds(grid);

            //Update the shadow and reflection probe settings
            int count = grid.topRenderer.Count;
            for (int i = 0; i < count; i++)
			{
                grid.topRenderer[i].receiveShadows = receiveShadows;
                grid.topRenderer[i].reflectionProbeUsage = reflectionProbes;

				if(oceanTopSideMat != null)
                    grid.topRenderer[i].sharedMaterial = oceanTopSideMat;
            }

            count = grid.underRenderer.Count;
            for (int i = 0; i < count; i++)
			{
                grid.underRenderer[i].receiveShadows = receiveShadows;
                grid.underRenderer[i].reflectionProbeUsage = reflectionProbes;

				if(oceanUnderSideMat != null)
                    grid.underRenderer[i].sharedMaterial = oceanUnderSideMat;
            }

			//The projected grid has no world space so it does not make sense to change the transform.
			//This may cause some issues if changed so zero transform before rendering.
			//The game object should be hidden so it is unlikely to be modified but just in case.
			count = grid.top.Count;
			for (int i = 0; i < count; i++)
			{
				grid.top[i].transform.localPosition = Vector3.zero;
				grid.top[i].transform.localRotation = Quaternion.identity;
				grid.top[i].transform.localScale = Vector3.one;
			}
			
			count = grid.under.Count;
			for (int i = 0; i < count; i++)
			{
				grid.under[i].transform.localPosition = Vector3.zero;
				grid.under[i].transform.localRotation = Quaternion.identity;
				grid.under[i].transform.localScale = Vector3.one;
			}

        }

		/// <summary>
		/// The mesh is always presumed to cover the ocean
		/// plane in a infinite xz direction so the bounds must be 
		/// set to very large xz values. The y range can be calculated 
		/// from the ocean max displacement and the ocean level.
		///
		/// Remember that the mesh is stored in screen space so 
		/// its default bounding box can not be used and as the 
		/// camera could in theory be anywhere the bounds must
		/// include all areas the camera could reasonably be 
		/// expected to be looking at.
		/// </summary>
		void ResetBounds(Grid grid)
		{
			
			float level = m_ocean.level;
			float range = m_ocean.FindMaxDisplacement(true);
			float bigNumber = 1e8f;
            int count = 0;

            Bounds bounds = new Bounds(Vector3.zero, new Vector3(bigNumber, level + range, bigNumber));

            count = grid.topFilters.Count;
			for (int i = 0; i < count; i++)
			{
				grid.topFilters[i].mesh.bounds = bounds;
			}

            count = grid.underFilters.Count;
            for (int i = 0; i < count; i++)
            {
                grid.underFilters[i].mesh.bounds = bounds;
            }
        }

		/// <summary>
		/// Updates the bounds for the camera that is currently rendering the mesh.
		/// </summary>
		void UpdateBounds(GameObject go, Camera cam)
		{

			//Now that it has been determined that this camera
			//is looking at the ocean plane re-adjust the bounds
			//to just cover the area the camera is looking at.
			//This was causing the shadows to not be rendered if
			//not done. 

			MeshFilter filter = go.GetComponent<MeshFilter>();

			if(filter == null) return;

			Vector3 pos = cam.transform.position;

			float level = m_ocean.level;
			float range = m_ocean.FindMaxDisplacement(true);
			float len = cam.farClipPlane * 2.0f;

			pos.y = level;

			filter.mesh.bounds = new Bounds(pos, new Vector3(len, range, len));

		}

        /// <summary>
        /// This will return true if the under side mesh is needed.
        /// If underwater component is not set or disable dont need underside.
        /// If mode above only dont need underside.
        /// </summary>
        bool UnderSideNeeded()
        {
            return !(m_ocean.UnderWater == null || 
                    !m_ocean.UnderWater.enabled || 
                    m_ocean.UnderWater.underwaterMode == UNDERWATER_MODE.ABOVE_ONLY);
        }

        /// <summary>
        /// This will return true if the top side mesh is needed.
        /// If underwater componet is not set cant tell what the 
        /// mode is so presume the topside is needed.
        /// If mode below only dont need topside.
        /// </summary>
        bool TopSideNeeded()
        {
            return m_ocean.UnderWater == null || 
                   m_ocean.UnderWater.underwaterMode != UNDERWATER_MODE.BELOW_ONLY;
        }

        /// <summary>
        /// Gets the screen width.
        /// Allows the width used to be changed if needed.
        /// </summary>
        int ScreenWidth()
		{
			return Mathf.Min(Screen.width, MAX_SCREEN_WIDTH);
		}

		/// <summary>
		/// Gets the screen height.
		/// Allows the height used to be changed if needed.
		/// </summary>
		int ScreenHeight()
		{
			return Mathf.Min(Screen.height, MAX_SCREEN_HEIGHT);
		}

		/// <summary>
		/// If called it means a grid is about to be rendered by the current camera.
		/// </summary>
        void ApplyProjection(GameObject go)
        {

			try
			{

				if(!enabled) return;

				Camera cam = Camera.current;
        
				if(cam == null) return;

                CameraData data = m_ocean.FindCameraData(cam);

				if(data.projection == null)
					data.projection = new ProjectionData();

                //If the projection for this camera has 
                //not been calculated yet do it now.
                if (!data.projection.IsViewUpdated(cam))
				{
                    //Update set to true in this function call
                    m_ocean.Projection.UpdateProjection(cam, data);

                    Shader.SetGlobalMatrix("Ceto_Interpolation", data.projection.interpolation);
					Shader.SetGlobalMatrix("Ceto_ProjectorVP", data.projection.projectorVP);
				}

                //If the projection has been flipped it will reverse
                //the mesh triangle so need to flip the cull face.
                if (!data.projection.checkedForFlipping)
                {
                    
                    int back = (int)CullMode.Back;
                    int front = (int)CullMode.Front;

                    if (!Ocean.DISABLE_PROJECTION_FLIPPING)
                    {

                        bool isFlipped = m_ocean.Projection.IsFlipped;

                        if (oceanTopSideMat != null)
                            oceanTopSideMat.SetInt("_CullFace", (isFlipped) ? front : back);

                        if (oceanUnderSideMat != null)
                            oceanUnderSideMat.SetInt("_CullFace", (isFlipped) ? back : front);
                    }
                    else
                    {
                        if (oceanTopSideMat != null)
                            oceanTopSideMat.SetInt("_CullFace", back);

                        if (oceanUnderSideMat != null)
                            oceanUnderSideMat.SetInt("_CullFace", front);
                    }

                    data.projection.checkedForFlipping = true;

                }

                UpdateBounds(go, cam);

			}
			catch(Exception e)
			{
				Ocean.LogError(e.ToString());
				WasError = true;
				enabled = false;
			}

        }

		/// <summary>
		/// Creates the grid for this resolution setting.
		/// </summary>
        void CreateGrid(MESH_RESOLUTION meshRes)
        {

            Grid grid = null;

            if(!m_grids.TryGetValue((int)meshRes, out grid))
            {
                grid = new Grid();
                m_grids.Add((int)meshRes, grid);
            }

            int screenWidth = ScreenWidth();
			int screenHeight = ScreenHeight();
			int resolution = ResolutionToNumber(meshRes);
			int groups = ChooseGroupSize(resolution, gridGroups, screenWidth, screenHeight);

			if(grid.screenWidth == screenWidth && grid.screenHeight == screenHeight && grid.groups == groups)
			{
				//Have already created a grid for this 
				//resolution at the current screen size
				return;
			}
			else
			{

				//Grid has either not been created or the screen has been resized.
				//Clear grid and create.
				ClearGrid(grid);

				grid.screenWidth = screenWidth;
				grid.screenHeight = screenHeight;
				grid.resolution = resolution;
				grid.groups = groups;
			}

            //Create the quads that will make up the grid.
            //The grid covers the screen and is split up 
            //into smaller quads. Each quads has a mesh.
            IList<Mesh> meshs = CreateScreenQuads(resolution, groups, screenWidth, screenHeight);

            //For each mesh make a gameobject for the top side and under side.
            foreach (Mesh mesh in meshs)
            {

				if(oceanTopSideMat != null)
				{

					GameObject top = new GameObject("Ceto TopSide Grid LOD: " + meshRes);
				
	                MeshFilter filter = top.AddComponent<MeshFilter>();
	                MeshRenderer renderer = top.AddComponent<MeshRenderer>();

					//The notify script will call the added functions when OnWillRender is called. 
					NotifyOnWillRender willRender = top.AddComponent<NotifyOnWillRender>();
		
					filter.sharedMesh = mesh;
	                renderer.shadowCastingMode = ShadowCastingMode.Off;
					renderer.receiveShadows = receiveShadows;
					renderer.sharedMaterial = oceanTopSideMat;
					renderer.reflectionProbeUsage = reflectionProbes;
					top.layer = LayerMask.NameToLayer(Ocean.OCEAN_LAYER);
					top.hideFlags = HideFlags.HideAndDontSave;

                    //Must render reflection first or it will cause so artefacts on ocean 
                    //for some reason. Its related to the overlays but exact cause is unknown.
                    willRender.AddAction(m_ocean.RenderReflection);

                    willRender.AddAction(ApplyProjection);
					willRender.AddAction(m_ocean.RenderWaveOverlays);
					willRender.AddAction(m_ocean.RenderOceanMask);
					willRender.AddAction(m_ocean.RenderOceanDepth);

                    grid.top.Add(top);
					grid.topRenderer.Add(renderer);
                    grid.topFilters.Add(filter);
				}

				if(oceanUnderSideMat)
				{

					GameObject under = new GameObject("Ceto UnderSide Grid LOD: " + meshRes);

					MeshFilter filter = under.AddComponent<MeshFilter>();
					MeshRenderer renderer = under.AddComponent<MeshRenderer>();

					//The notify script will call the added functions when OnWillRender is called.
					NotifyOnWillRender willRender = under.AddComponent<NotifyOnWillRender>();
		
					filter.sharedMesh = mesh;
	                renderer.shadowCastingMode = ShadowCastingMode.Off;
					renderer.receiveShadows = receiveShadows;
					renderer.reflectionProbeUsage = reflectionProbes;
                    renderer.sharedMaterial = oceanUnderSideMat;
					under.layer = LayerMask.NameToLayer(Ocean.OCEAN_LAYER);
					under.hideFlags = HideFlags.HideAndDontSave;

					willRender.AddAction(ApplyProjection);
					willRender.AddAction(m_ocean.RenderWaveOverlays);
					willRender.AddAction(m_ocean.RenderOceanMask);
					willRender.AddAction(m_ocean.RenderOceanDepth);

	                grid.under.Add(under);
					grid.underRenderer.Add(renderer);
                    grid.underFilters.Add(filter);
				}

                Destroy(mesh);

            }

        }

		/// <summary>
		/// Clears the grid.
		/// Deletes all the gameobjects for this grid.
		/// </summary>
		void ClearGrid(Grid grid)
		{

			if(grid == null) return;

			grid.screenWidth = 0;
			grid.screenHeight = 0;
			grid.resolution = 0;
			grid.groups = 0;

			if(grid.top != null)
			{
                int count = grid.top.Count;
                for(int i = 0; i < count; i++)
                {
                    if(grid.top[i] == null) continue;
                    Destroy(grid.top[i]);
                }
				
				grid.top.Clear();
			}

            if (grid.topFilters != null)
            {
                int count = grid.topFilters.Count;
                for (int i = 0; i < count; i++)
                {
                    if (grid.topFilters[i] == null) continue;
                    Destroy(grid.topFilters[i].mesh);
                }

                grid.topFilters.Clear();
            }

            if (grid.under != null)
			{
                int count = grid.under.Count;
                for (int i = 0; i < count; i++)
                {
                    if (grid.under[i] == null) continue;
                    Destroy(grid.under[i]);
                }
				
				grid.under.Clear();
            }

            if (grid.underFilters != null)
            {
                int count = grid.underFilters.Count;
                for (int i = 0; i < count; i++)
                {
                    if (grid.underFilters[i] == null) continue;
                    Destroy(grid.underFilters[i].mesh);
                }

                grid.underFilters.Clear();
            }

            if (grid.topRenderer != null)
				grid.topRenderer.Clear();

			if(grid.underRenderer != null)
				grid.underRenderer.Clear();

		}

		/// <summary>
		/// Converts the resolution enum to a number.
		/// The number is how many pixels make up one grid in the mesh.
		/// </summary>
        int ResolutionToNumber(MESH_RESOLUTION resolution)
        {

            switch (resolution)
            {

                case MESH_RESOLUTION.EXTREME:
                    return 1;

                case MESH_RESOLUTION.ULTRA:
                    return 2;

                case MESH_RESOLUTION.HIGH:
                    return 4;

                case MESH_RESOLUTION.MEDIUM:
                    return 8;

                case MESH_RESOLUTION.LOW:
                    return 16;

                default:
                    return 16;
            }

        }

		/// <summary>
		/// Converts the group enum to a number.
		/// The group number is the sqrt of the number of verts in each mesh at resolution of 1.
		/// It will require less meshes to fill the screen the bigger they are.
		/// </summary>
		int GroupToNumber(GRID_GROUPS groups)
		{

			switch(groups)
			{

			case GRID_GROUPS.EXTREME:
				return 128;

			case GRID_GROUPS.HIGH:
				return 196;
				
			case GRID_GROUPS.MEDIUM:
				return 256;
				
			case GRID_GROUPS.LOW:
				return 512;

			case GRID_GROUPS.SINGLE:
				//special case. Will try and create just 1 mesh.
				return -1; 
				
			default:
				return 128;
			}

		}

		/// <summary>
		/// Chooses the number of verts that can be in each mesh given the mesh resolution. 
		/// </summary>
		int ChooseGroupSize(int resolution, GRID_GROUPS groups, int width, int height)
		{
			int numVertsX = 0, numVertsY = 0;
			int groupSize = GroupToNumber(groups);
			
			if(groupSize == -1)
			{
				//If group size -1 try and create just a single mesh.
				numVertsX = width / resolution;
				numVertsY = height / resolution;
			}
			else
			{
				//Else work out how many verts will be in the group.
				numVertsX = groupSize / resolution;
				numVertsY = groupSize / resolution;
			}
			
			//If the number of verts is greater than Unitys max then will have to use a larger number of verts.
			while(numVertsX * numVertsY > 65000)
			{
				//This should never happen as the Extreme size should not be over max verts
				if(groups == GRID_GROUPS.EXTREME)
					throw new InvalidOperationException("Can not increase group size");
				
				int nextSize = (int)groups + 1;
				
				//Ocean.LogWarning("Mesh resolution to high for group size. Trying next group size of " + ((GRID_GROUPS)nextSize));

				groups = (GRID_GROUPS)nextSize;
				
				groupSize = GroupToNumber(groups);
				
				numVertsX = groupSize / resolution;
				numVertsY = groupSize / resolution;
			}

			//gridGroups = groups;

			return groupSize;

		}

		/// <summary>
		/// Creates the quads that make up the grid for this resolution setting.
		/// </summary>
		IList<Mesh> CreateScreenQuads(int resolution, int groupSize, int width, int height)
		{

			float w = 0.0f, h = 0.0f;
			int numVertsX = 0, numVertsY = 0, numX = 0, numY = 0;

			//Group size is sqrt of number of verts in mesh at resolution 1.
			//Work out how many meshes can fit in the screen at this size.
			if(groupSize != -1)
			{
				//Change size to be divisible by groups
				while(width % groupSize != 0) width++;
				while(height % groupSize != 0) height++;
				
				numVertsX = groupSize / resolution;
				numVertsY = groupSize / resolution;

				numX = width / groupSize;
				numY = height / groupSize;

				w = (float)groupSize / (float)width;
				h = (float)groupSize / (float)height;
			}
			else
			{
				numVertsX = width / resolution;
				numVertsY = height / resolution;
				numX = 1;
				numY = 1;
				w = 1.0f;
				h = 1.0f;
			}

			List<Mesh> quads = new List<Mesh>();
			
			for(int x = 0; x < numX; x++)
			{
				for(int y = 0; y < numY; y++)
				{
					float ux = (float)x * w;
					float uy = (float)y * h;

					Mesh quad = CreateQuad(numVertsX, numVertsY, ux, uy, w, h);
					quads.Add(quad);
					
				}
				
			}

			return quads;
			
		}

		/// <summary>
		/// Creates a mesh in screen space.
		/// The offset and scale is used to control the region of the screen the 
		/// mesh covers. The grid border value is calculated and stored in the uv's.
		/// The border is used to extend the edges of the grid a bit to prevent the xz
		/// displacement from pulling the mesh away from the screen.
		/// If the edge border is seen it can cause stretching of the mesh so it is best
		/// to try and minimize how often it can be seen but seeing the edge border is better
		/// than having the grid pull from the screen.
		/// </summary>
		public Mesh CreateQuad(int numVertsX, int numVertsY, float ux, float uy, float w, float h)
		{
			
			Vector3[] vertices = new Vector3[numVertsX * numVertsY];
			Vector2[] texcoords = new Vector2[numVertsX * numVertsY];
			int[] indices = new int[numVertsX * numVertsY * 6];
			
			//Percentage of verts that will be in the border.
			//Only a small number is needed.
			float border = 0.1f;
			
			for (int x = 0; x < numVertsX; x++)
			{
				for (int y = 0; y < numVertsY; y++)
				{

                    Vector2 uv = new Vector3((float)x / (float)(numVertsX - 1), (float)y / (float)(numVertsY - 1));
					
					uv.x *= w;
					uv.x += ux;
					
					uv.y *= h;
					uv.y += uy;

					if(!Ocean.DISABLE_PROJECTED_GRID_BORDER)
					{
						//Add border. Values outside of 0-1 are verts that will be in the border.
						uv.x = uv.x * (1.0f + border*2.0f) - border;
						uv.y = uv.y * (1.0f + border*2.0f) - border;
						
						//The screen uv is used for the interpolation to calculate the
						//world position from the interpolation matrix so must be in a 0-1 range.
						Vector2 screenUV = uv;
						screenUV.x = Mathf.Clamp01(screenUV.x);
						screenUV.y = Mathf.Clamp01(screenUV.y);
						
						//For the edge verts calculate the direction in screen space 
						//and normalize. Only the directions length is needed but store the
						//x and y direction because edge colors are output sometimes for debugging.
						Vector2 edgeDirection = uv;
						
						if(edgeDirection.x < 0.0f)
							edgeDirection.x = Mathf.Abs(edgeDirection.x) / border;
						else if(edgeDirection.x > 1.0f)
							edgeDirection.x = Mathf.Max(0.0f, edgeDirection.x-1.0f) / border;
						else
							edgeDirection.x = 0.0f;
						
						if(edgeDirection.y < 0.0f)
							edgeDirection.y = Mathf.Abs(edgeDirection.y) / border;
						else if(edgeDirection.y > 1.0f)
							edgeDirection.y = Mathf.Max(0.0f, edgeDirection.y-1.0f) / border;
						else
							edgeDirection.y = 0.0f;
						
						edgeDirection.x = Mathf.Pow(edgeDirection.x, 2);
						edgeDirection.y = Mathf.Pow(edgeDirection.y, 2);
						
						texcoords[x + y * numVertsX] = edgeDirection;
						vertices[x + y * numVertsX] = new Vector3(screenUV.x, screenUV.y, 0.0f);
					}
					else
					{
					
						texcoords[x + y * numVertsX] = new Vector2(0,0);
						vertices[x + y * numVertsX] = new Vector3(uv.x, uv.y, 0.0f);
					}
				}
			}
			
			int num = 0;
			for (int x = 0; x < numVertsX - 1; x++)
			{
				for (int y = 0; y < numVertsY - 1; y++)
				{
					indices[num++] = x + y * numVertsX;
					indices[num++] = x + (y + 1) * numVertsX;
					indices[num++] = (x + 1) + y * numVertsX;
					
					indices[num++] = x + (y + 1) * numVertsX;
					indices[num++] = (x + 1) + (y + 1) * numVertsX;
					indices[num++] = (x + 1) + y * numVertsX;
				}
			}
			
			Mesh mesh = new Mesh();
			
			mesh.vertices = vertices;
			mesh.uv = texcoords;
			mesh.triangles = indices;
            mesh.name = "Ceto Projected Grid Mesh";
            mesh.hideFlags = HideFlags.HideAndDontSave;
            ;

			return mesh;
		}

    }


}







