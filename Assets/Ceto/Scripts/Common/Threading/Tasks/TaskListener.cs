
using System.Collections.Generic;

namespace Ceto.Common.Threading.Tasks
{
    /// <summary>
    /// Allows a task to listen on for when
    /// other tasks finish. When all tasks 
    /// this task is listening to are finished
    /// the tasks StopWaiting function is called.
    /// </summary>
	public class TaskListener
	{
        /// <summary>
        /// The task that is listening.
        /// </summary>
		public ThreadedTask ListeningTask { get { return m_task; } }
		ThreadedTask m_task;

        /// <summary>
        /// How many tasks the task is waiting on.
        /// </summary>
		public int Waiting 
		{ 
			get { return m_waiting; }
			set { m_waiting = value; }
		}
		volatile int m_waiting;

        /// <summary>
        /// Create a new listener.
        /// </summary>
        /// <param name="task">The task that is listening.</param>
		public TaskListener(ThreadedTask task)
		{
			m_task = task;
		}

        /// <summary>
        /// Called when any of the tasks this task is listening
        /// on have finished. Once waiting reaches 0 the task
        /// stops waiting.
        /// </summary>
		public void OnFinish()
		{
			m_waiting--;

			if(m_waiting == 0 && !m_task.Cancelled)
				m_task.StopWaiting();
		}

	}
}











