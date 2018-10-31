
using System.Collections;
using System.Collections.Generic;
using Ceto.Common.Threading.Scheduling;

namespace Ceto.Common.Threading.Tasks
{
    /// <summary>
    /// Interface for a task. 
    /// </summary>
	public interface IThreadedTask
    {

		/// <summary>
		/// How long the task took to run in milliseconds.
		/// </summary>
		float RunTime { get; set; }

        /// <summary>
        /// True if threaded.
        /// </summary>
        bool IsThreaded { get; }

		/// <summary>
		/// True if the task has ran.
		/// Should be set to true in the tasks run function.
		/// </summary>
		bool Ran { get; }

        /// <summary>
        /// True if the task is finished.
        /// Should be set to true in the tasks end function.
        /// </summary>
		bool Done { get; }

		/// <summary>
		/// Set to true to skip the end function. 
		/// This will immediately trigger any tasks
		/// waiting on this one to stop waiting.
		/// </summary>
		bool NoFinish { get; set; }

        /// <summary>
        /// Is the task waiting on another task to finish.
        /// </summary>
        bool Waiting { get;}

		/// <summary>
		/// True if the tasks runs immediately after stop wait 
		/// or gets queued as a scheduled task.
		/// </summary>
		bool RunOnStopWaiting { get; set; }

        /// <summary>
        /// True if the task has started.
        /// Should be set to true in the tasks start function
        /// </summary>
        bool Started { get; }

		/// <summary>
		/// True if this task was scheduled but cancelled.
		/// </summary>
		bool Cancelled { get; }

		/// <summary>
		/// Reset task to its starting conditions.
		/// </summary>
		void Reset();

        /// <summary>
        /// Starts the task. Used to initialize anything
        /// that maybe needed before the task is run. 
        /// Is always called from the main thread.
        /// </summary>
		void Start();

        /// <summary>
        /// Runs the task. If mainThread is true this will
        /// only be called from the main thread. If it is false the
        /// task will be run on any available thread. 
        /// </summary>
		IEnumerator Run();

        /// <summary>
        /// Ends the task. Used to do any clean up when the task is 
        /// finished. Is always called from the main thread.
        /// </summary>
        void End();

        /// <summary>
        /// This function gets called on task if
        /// scheduler cancels tasks.
        /// </summary>
        void Cancel();

		/// <summary>
		/// Wait on task to finish before running.
		/// This task will be added to the scheduler waiting queue
		/// and will be added to the schedule queue was all tasks
		/// it is waiting on have finished.
		/// </summary>
		void WaitOn(ThreadedTask task);

		/// <summary>
		/// The scheduler that will run the task.
		/// Is set by the scheduler before running task.
		/// Is null until then.
		/// </summary>
		IScheduler Scheduler { set; }

    }
}










