using UnityEngine;
using System.Collections.Generic;


namespace Ceto
{

    /// <summary>
    /// The base class for the ocean data classes.
    /// Manages if the data has been updated for the current camera view.
    /// </summary>
    public abstract class ViewData
    {

        //HashSet<Matrix4x4> m_views = new HashSet<Matrix4x4>();

        bool m_updated;

        public bool IsViewUpdated(Camera cam)
        {
            return m_updated;
            //return m_views.Contains(cam.worldToCameraMatrix);
        }

        public void ClearUpdatedViews()
        {
            m_updated = false;
            //m_views.Clear();
        }

        public void SetViewAsUpdated(Camera cam)
        {
            m_updated = true;
            //m_views.Add(cam.worldToCameraMatrix);
        }



    }

}
