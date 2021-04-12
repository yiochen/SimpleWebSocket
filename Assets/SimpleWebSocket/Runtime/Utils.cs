using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebSocket
{
    delegate void Callback(TaskCompletionSource<bool> tsc);
    internal static class Utils
    {
        /// <summary>
        /// Convert a callback to Task that can be await on. The callback should
        /// set the TaskCompletionSource's result when done.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        internal static Task Taskify(Callback callback)
        {
            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
            callback.Invoke(tsc);
            return tsc.Task;
        }

        internal static Task RunInContext(this SynchronizationContext context, Action callback)
        {
            return Taskify((tsc) =>
            {
                context.Post(_ =>
                {
                    callback.Invoke();
                    tsc.TrySetResult(true);
                }, null);
            });
        }
    }
}