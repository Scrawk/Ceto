using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ceto
{

    /// <summary>
    /// This task will run a batch of querys on another thread. 
    /// </summary>
    public class ThreadedWaveQueryTask : WaveQueryTask
    {

        /// <summary>
        /// Threaded sampling of the overlays is not supported.
        /// </summary>
        public override bool SupportsOverlays { get { return false; } }

        public ThreadedWaveQueryTask(IEnumerable<WaveQuery> querys, Action<IEnumerable<WaveQuery>> callBack)
            : base(querys, callBack, true)
        {

        }

        /// <summary>
        /// Run the task. 
        /// Warning - this will not run on the main thread.
        /// </summary>
        public override IEnumerator Run()
        {

            var e = Querys.GetEnumerator();
            while (e.MoveNext())
            {

                //Task has been cancelled. Stop and return.
                if (Cancelled) break;

                WaveQuery query = e.Current;

                query.result.Clear();

                //Sample the spectrum waves.
                if (Displacements != null && query.SamplesSpectrum)
                    QueryDisplacements.QueryWaves(query, EnabledBuffers, Displacements, Scaling);
                
                query.result.height += OceanLevel;
            }

            FinishedRunning();
            return null;
        }

    }
}




