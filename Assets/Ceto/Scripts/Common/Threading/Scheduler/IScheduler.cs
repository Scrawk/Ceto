
using Ceto.Common.Threading.Tasks;

namespace Ceto.Common.Threading.Scheduling
{
    /// <summary>
    /// scheduler interface
    /// </summary>
    public interface IScheduler
    {

		/// <summary>
		/// Returns > 0 if the scheduler has tasks scheduled.
		/// </summary>
		int ScheduledTasks();

		/// <summary>
		/// Returns > 0 if the scheduler has tasks running.
		/// </summary>
		int RunningTasks();

		/// <summary>
		/// Returns > 0 if the scheduler has tasks waiting.
		/// </summary>
		int WaitingTasks();

		/// <summary>
		/// Returns > 0 if the scheduler has tasks finishing.
		/// </summary>
		int FinishingTasks();

		/// <summary>
		/// Returns > 0 if the scheduler has tasks.
		/// </summary>
		int Tasks();

        /// <summary>
        /// Returns true if the scheduler has
        /// tasks to run or if there are tasks running.
        /// </summary>
        bool HasTasks();

        /// <summary>
        /// Cancel a task. Task will have its cancel function called
        /// if it is cancelled. Tasks that are already running or 
        /// finishing can not be cancelled.
        /// </summary>
		void Cancel(IThreadedTask task);

        /// <summary>
        /// Does the scheduler contain the task in any of
        /// its queues.
        /// </summary>
		bool Contains(IThreadedTask task);

		/// <summary>
		/// Returns true if the tasks has been added to 
		/// the scheduler and has not been run.
		/// </summary>
		bool IsScheduled(IThreadedTask task);
		
		/// <summary>
		/// Returns true if the tasks has been added to 
		/// the scheduler and is running.
		/// </summary>
		bool IsRunning(IThreadedTask task);

        /// <summary>
        /// Returns true if the tasks has been added to 
        /// the scheduler and is waiting.
        /// </summary>
		bool IsWaiting(IThreadedTask task);

        /// <summary>
        /// Returns true if the tasks has been added to 
        /// the scheduler and is finishing.
        /// </summary>
		bool IsFinishing(IThreadedTask task);

        /// <summary>
        /// Add a task to scheduler. The task will be queued and
        /// will be run when it reaches the front of queue.
        /// </summary>
		void Add(IThreadedTask task);

		/// <summary>
		/// Add a task to scheduler and run immediately.
		/// </summary>
		void Run(IThreadedTask task);

        /// <summary>
        /// Adds a task to the waiting queue. A task will stop
        /// waiting on some predefined event like another task
        /// finishing.
        /// </summary>
		void AddWaiting(IThreadedTask task);

        /// <summary>
        /// Removes a task from the waiting queue and
        /// adds it to the scheduled queue were it will be run.
        /// </summary>
        void StopWaiting(IThreadedTask task, bool run);

    }

}













