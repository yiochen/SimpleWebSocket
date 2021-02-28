using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SimpleWebSocket
{
    internal readonly struct SwitchToBackgroundThread
    {
        internal ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
        {
            // Do not use the current SynchronizationContext, which cause it to
            // run in a pooled thread.
            return Task.Run(() => { }).ConfigureAwait(false).GetAwaiter();
        }
    }
}