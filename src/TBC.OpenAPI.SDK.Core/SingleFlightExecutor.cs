using System.Collections.Concurrent;

namespace TBC.OpenAPI.SDK.Core
{
    /// <summary>
    /// Coalesces concurrent asynchronous operations that share the same key so that the underlying
    /// work runs only once at a time (single-flight / stampede protection). Callers that arrive
    /// while an operation for a given key is in flight await the same task rather than starting a
    /// duplicate one.
    /// </summary>
    /// <typeparam name="TResult">The type produced by the coalesced operation.</typeparam>
    internal sealed class SingleFlightExecutor<TResult>
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<TResult>>> _pending
            = new ConcurrentDictionary<string, Lazy<Task<TResult>>>(StringComparer.Ordinal);

        /// <summary>
        /// Runs <paramref name="factory"/> for the specified <paramref name="key"/>, ensuring that
        /// concurrent callers with the same key share a single in-flight execution. The pending
        /// entry is removed once the operation completes (whether it succeeds or fails).
        /// </summary>
        /// <param name="key">The key that identifies the operation to coalesce.</param>
        /// <param name="factory">The work to execute when no operation for the key is in flight.</param>
        /// <param name="cancellationToken">A token that lets the caller stop awaiting the shared task.</param>
        public Task<TResult> ExecuteAsync(string key, Func<Task<TResult>> factory, CancellationToken cancellationToken)
        {
            var pending = _pending.GetOrAdd(
                key,
                k => new Lazy<Task<TResult>>(
                    () => RunAsync(k, factory),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return WaitAsyncSafe(pending.Value, cancellationToken);
        }

        private async Task<TResult> RunAsync(string key, Func<Task<TResult>> factory)
        {
            try
            {
                return await factory().ConfigureAwait(false);
            }
            finally
            {
                _pending.TryRemove(key, out _);
            }
        }

        private static Task<TResult> WaitAsyncSafe(Task<TResult> task, CancellationToken cancellationToken)
        {
#if NET6_0_OR_GREATER
            return task.WaitAsync(cancellationToken);
#else
            return WaitAsyncPolyfill(task, cancellationToken);
#endif
        }

#if !NET6_0_OR_GREATER
        private static async Task<TResult> WaitAsyncPolyfill(Task<TResult> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return await task.ConfigureAwait(false);
            }

            var cancellationSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancellationSignal))
            {
                var completed = await Task.WhenAny(task, cancellationSignal.Task).ConfigureAwait(false);
                if (completed != task)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
#endif
    }
}
