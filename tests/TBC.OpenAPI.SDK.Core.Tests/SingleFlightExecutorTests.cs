using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class SingleFlightExecutorTests
    {
        private const string Key = "some-key";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task ExecuteAsync_WhenCalledOnce_RunsFactoryAndReturnsResult()
        {
            var sut = new SingleFlightExecutor<int>();
            var callCount = 0;

            var result = await sut
                .ExecuteAsync(Key, () =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult(42);
                }, CancellationToken.None)
                .WaitAsync(Timeout);

            result.Should().Be(42);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_WhenConcurrentSameKey_RunsFactoryOnce()
        {
            var sut = new SingleFlightExecutor<int>();

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callCount = 0;

            Func<Task<int>> factory = async () =>
            {
                Interlocked.Increment(ref callCount);
                await gate.Task;
                return 7;
            };

            var callers = Enumerable
                .Range(0, 50)
                .Select(_ => Task.Run(() => sut.ExecuteAsync(Key, factory, CancellationToken.None)))
                .ToArray();

            // Give every caller time to subscribe to the single in-flight execution, then release it.
            await Task.Delay(100);
            gate.SetResult(true);

            var results = await Task.WhenAll(callers).WaitAsync(Timeout);

            results.Should().OnlyContain(r => r == 7);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_WhenDifferentKeys_RunsFactoryPerKey()
        {
            var sut = new SingleFlightExecutor<string>();

            const int keyCount = 10;
            var keys = Enumerable.Range(0, keyCount).Select(i => $"key-{i}").ToArray();

            var started = 0;
            var allStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<string, Task<string>> factory = async key =>
            {
                // A single global lock would prevent later keys from ever starting,
                // so allStarted would never complete and the test would time out.
                if (Interlocked.Increment(ref started) == keyCount)
                {
                    allStarted.SetResult(true);
                }

                await allStarted.Task;
                return $"value:{key}";
            };

            var tasks = keys
                .Select(key => Task.Run(() => sut.ExecuteAsync(key, () => factory(key), CancellationToken.None)))
                .ToArray();

            var results = await Task.WhenAll(tasks).WaitAsync(Timeout);

            results.Should().BeEquivalentTo(keys.Select(key => $"value:{key}"));
            started.Should().Be(keyCount);
        }

        [Fact]
        public async Task ExecuteAsync_WhenOperationCompletes_RemovesPendingEntrySoNextCallReruns()
        {
            var sut = new SingleFlightExecutor<int>();
            var callCount = 0;

            Func<Task<int>> factory = () =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult(1);
            };

            await sut.ExecuteAsync(Key, factory, CancellationToken.None).WaitAsync(Timeout);
            await sut.ExecuteAsync(Key, factory, CancellationToken.None).WaitAsync(Timeout);

            callCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_WhenFactoryThrows_PropagatesExceptionToAllCallers()
        {
            var sut = new SingleFlightExecutor<int>();

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var callCount = 0;
            Func<Task<int>> factory = async () =>
            {
                Interlocked.Increment(ref callCount);
                await gate.Task;
                throw new InvalidOperationException("boom");
            };

            var callers = Enumerable
                .Range(0, 50)
                .Select(_ => Task.Run(() => sut.ExecuteAsync(Key, factory, CancellationToken.None)))
                .ToArray();

            // Give every caller time to subscribe to the single in-flight execution, then release it.
            await Task.Delay(100);
            gate.SetResult(true);

            foreach (var caller in callers)
            {
                await FluentActions
                    .Awaiting(() => caller.WaitAsync(Timeout))
                    .Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("boom");
            }

            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_WhenFactoryThrows_RemovesPendingEntrySoNextCallReruns()
        {
            var sut = new SingleFlightExecutor<int>();
            var callCount = 0;

            Func<Task<int>> throwingFactory = () =>
            {
                Interlocked.Increment(ref callCount);
                throw new InvalidOperationException("boom");
            };

            await FluentActions
                .Awaiting(() => sut.ExecuteAsync(Key, throwingFactory, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<InvalidOperationException>();

            var result = await sut
                .ExecuteAsync(Key, () => Task.FromResult(99), CancellationToken.None)
                .WaitAsync(Timeout);

            result.Should().Be(99);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_WhenOneCallerCancels_DoesNotAbortSharedExecution()
        {
            var sut = new SingleFlightExecutor<int>();

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callCount = 0;

            Func<Task<int>> factory = async () =>
            {
                Interlocked.Increment(ref callCount);
                await gate.Task;
                return 5;
            };

            using var ctsForCanceller = new CancellationTokenSource();
            var cancellingCaller = sut.ExecuteAsync(Key, factory, ctsForCanceller.Token);
            var survivingCallers = Enumerable
                .Range(0, 50)
                .Select(_ => sut.ExecuteAsync(Key, factory, CancellationToken.None))
                .ToArray();

            // Let all callers subscribe to the same in-flight execution before cancelling one of them.
            await Task.Delay(100);
            ctsForCanceller.Cancel();

            await FluentActions
                .Awaiting(() => cancellingCaller.WaitAsync(Timeout))
                .Should().ThrowAsync<OperationCanceledException>();

            gate.SetResult(true);

            var results = await Task.WhenAll(survivingCallers).WaitAsync(Timeout);
            results.Should().OnlyContain(r => r == 5);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_WhenTokenAlreadyCancelled_ThrowsOperationCanceled()
        {
            var sut = new SingleFlightExecutor<int>();

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Func<Task<int>> factory = async () =>
            {
                await gate.Task;
                return 1;
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await FluentActions
                .Awaiting(() => sut.ExecuteAsync(Key, factory, cts.Token).WaitAsync(Timeout))
                .Should().ThrowAsync<OperationCanceledException>();

            gate.SetResult(true);
        }
    }
}
