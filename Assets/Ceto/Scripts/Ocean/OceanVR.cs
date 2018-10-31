#if !( UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 )
#define UNITY_540_OR_HIGHER
#endif

using UnityEngine;
using UnityEngine.VR;
using System.Collections;

namespace Ceto
{

    public static class OceanVR
    {

        public static bool OpenVRInUse { get; private set; }


        public static void Initialize()
        {

#if UNITY_540_OR_HIGHER && CETO_USE_STEAM_VR
            OpenVRInUse = VRSettings.loadedDeviceName == "OpenVR";
#endif

        }

#if UNITY_540_OR_HIGHER && CETO_USE_STEAM_VR
        public static void GetSteamVRLeftEye(Camera cam, out Vector3 position, out Quaternion rotation, out Matrix4x4 projection)
        {

            position = cam.transform.TransformPoint(SteamVR.instance.eyes[0].pos);
            rotation = cam.transform.rotation * SteamVR.instance.eyes[0].rot;
            projection = GetSteamVRProjectionMatrix(cam, Valve.VR.EVREye.Eye_Left);

        }

        public static void GetSteamVRRightEye(Camera cam, out Vector3 position, out Quaternion rotation, out Matrix4x4 projection)
        {

            position = cam.transform.TransformPoint(SteamVR.instance.eyes[1].pos);
            rotation = cam.transform.rotation * SteamVR.instance.eyes[1].rot;
            projection = GetSteamVRProjectionMatrix(cam, Valve.VR.EVREye.Eye_Right);

        }

        public static Matrix4x4 GetSteamVRProjectionMatrixLeftEye(Camera cam)
        {
            return GetSteamVRProjectionMatrix(cam, Valve.VR.EVREye.Eye_Left);
        }

        public static Matrix4x4 GetSteamVRProjectionMatrixRightEye(Camera cam)
        {
            return GetSteamVRProjectionMatrix(cam, Valve.VR.EVREye.Eye_Right);
        }

        static Matrix4x4 GetSteamVRProjectionMatrix(Camera cam, Valve.VR.EVREye eye)
        {
            Valve.VR.HmdMatrix44_t proj = SteamVR.instance.hmd.GetProjectionMatrix(eye, cam.nearClipPlane, cam.farClipPlane, SteamVR.instance.graphicsAPI);
            Matrix4x4 m = new Matrix4x4();
            m.m00 = proj.m0;
            m.m01 = proj.m1;
            m.m02 = proj.m2;
            m.m03 = proj.m3;
            m.m10 = proj.m4;
            m.m11 = proj.m5;
            m.m12 = proj.m6;
            m.m13 = proj.m7;
            m.m20 = proj.m8;
            m.m21 = proj.m9;
            m.m22 = proj.m10;
            m.m23 = proj.m11;
            m.m30 = proj.m12;
            m.m31 = proj.m13;
            m.m32 = proj.m14;
            m.m33 = proj.m15;
            return m;
        }
#endif

    }

}
