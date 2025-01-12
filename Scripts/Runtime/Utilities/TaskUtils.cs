using System;
using System.Threading;
using System.Threading.Tasks;

namespace DoubTech.ThirdParty.AI.Common.Utilities
{
    /// <summary>
    /// Provides utility methods for executing tasks asynchronously and returning their results to the caller on the foreground thread.
    /// </summary>
    public static class ThreadUtils
    {
        private static SynchronizationContext mainThreadContext;
        private static Foregrounder foregrounderInstance;

        /// <summary>
        /// Initializes the synchronization context to the current context. Should be called from the main thread.
        /// </summary>
        public static void Init()
        {
            if (null != mainThreadContext) return;
            
            mainThreadContext = SynchronizationContext.Current;
            if (mainThreadContext == null)
            {
                throw new InvalidOperationException(
                    "SynchronizationContext is null. Ensure this method is called from the main thread.");
            }
        }

        /// <summary>
        /// Sets the Foregrounder instance for job execution.
        /// </summary>
        /// <param name="foregrounder">The Foregrounder instance to use.</param>
        public static void SetForegrounder(Foregrounder foregrounder)
        {
            foregrounderInstance = foregrounder;
        }

        /// <summary>
        /// Clears the Foregrounder instance.
        /// </summary>
        public static void ClearForegrounder()
        {
            foregrounderInstance = null;
        }

        /// <summary>
        /// Executes an asynchronous task on a background thread and ensures the result is marshaled back to the foreground thread.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="asyncTask">The asynchronous task to execute.</param>
        /// <returns>A task representing the operation, with its result returned on the foreground thread.</returns>
        public static async Task<TResult> RunOnForegroundThread<TResult>(Func<TResult> asyncTask)
        {
            if (asyncTask == null) throw new ArgumentNullException(nameof(asyncTask));

            if (foregrounderInstance != null)
            {
                return await foregrounderInstance.ExecuteJobAsync(asyncTask);
            }

            // Use the stored synchronization context or capture the current one.
            var synchronizationContext = mainThreadContext ?? SynchronizationContext.Current;

            if (synchronizationContext == null)
            {
                // If there is no synchronization context, run the task directly.
                return asyncTask();
            }

            // Marshal the result back to the foreground thread.
            var tcs = new TaskCompletionSource<TResult>();

            synchronizationContext.Post(_ =>
            {
                try
                {
                    tcs.SetResult(asyncTask());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return await tcs.Task;
        }

        /// <summary>
        /// Executes an action asynchronously on the background thread and marshals the result back to the foreground thread.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to execute.</param>
        /// <returns>A task that completes when the action is finished on the foreground thread.</returns>
        public static async Task RunOnForegroundThread(Action asyncAction)
        {
            if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));

            if (foregrounderInstance != null)
            {
                await foregrounderInstance.ExecuteJobAsync(async () => asyncAction.Invoke());
                return;
            }

            // Use the stored synchronization context or capture the current one.
            var synchronizationContext = mainThreadContext ?? SynchronizationContext.Current;

            if (synchronizationContext == null)
            {
                // If there is no synchronization context, run the action directly.
                asyncAction();
                return;
            }
            
            // Marshal back to the foreground thread.
            var tcs = new TaskCompletionSource<bool>();

            synchronizationContext.Post(async _ =>
            {
                try
                {
                    asyncAction();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            await tcs.Task;
        }
    }
}