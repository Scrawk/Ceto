using UnityEngine;
using System.Collections;

namespace Ceto
{
    public static class DrawCameraFrustum
    {

        static Vector4[] box = new Vector4[]
        {
            new Vector4(-1, -1, -1, 1),
            new Vector4(-1, 1, -1, 1),
            new Vector4(1, 1, -1, 1),
            new Vector4(1, -1, -1, 1),

            new Vector4(-1, -1, 1, 1),
            new Vector4(-1, 1, 1, 1),
            new Vector4(1, 1, 1, 1),
            new Vector4(1, -1, 1, 1)
        };


        static int[,] indexs = new int[,]
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };

        static public void DrawFrustum(Matrix4x4 projectionView, Color col)
        {

            Vector4[] positions = new Vector4[8];

            Matrix4x4 IVP = (projectionView).inverse;

            for (int i = 0; i < 8; i++)
            {
                positions[i] = IVP * box[i];
                positions[i] /= positions[i].w;
            }

            Gizmos.color = col;

            for (int i = 0; i < indexs.GetLength(0); i++)
                Gizmos.DrawLine(positions[indexs[i, 0]], positions[indexs[i, 1]]);

        }


    }
}
