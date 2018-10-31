using UnityEngine;
using System.Collections;

namespace Ceto
{
    [RequireComponent(typeof(Camera))]
    public class AddRenderTarget : MonoBehaviour
    {

        public int scale = 2;

        void Start()
        {

            Camera cam = GetComponent<Camera>();

            cam.targetTexture = new RenderTexture(Screen.width / scale, Screen.height / scale, 24);

        }

        void OnGUI()
        {

            Camera cam = GetComponent<Camera>();

            if (cam.targetTexture == null) return;

            int width = cam.targetTexture.width;
            int height = cam.targetTexture.height;

            GUI.DrawTexture(new Rect(10, 10, width, height), cam.targetTexture);


        }


    }
}
