using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebSocket
{
    delegate void PromisifiedCallback(TaskCompletionSource<bool> tsc);
    internal static class Utils
    {
        internal static Task Promisify(PromisifiedCallback callback)
        {
            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
            callback.Invoke(tsc);
            return tsc.Task;
        }

        internal static Task RunInContext(this SynchronizationContext context, Action callback)
        {
            return Promisify((tsc) =>
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