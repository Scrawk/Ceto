using UnityEngine;
using UnityEditor;
using System;


using Ceto.Common.Containers.Interpolation;

namespace Ceto
{
    /// <summary>
    /// Editor window to export the shore masks from a terrain.
    /// </summary>
    public class ShoreMaskExporter : EditorWindow
    {

        GUIStyle m_boxStyle;
        GUIStyle m_wrapStyle;

        float m_oceanLevel;

        bool m_exportHeightMask = true;
        int m_heightMaskResolution = 1024;
        float m_heightMaskSpread = 10.0f;

        bool m_exportEdgeFoam = true;
        int m_edgeFoamResolution = 1024;
        float m_edgeFoamSpread = 2.0f;

        bool m_exportClipMask = true;
        int m_clipMaskResolution = 1024;
        float m_clipMaskOffset = 4.0f;

        bool m_exportNormalMask = false;
        int m_normalMaskResolution = 1024;
        float m_normalMaskSpread = 10.0f;

        bool m_exportFoamMask = false;
        int m_foamMaskResolution = 1024;
        float m_foamMaskSpread = 10.0f;

        [MenuItem("Window/Ceto/Shore Mask Exporter")]
        static void Init()
        {
            ShoreMaskExporter window = EditorWindow.GetWindow<ShoreMaskExporter>(false, "Ceto Shore Mask Exporter");
            window.Show();
        }

        void OnGUI()
        {

            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            if (m_wrapStyle == null)
            {
                m_wrapStyle = new GUIStyle(GUI.skin.label);
                m_wrapStyle.fontStyle = FontStyle.Normal;
                m_wrapStyle.wordWrap = true;
            }

            GUILayout.BeginVertical("Shore Mask Exporter", m_boxStyle);
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Creates and exports the mask textures for the active terrain. The textures will be saved in the asset folder.", m_wrapStyle);
            GUILayout.EndVertical();

            m_oceanLevel = EditorGUILayout.FloatField("Ocean Level", m_oceanLevel);
 
            m_exportHeightMask = EditorGUILayout.BeginToggleGroup("Export Height Mask", m_exportHeightMask);
            m_heightMaskResolution = Mathf.Clamp(EditorGUILayout.IntField("Height Mask Resolution", m_heightMaskResolution), 32, 4096);
            m_heightMaskSpread = Mathf.Max(0.1f, EditorGUILayout.FloatField("Height Mask Spread", m_heightMaskSpread));
            EditorGUILayout.EndToggleGroup();

            m_exportEdgeFoam = EditorGUILayout.BeginToggleGroup("Export Edge Foam", m_exportEdgeFoam);
            m_edgeFoamResolution = Mathf.Clamp(EditorGUILayout.IntField("Edge Foam Resolution", m_edgeFoamResolution), 32, 4096);
            m_edgeFoamSpread = Mathf.Max(0.1f, EditorGUILayout.FloatField("Edge Foam Spread", m_edgeFoamSpread));
            EditorGUILayout.EndToggleGroup();

            m_exportClipMask = EditorGUILayout.BeginToggleGroup("Export Clip Mask", m_exportClipMask);
            m_clipMaskResolution = Mathf.Clamp(EditorGUILayout.IntField("Clip Mask Resolution", m_clipMaskResolution), 32, 4096);
            m_clipMaskOffset = Mathf.Max(0.1f, EditorGUILayout.FloatField("Clip Mask Offset", m_clipMaskOffset));
            EditorGUILayout.EndToggleGroup();

            m_exportNormalMask = EditorGUILayout.BeginToggleGroup("Export Normal Mask", m_exportNormalMask);
            m_normalMaskResolution = Mathf.Clamp(EditorGUILayout.IntField("Normal Mask Resolution", m_normalMaskResolution), 32, 4096);
            m_normalMaskSpread = Mathf.Max(0.1f, EditorGUILayout.FloatField("Normal Mask Spread", m_normalMaskSpread));
            EditorGUILayout.EndToggleGroup();

            m_exportFoamMask = EditorGUILayout.BeginToggleGroup("Export Foam Mask", m_exportFoamMask);
            m_foamMaskResolution = Mathf.Clamp(EditorGUILayout.IntField("Foam Mask Resolution", m_foamMaskResolution), 32, 4096);
            m_foamMaskSpread = Mathf.Max(0.1f, EditorGUILayout.FloatField("Foam Mask Spread", m_foamMaskSpread));
            EditorGUILayout.EndToggleGroup();

            if (GUILayout.Button("Export Shore Masks"))
            {
                CreateShoreMasks();
                //AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
            }

        }


        void CreateShoreMasks()
        {

            Terrain terrain;

            GameObject obj = Selection.activeGameObject;

            //If obj is null user has not selected a object.
            if (obj == null)
            {
                string msg = string.Format("{0:MM/dd/yy H:mm:ss zzz} - No object was selected. Please select the terrain in the scene. The shore mask will not be created.", DateTime.Now);
                Ocean.LogInfo(msg);
                return;
            }

            terrain = obj.GetComponent<Terrain>();

            //If terrain is null user has not selected a object with a terrain component.
            if (terrain == null)
            {
                string msg = string.Format("{0:MM/dd/yy H:mm:ss zzz} - Selected object was not a terrain. The shore mask will not be created.", DateTime.Now);
                Ocean.LogInfo(msg);
                return;
            }

            //If terrain data is null user has deleted the data at some point.
            if (terrain.terrainData == null)
            {
                string msg = string.Format("{0:MM/dd/yy H:mm:ss zzz} - Terrains data is null. The shore mask will not be created.", DateTime.Now);
                Ocean.LogInfo(msg);
                return;
            }

            DateTime time = DateTime.Now;

            //Get the height map data from the terrain.
            float[] data = ShoreMaskGenerator.CreateHeightMap(terrain);

            int resolution = terrain.terrainData.heightmapResolution;

            //Export each of the masks if required.

            if (m_exportHeightMask)
                ExportMask(data, resolution, m_heightMaskResolution, terrain.name + "-HeightMask", time, m_heightMaskSpread, false, true);

            if (m_exportEdgeFoam)
                ExportMask(data, resolution, m_edgeFoamResolution, terrain.name + "-EdgeFoam", time, m_edgeFoamSpread, false, false);

            if (m_exportFoamMask)
                ExportMask(data, resolution, m_foamMaskResolution, terrain.name + "-FoamMask", time, m_foamMaskSpread, false, false);

            if (m_exportNormalMask)
                ExportMask(data, resolution, m_normalMaskResolution, terrain.name + "-NormalMask", time, m_normalMaskSpread, false, false);

            if (m_exportClipMask)
                ExportMask(data, resolution, m_clipMaskResolution, terrain.name + "-ClipMask", time, m_clipMaskOffset, true, true);

        }

        void ExportMask(float[] data, int size, int resolution, string name, DateTime stamp, float spread, bool isClip, bool isReadable)
        {

            //Create interpolated array from data so the mask resolution does not need to match the terrain height map size.
            InterpolatedArray2f heightMap = new InterpolatedArray2f(data, size, size, 1, false);

            //Create the path name to save mask.
            string fileName = string.Format("{0}-{1:yyyyMMdd-HHmmss}.png", name, stamp);
            string path = System.IO.Path.Combine(Application.dataPath, fileName);
            path = path.Replace('\\', '/');

            Texture2D mask = null;

            //Create the mask texture. Clip masks are created slightly different.
            if(isClip)
                mask = ShoreMaskGenerator.CreateClipMask(heightMap, resolution, resolution, m_oceanLevel + spread, TextureFormat.ARGB32);
            else
                mask = ShoreMaskGenerator.CreateMask(heightMap, resolution, resolution, m_oceanLevel, spread, TextureFormat.ARGB32);

            //Save the texture.
            byte[] bytes = mask.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);

            DestroyImmediate(mask);

            string relativePath = "Assets/" + fileName;

            //Update asset data base with new mask texture.
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

            //Create a importer from the texture so some of its settings can be changed.
            AssetImporter tmp = TextureImporter.GetAtPath(relativePath);
            TextureImporter importer = tmp as TextureImporter;

            if (importer != null)
            {
                //Change some of the settings of textures.
                //Should always be bilinear clamped with no mipmaps.
                //Height and clip masks should also be readable.
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.isReadable = isReadable;

                AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                string msg = string.Format("{0:MM/dd/yy H:mm:ss zzz} - Failed to modify texture settings after creation. You will need to manually adjust texture settings.", DateTime.Now);
                Ocean.LogInfo(msg);
            }


        }
    }

}