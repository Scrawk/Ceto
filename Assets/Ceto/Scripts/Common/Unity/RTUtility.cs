using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ceto.Common.Unity.Utility
{

    public class RTSettings
    {

        public string name = "";
        public int width = 1;
        public int height = 1;
        public int depth = 0;
        public int ansioLevel = 1;
        public bool mipmaps = false;
        public bool randomWrite = false;
        public RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        public TextureWrapMode wrap = TextureWrapMode.Clamp;
        public FilterMode filer = FilterMode.Bilinear;
        public RenderTextureFormat format = RenderTextureFormat.ARGB32;
        public List<RenderTextureFormat> fallbackFormats = new List<RenderTextureFormat>();

    }

	static public class RTUtility
	{
		
		static public void Blit(RenderTexture des, Material mat, int pass = 0)
	    {

	        //RenderTexture oldRT = RenderTexture.active;

	        Graphics.SetRenderTarget(des);

	        GL.PushMatrix();
	        GL.LoadOrtho();

			mat.SetPass(pass);

	        GL.Begin(GL.QUADS);
	        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
	        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
	        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
	        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
	        GL.End();

	        GL.PopMatrix();

	        //RenderTexture.active = oldRT;
	    }

		static public void Blit(RenderTexture des, Material mat, Vector3[] verts, int pass = 0)
		{
            
            //RenderTexture oldRT = RenderTexture.active;
			
			Graphics.SetRenderTarget(des);

			GL.PushMatrix();
			GL.LoadOrtho();
			
			mat.SetPass(pass);
			
			GL.Begin(GL.QUADS);
			GL.TexCoord2(0.0f, 0.0f); GL.Vertex(verts[0]);
			GL.TexCoord2(1.0f, 0.0f); GL.Vertex(verts[1]);
			GL.TexCoord2(1.0f, 1.0f); GL.Vertex(verts[2]);
			GL.TexCoord2(0.0f, 1.0f); GL.Vertex(verts[3]);
			GL.End();
			
			GL.PopMatrix();
			
			//RenderTexture.active = oldRT;
		}

		static public void Blit(RenderTexture des, Material mat, Vector3[] verts, Vector2[] uvs, int pass = 0)
		{
            
            //RenderTexture oldRT = RenderTexture.active;
			
			Graphics.SetRenderTarget(des);

			GL.PushMatrix();
			GL.LoadOrtho();
			
			mat.SetPass(pass);
			
			GL.Begin(GL.QUADS);
			GL.TexCoord(uvs[0]); GL.Vertex(verts[0]);
			GL.TexCoord(uvs[1]); GL.Vertex(verts[1]);
			GL.TexCoord(uvs[2]); GL.Vertex(verts[2]);
			GL.TexCoord(uvs[3]); GL.Vertex(verts[3]);
			GL.End();
			
			GL.PopMatrix();
			
			//RenderTexture.active = oldRT;
		}
		
	    static public void MultiTargetBlit(IList<RenderTexture> des, Material mat, int pass = 0)
	    {
            
            //RenderTexture oldRT = RenderTexture.active;
			
			RenderBuffer[] rb = new RenderBuffer[des.Count];
			
			for(int i = 0; i < des.Count; i++)
				rb[i] = des[i].colorBuffer;

	        Graphics.SetRenderTarget(rb, des[0].depthBuffer);

	        GL.PushMatrix();
	        GL.LoadOrtho();
			
			mat.SetPass(pass);

	        GL.Begin(GL.QUADS);
	        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
	        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
	        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
	        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
	        GL.End();

	        GL.PopMatrix();

	        //RenderTexture.active = oldRT;
	    }

        static public void MultiTargetBlit(RenderBuffer[] des_rb, RenderBuffer des_db, Material mat, int pass = 0)
	    {
            
            //RenderTexture oldRT = RenderTexture.active;
			
	        Graphics.SetRenderTarget(des_rb, des_db);

	        GL.PushMatrix();
	        GL.LoadOrtho();
			
			mat.SetPass(pass);

	        GL.Begin(GL.QUADS);
	        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
	        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
	        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
	        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
            GL.End();

	        GL.PopMatrix();

	        //RenderTexture.active = oldRT;
	    }

		static public void ClearColor(RenderTexture tex, Color col)
		{
            if (tex == null) return;
    
            //RenderTexture oldRT = RenderTexture.active;

			if (!SystemInfo.SupportsRenderTextureFormat(tex.format)) return;

            Graphics.SetRenderTarget(tex);
			GL.Clear(false, true, col);

			//RenderTexture.active = oldRT;
		}

		static public void Release(RenderTexture tex)
		{
			if(tex == null) return;
			tex.Release();
		}

		static public void Release(IList<RenderTexture> texList)
		{

			if(texList == null) return;

            int count = texList.Count;
			for(int i = 0; i < count; i++)
			{
				if(texList[i] == null) continue;
                texList[i].Release();
			}

		}

        static public void ReleaseAndDestroy(RenderTexture tex)
        {
            if (tex == null) return;
            tex.Release();
            UnityEngine.Object.Destroy(tex);
        }

        static public void ReleaseAndDestroy(IList<RenderTexture> texList)
        {

            if (texList == null) return;

            int count = texList.Count;
            for (int i = 0; i < count; i++)
            {
                if (texList[i] == null) continue;
                texList[i].Release();
                UnityEngine.Object.Destroy(texList[i]);
            }

        }

        static public void ReleaseTemporary(RenderTexture tex)
		{
			if(tex == null) return;
			RenderTexture.ReleaseTemporary(tex);
		}

		static public void ReleaseTemporary(IList<RenderTexture> texList)
		{

			if(texList == null) return;

            int count = texList.Count;
            for (int i = 0; i < count; i++)
            {
                if (texList[i] == null) continue;
                RenderTexture.ReleaseTemporary(texList[i]);
            }

        }

        static RenderTextureFormat CheckFormat(RTSettings setting)
        {

            RenderTextureFormat format = setting.format;

            if (!SystemInfo.SupportsRenderTextureFormat(format))
            {

                Debug.Log("System does not support " + format + " render texture format.");

                bool foundFallback = false;
                int count = setting.fallbackFormats.Count;
                for (int i = 0; i < count; i++)
                {
                    if (SystemInfo.SupportsRenderTextureFormat(setting.fallbackFormats[i]))
                    {
                        format = setting.fallbackFormats[i];
                        Debug.Log("Found fallback format: " + format);
                        foundFallback = true;
                        break;
                    }
                }

                if (!foundFallback)
                {
                    throw new InvalidOperationException("Could not find fallback render texture format");
                }

            }

            return format;
        }

        static public RenderTexture CreateRenderTexture(RTSettings setting)
        {

            if (setting == null)
                throw new NullReferenceException("RTSettings is null");

            RenderTextureFormat format = CheckFormat(setting);

            RenderTexture tex = new RenderTexture(setting.width, setting.height, setting.depth, format, setting.readWrite);

            tex.name = setting.name;
            tex.wrapMode = setting.wrap;
            tex.filterMode = setting.filer;
            tex.useMipMap = setting.mipmaps;
            tex.anisoLevel = setting.ansioLevel;
            tex.enableRandomWrite = setting.randomWrite;

            return tex;

        }

        static public RenderTexture CreateTemporyRenderTexture(RTSettings setting)
        {

            if (setting == null)
                throw new NullReferenceException("RTSettings is null");

            RenderTextureFormat format = CheckFormat(setting);

            RenderTexture tex = RenderTexture.GetTemporary(setting.width, setting.height, setting.depth, format, setting.readWrite);

            tex.name = setting.name;
            tex.wrapMode = setting.wrap;
            tex.filterMode = setting.filer;
            //tex.useMipMap = setting.mipmaps;
            tex.anisoLevel = setting.ansioLevel;
            //tex.enableRandomWrite = setting.randomWrite;

            return tex;

        }

    }

}
















