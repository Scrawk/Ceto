
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Ceto
{

    /// <summary>
    /// 
    /// </summary>
    public class RefractionCommand : IRefractionCommand
    {

        public class Command
        {
            public CommandBuffer buffer;
            public Material material;
            public bool enabled;
            public CameraEvent camEvent;
        }

        Command CopyScreenCmd { get; set; }

        Command CopyDepthCmd { get; set; }

        Command NormalFadeCmd { get; set; }

        /// <summary>
        /// Disables the copy depth command.
        /// Use this if providing your own depth buffer grab.
        /// </summary>
        public bool DisableCopyDepthCmd
        {
            get { return CopyDepthCmd.enabled;}
            set { CopyDepthCmd.enabled = !value; }
        }

        /// <summary>
        /// Disables the normal fade command.
        /// Used for the caustics.
        /// </summary>
        public bool DisableNormalFadeCmd
        {
            get { return NormalFadeCmd.enabled; }
            set { NormalFadeCmd.enabled = !value; }
        }

        Camera m_camera;

        /// <summary>
        /// 
        /// </summary>
        public RefractionCommand(Camera cam, Shader copyDepth, Shader normalFade)
        {

            CopyScreenCmd = new Command();

            CopyDepthCmd = new Command();

            NormalFadeCmd = new Command();

            m_camera = cam;

            CopyDepthCmd.material = new Material(copyDepth);
            CopyDepthCmd.enabled = true;

            NormalFadeCmd.material = new Material(normalFade);
            NormalFadeCmd.enabled = true;
        }

        /// <summary>
        /// Clear all commands from camera.
        /// </summary>
        public void ClearCommands()
        {

            if (CopyScreenCmd.buffer != null)
            {
                m_camera.RemoveCommandBuffer(CopyScreenCmd.camEvent, CopyScreenCmd.buffer);
                CopyScreenCmd.buffer = null;
            }

            if (CopyDepthCmd.buffer != null)
            {
                m_camera.RemoveCommandBuffer(CopyDepthCmd.camEvent, CopyDepthCmd.buffer);
                CopyDepthCmd.buffer = null;
            }

            if (NormalFadeCmd.buffer != null)
            {
                m_camera.RemoveCommandBuffer(NormalFadeCmd.camEvent, NormalFadeCmd.buffer);
                NormalFadeCmd.buffer = null;
            }

        }

        /// <summary>
        /// Create or remove commands if needed.
        /// </summary>
        public void UpdateCommands()
        {

            RenderingPath path = m_camera.actualRenderingPath;

            CameraEvent copyScreenEvent, copyDepthEvent, normalFadeEvent;
            int normalFadePass;

            if (path == RenderingPath.DeferredShading)
            {
                copyScreenEvent = CameraEvent.AfterSkybox;
                copyDepthEvent = CameraEvent.AfterLighting;
                normalFadeEvent = CameraEvent.AfterLighting;
                normalFadePass = 1;
            }
            else if (path == RenderingPath.DeferredLighting)
            {
                //Legacy defered rendering.
                copyScreenEvent = CameraEvent.AfterSkybox;
                copyDepthEvent = CameraEvent.AfterLighting;
                normalFadeEvent = CameraEvent.AfterLighting;
                normalFadePass = 0;
            }
            else 
            {
                //Forward Rendering
                copyScreenEvent = CameraEvent.AfterSkybox;
                copyDepthEvent = CameraEvent.AfterDepthTexture;
                normalFadeEvent = CameraEvent.AfterDepthNormalsTexture;
                normalFadePass = 0;

            }

            //If the camera rendering path has changed or command has been disabled clear the command

            if ((!CopyScreenCmd.enabled || CopyScreenCmd.camEvent != copyScreenEvent) && CopyScreenCmd.buffer != null)
            {
                m_camera.RemoveCommandBuffer(CopyScreenCmd.camEvent, CopyScreenCmd.buffer);
                CopyScreenCmd.buffer = null;
            }

            if ((!CopyDepthCmd.enabled || CopyDepthCmd.camEvent != copyDepthEvent) && CopyDepthCmd.buffer != null)
            {
                m_camera.RemoveCommandBuffer(CopyDepthCmd.camEvent, CopyDepthCmd.buffer);
                CopyDepthCmd.buffer = null;
            }

            if ((!NormalFadeCmd.enabled || NormalFadeCmd.camEvent != normalFadeEvent) && NormalFadeCmd.buffer != null)
            {
                m_camera.RemoveCommandBuffer(NormalFadeCmd.camEvent, NormalFadeCmd.buffer);
                NormalFadeCmd.buffer = null;
            }

            //Create the command to grab the screen color for the refractions.
            //NOTE - THIS IS NOT CURRENTLY USED AND IS ALWAYS DISABLED

            if (CopyScreenCmd.enabled && CopyScreenCmd.buffer == null)
            {

                RenderTextureFormat format;

                if (m_camera.allowHDR)
                    format = RenderTextureFormat.ARGBHalf;
                else
                    format = RenderTextureFormat.ARGB32;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "Ceto CopyScreen Cmd: " + m_camera.name;

                int grabID = Shader.PropertyToID("Ceto_CopyScreenTexture_Tmp");
			    cmd.GetTemporaryRT(grabID, -1, -1, 0, FilterMode.Bilinear, format, RenderTextureReadWrite.Default);
			    cmd.Blit(BuiltinRenderTextureType.CurrentActive, grabID);
                cmd.SetGlobalTexture(Ocean.REFRACTION_GRAB_TEXTURE_NAME, grabID);

                CopyScreenCmd.buffer = cmd;
                CopyScreenCmd.camEvent = copyScreenEvent;

                m_camera.AddCommandBuffer(CopyScreenCmd.camEvent, CopyScreenCmd.buffer);
            }

            //Create the command to copy the depth buffer.

            if (CopyDepthCmd.enabled && CopyDepthCmd.buffer == null)
            {

                RenderTextureFormat format = RenderTextureFormat.RFloat;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.RHalf;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGBHalf;

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "Ceto Copy Depth Cmd: " + m_camera.name;

                int depthID = Shader.PropertyToID("Ceto_DepthCopyTexture_Tmp");
                cmd.GetTemporaryRT(depthID, -1, -1, 0, FilterMode.Point, format, RenderTextureReadWrite.Linear);
                cmd.Blit(BuiltinRenderTextureType.CurrentActive, depthID, CopyDepthCmd.material, 0);
                cmd.SetGlobalTexture(Ocean.DEPTH_GRAB_TEXTURE_NAME, depthID);
      
                CopyDepthCmd.buffer = cmd;
                CopyDepthCmd.camEvent = copyDepthEvent;

                m_camera.AddCommandBuffer(CopyDepthCmd.camEvent, CopyDepthCmd.buffer);
            }

            //Create the command to used the screen normals to create the normal fade.
            //This is used for the caustics so they are only applied to the top of objects.

            if (NormalFadeCmd.enabled && NormalFadeCmd.buffer == null)
            {

                RenderTextureFormat format = RenderTextureFormat.R8;

                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "Ceto Normal Fade Cmd: " + m_camera.name;

                int normalFadeID = Shader.PropertyToID("Ceto_NormalFadeTexture_Tmp");
                cmd.GetTemporaryRT(normalFadeID, -1, -1, 0, FilterMode.Bilinear, format, RenderTextureReadWrite.Linear);
                cmd.Blit(BuiltinRenderTextureType.CurrentActive, normalFadeID, NormalFadeCmd.material, normalFadePass);
                cmd.SetGlobalTexture(Ocean.NORMAL_FADE_TEXTURE_NAME, normalFadeID);

                NormalFadeCmd.buffer = cmd;
                NormalFadeCmd.camEvent = normalFadeEvent;

                m_camera.AddCommandBuffer(NormalFadeCmd.camEvent, NormalFadeCmd.buffer);
            }

            if(NormalFadeCmd.buffer != null)
            {
                //Need the cam model veiw to convert screen normals to world normals
                NormalFadeCmd.material.SetMatrix("Ceto_NormalFade_MV", m_camera.cameraToWorldMatrix);
            }

        }


    }

}




