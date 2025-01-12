using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Common.Utilities
{



    /// <summary>
    /// A MonoBehaviour that manages foreground tasks using Unity's Job System.
    /// </summary>
    public class Foregrounder : MonoBehaviour
    {
        private SynchronizationContext threadContext;
        private readonly ConcurrentQueue<Func<Task>> taskQueue = new ConcurrentQueue<Func<Task>>();

        private void OnEnable()
        {
            threadContext = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(threadContext);
            ThreadUtils.SetForegrounder(this);
        }

        private void OnDisable()
        {
            ThreadUtils.ClearForegrounder();
            threadContext = null;
        }

        /// <summary>
        /// Executes a job using Unity's Job System.
        /// </summary>
        private void ExecuteJob(Action jobAction)
        {
            threadContext.Post(_ =>
            {
                #if USE_BURST
                var handle = GCHandle.Alloc(jobAction);
                var job = new JobWrapper
                {
                    TaskPtr = GCHandle.ToIntPtr(handle)
                };
                var jobHandle = job.Schedule();
                jobHandle.Complete();
                #else
                jobAction();
                #endif
            }, null);
        }

        private struct JobWrapper : IJob
        {
            public IntPtr TaskPtr;

            public void Execute()
            {
                var handle = GCHandle.FromIntPtr(TaskPtr);
                var task = (Action)handle.Target;
                try
                {
                    task?.Invoke();
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        /// <summary>
        /// Processes asynchronous jobs from the queue.
        /// </summary>
        private void ProcessQueue()
        {
            while (taskQueue.TryDequeue(out var task))
            {
                ExecuteJob(() => task.Invoke().Wait());
            }
        }

        /// <summary>
        /// Executes an asynchronous job and returns the result.
        /// </summary>
        public async Task<TResult> ExecuteJobAsync<TResult>(Func<TResult> jobFunc)
        {
            var tcs = new TaskCompletionSource<TResult>();
            taskQueue.Enqueue(async () =>
            {
                try
                {
                    var result = jobFunc();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            ProcessQueue();
            return await tcs.Task;
        }

        /// <summary>
        /// Executes an asynchronous job.
        /// </summary>
        public async Task ExecuteJobAsync(Func<Task> jobFunc)
        {
            var tcs = new TaskCompletionSource<bool>();
            taskQueue.Enqueue(async () =>
            {
                try
                {
                    await jobFunc();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            ProcessQueue();
            await tcs.Task;
        }
    }
}
