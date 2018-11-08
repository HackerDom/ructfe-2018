using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class BlockingQueue<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        
        private volatile TaskCompletionSource<bool> signal = CreateSignal();

        public void Add(T value)
        {
            queue.Enqueue(value);
            signal.TrySetResult(true);
        }

        public async Task<T> TakeAsync()
        {
            while (true)
            {
                if (queue.TryDequeue(out var value))
                    return value;

                if (!await signal.Task)
                    throw new ObjectDisposedException($"Cannot {nameof(TakeAsync)}() from a disposed {nameof(BlockingQueue<T>)}.");
                
                signal = CreateSignal();
            }
        }

        private static TaskCompletionSource<bool> CreateSignal() => 
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Dispose() => signal.TrySetResult(false);
    }
}