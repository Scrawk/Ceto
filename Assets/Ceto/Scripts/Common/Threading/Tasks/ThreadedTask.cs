using System;
using System.Collections;
using System.Collections.Generic;
using Ceto.Common.Threading.Scheduling;

namespace Ceto.Common.Threading.Tasks
{
    /// <summary>
    /// A abstract task that implements the default behaviour of a task
    /// </summary>
	public abstract class ThreadedTask : IThreadedTask, ICancelToken
	{

		/// <summary>
		/// How long the task took to run in milliseconds.
		/// </summary>
		public float RunTime { get; set; }

        /// <summary>
        /// True if this task must be run on the main thread.
        /// </summary>
		public bool IsThreaded { get { return m_isThreaded; } }
        readonly bool m_isThreaded;

		/// <summary>
		/// True if the task has ran.
		/// Should be set to true in the tasks end function.
		/// </summary>
		public bool Done { get { return m_done; } }
		volatile bool m_done;

        /// <summary>
        /// True if the task is finished.
        /// Should be set to true in the tasks run function.
        /// </summary>
        public bool Ran { get { return m_ran; } }
        volatile bool m_ran;
 
		/// <summary>
		/// Set to true to skip the end function. 
		/// This will immediately trigger any tasks
		/// waiting on this one to stop waiting.
		/// </summary>
		public bool NoFinish 
		{ 
			get { return  m_noFinish; }  
			set { m_noFinish = value; } 
		}
		volatile bool m_noFinish;

        /// <summary>
        /// Is the task waiting on another task to finish.
        /// </summary>
        public bool Waiting { get { return m_listener.Waiting > 0; } }

		/// <summary>
		/// True if the tasks runs immediately after stop wait 
		/// or gets queued as a scheduled task.
		/// </summary>
		public bool RunOnStopWaiting 
		{ 
			get { return  m_runOnStopWaiting; }  
			set { m_runOnStopWaiting = value; } 
		}
		volatile bool m_runOnStopWaiting;

        /// <summary>
        /// True if the task has started.
        /// Should be set to true in the tasks start function
        /// </summary>
        public bool Started { get { return m_started; } }
		volatile bool m_started;

        /// <summary>
        /// True if the task has been cancelled.
        /// </summary>
        public bool Cancelled { get { return m_cancelled; } }
		volatile bool m_cancelled;

		/// <summary>
		/// The scheduler used to run this task
		/// </summary>
        public IScheduler Scheduler { set { m_scheduler = value; } }
        protected IScheduler m_scheduler;

        /// <summary>
        /// A list of task listeners that are waiting 
        /// on this task to finish running.
        /// </summary>
        protected LinkedList<TaskListener> Listeners { get { return m_listeners; } }
		LinkedList<TaskListener> m_listeners;

        /// <summary>
        /// The listener for this task that can listen on another 
        /// task to stop running.
        /// </summary>
        protected TaskListener Listener { get { return m_listener; } }
		TaskListener m_listener;

		/// <summary>
		/// Lock for functions that maybe accessed by task running on another thread.
		/// </summary>
		readonly object m_lock = new object();

        /// <summary>
        /// Create a task.
        /// </summary>
        /// <param name="mainThread"> Can the task only be run on the main thread</param>
        /// <param name="key">The key to identify the task. Can be null</param>
		protected ThreadedTask(bool isThreaded)
		{
            m_scheduler = null;
			m_isThreaded = isThreaded;
			m_listeners = new LinkedList<TaskListener>();
			m_listener = new TaskListener(this);
		}

        /// <summary>
        /// Starts the task. Used to initialize anything
        /// that maybe needed before the task is run. 
        /// Is always called from the main thread.
        /// </summary>
		public virtual void Start()
		{
			m_started = true;
		}

		/// <summary>
		/// Reset task to its starting conditions.
		/// </summary>
		public virtual void Reset()
		{
			lock(m_lock)
			{
				m_listeners.Clear();
				m_listener.Waiting = 0;
				m_ran = false;
				m_done = false;
				m_cancelled = false;
				m_started = false;
				RunTime = 0.0f;
			}

		}

        /// <summary>
        /// Runs the task. If mainThread is true this will
        /// only be called from the main thread. If it is false the
        /// task will be run on any available thread. 
        /// </summary>
		public abstract IEnumerator Run();

        /// <summary>
        /// Must be called at the end of the run function
        /// to notify the scheduler that the task has finished.
        /// </summary>
        protected virtual void FinishedRunning()
        {

			m_ran = true;

			if(m_noFinish)
				m_done = true;

			lock(m_lock)
			{
				if(m_noFinish && !m_cancelled)
				{
                    //Inform tasks waiting on this task to finish that it has.
                    var e = m_listeners.GetEnumerator();
					while(e.MoveNext()) e.Current.OnFinish();
					
					m_listeners.Clear();
				}
			}

        }

        /// <summary>
        /// Ends the task. Used to do any clean up when the task is 
        /// finished. Is always called from the main thread.
        /// </summary>
		public virtual void End()
		{

			m_done = true;

			lock(m_lock)
			{
                if (!m_cancelled)
                {

                    //Inform tasks waiting on this task to finish that it has.
                    var e = m_listeners.GetEnumerator();
                    while (e.MoveNext()) e.Current.OnFinish();
                }

                m_listeners.Clear();
            }
		}

        /// <summary>
        /// This function gets called on task if
        /// scheduler cancels tasks.
        /// </summary>
        public virtual void Cancel()
        {
			lock(m_lock)
			{
                m_cancelled = true;
                m_listeners.Clear();
            }
        }

		/// <summary>
		/// Wait on task to finish before running.
		/// This task will be added to the scheduler waiting queue
		/// and will be added to the schedule queue when all tasks
		/// it is waiting on have finished.
		/// </summary>
		public virtual void WaitOn(ThreadedTask task)
		{
			lock(m_lock)
			{
                if (task.Cancelled)
                    throw new InvalidOperationException("Can not wait on a task that is cancelled");

                if (task.Done)
					throw new InvalidOperationException("Can not wait on a task that is already done");

				if(task.IsThreaded && task.NoFinish && !m_isThreaded)
					throw new InvalidOperationException("A non-threaded task cant wait on a threaded task with no finish");

				m_listener.Waiting++;
				task.Listeners.AddLast(m_listener);
			}
		}

		/// <summary>
		/// The tasks that this task was waiting on to finish have 
		/// now finished and it will now be run by the scheduler.
		/// </summary>
		public virtual void StopWaiting()
		{
			lock(m_lock)
			{
            	if (m_scheduler == null || m_cancelled) return;

				m_scheduler.StopWaiting(this, m_runOnStopWaiting);
			}
		}
		
        /// <summary>
        /// The task as a string.
        /// </summary>
		public override string ToString ()
		{
			return string.Format("[Task: isThreaded={0}, started={1}, ran={2}, done={3}, cancelled={4}]", m_isThreaded, m_started, m_ran, m_done, m_cancelled);
		}


	}
}












