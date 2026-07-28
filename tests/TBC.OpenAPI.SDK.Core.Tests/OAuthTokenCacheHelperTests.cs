using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TBC.OpenAPI.SDK.Core.Authentication;
using TBC.OpenAPI.SDK.Core.Exceptions;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class OAuthTokenCacheHelperTests
    {
        private const string Scope = "some-scope";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private readonly Mock<IOAuthTokenHelper<ITestClient>> _tokenHelper = new(MockBehavior.Strict);
        private readonly FakeDistributedCache _cache = new();

        private OAuthTokenCacheHelper<ITestClient> CreateSut()
            => new(_tokenHelper.Object, _cache);

        [Fact]
        public async Task GetTokenAsync_WhenTokenCached_ReturnsCachedTokenWithoutRequesting()
        {
            var sut = CreateSut();
            _cache.Seed(GetCacheKey(Scope), "cached-token");

            var result = await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            result.AccessToken.Should().Be("cached-token");
            _tokenHelper.Verify(
                x => x.RequestToken(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetTokenAsync_WhenNotCached_RequestsCachesAndReturnsToken()
        {
            var sut = CreateSut();
            SetupToken(accessToken: "fresh-token", expiresIn: 3600);

            var result = await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            result.AccessToken.Should().Be("fresh-token");
            _cache.GetString(GetCacheKey(Scope)).Should().Be("fresh-token");
            _tokenHelper.Verify(
                x => x.RequestToken(Scope, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync_WhenConcurrentSameScope_RequestsTokenOnce()
        {
            var sut = CreateSut();

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callCount = 0;
            _tokenHelper
                .Setup(x => x.RequestToken(Scope, It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>(async (_, _) =>
                {
                    Interlocked.Increment(ref callCount);
                    await gate.Task;
                    return new OAuthTokenResponse { AccessToken = "coalesced-token", ExpiresIn = 3600 };
                });

            var callers = Enumerable
                .Range(0, 50)
                .Select(_ => Task.Run(() => sut.GetTokenAsync(Scope, CancellationToken.None)))
                .ToArray();

            // Give every caller time to subscribe to the single in-flight fetch, then release it.
            await Task.Delay(100);
            gate.SetResult(true);

            var results = await Task.WhenAll(callers).WaitAsync(Timeout);

            results.Should().OnlyContain(r => r.AccessToken == "coalesced-token");
            callCount.Should().Be(1);
            _tokenHelper.Verify(
                x => x.RequestToken(Scope, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync_WhenDifferentScopes_FetchesConcurrentlyPerScope()
        {
            var sut = CreateSut();

            const string scopeA = "scope-a";
            const string scopeB = "scope-b";

            var started = 0;
            var bothStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _tokenHelper
                .Setup(x => x.RequestToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>(async (scope, _) =>
                {
                    // A single global lock would prevent the second scope from ever starting,
                    // so bothStarted would never complete and the test would time out.
                    if (Interlocked.Increment(ref started) == 2)
                    {
                        bothStarted.SetResult(true);
                    }

                    await bothStarted.Task;
                    return new OAuthTokenResponse { AccessToken = $"token:{scope}", ExpiresIn = 3600 };
                });

            var taskA = Task.Run(() => sut.GetTokenAsync(scopeA, CancellationToken.None));
            var taskB = Task.Run(() => sut.GetTokenAsync(scopeB, CancellationToken.None));

            var results = await Task.WhenAll(taskA, taskB).WaitAsync(Timeout);

            results[0].AccessToken.Should().Be($"token:{scopeA}");
            results[1].AccessToken.Should().Be($"token:{scopeB}");
            _tokenHelper.Verify(
                x => x.RequestToken(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetTokenAsync_WhenOneCallerCancels_DoesNotAbortSharedFetch()
        {
            var sut = CreateSut();

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _tokenHelper
                .Setup(x => x.RequestToken(Scope, It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>(async (_, _) =>
                {
                    await gate.Task;
                    return new OAuthTokenResponse { AccessToken = "shared-token", ExpiresIn = 3600 };
                });

            using var ctsForCanceller = new CancellationTokenSource();
            var cancellingCaller = sut.GetTokenAsync(Scope, ctsForCanceller.Token);
            var survivingCaller = sut.GetTokenAsync(Scope, CancellationToken.None);

            // Let both callers subscribe to the same in-flight fetch before cancelling one of them.
            await Task.Delay(100);
            ctsForCanceller.Cancel();

            await FluentActions
                .Awaiting(() => cancellingCaller.WaitAsync(Timeout))
                .Should().ThrowAsync<OperationCanceledException>();

            gate.SetResult(true);

            var result = await survivingCaller.WaitAsync(Timeout);
            result.AccessToken.Should().Be("shared-token");
            _tokenHelper.Verify(
                x => x.RequestToken(Scope, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(3600, 3600 - OAuthConstants.TokenTimeoutGracePeriodSec)]
        [InlineData(10, OAuthConstants.MinCacheTtlSec)]
        [InlineData(null, OAuthConstants.MinCacheTtlSec)]
        public async Task GetTokenAsync_CachesTokenWithExpectedTtl(int? expiresIn, int expectedTtlSec)
        {
            var sut = CreateSut();
            SetupToken(accessToken: "ttl-token", expiresIn: expiresIn);

            await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            _cache.LastSetOptions.Should().NotBeNull();
            _cache.LastSetOptions!.AbsoluteExpirationRelativeToNow
                .Should().Be(TimeSpan.FromSeconds(expectedTtlSec));
        }

        [Fact]
        public async Task RemoveTokenAsync_RemovesCachedTokenSoNextCallRefetches()
        {
            var sut = CreateSut();
            SetupToken(accessToken: "first-token", expiresIn: 3600);

            await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);
            await sut.RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            _cache.GetString(GetCacheKey(Scope)).Should().BeNull();

            await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            _tokenHelper.Verify(
                x => x.RequestToken(Scope, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetTokenAsync_WhenTokenHelperReturnsNull_ThrowsAndDoesNotCache()
        {
            var sut = CreateSut();
            _tokenHelper
                .Setup(x => x.RequestToken(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync((OAuthTokenResponse)null!);

            await FluentActions
                .Awaiting(() => sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>()
                .WithMessage("Failed to obtain a new OAuth token.");

            _cache.GetString(GetCacheKey(Scope)).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetTokenAsync_WhenAccessTokenEmpty_ThrowsAndDoesNotCache(string? accessToken)
        {
            var sut = CreateSut();
            _tokenHelper
                .Setup(x => x.RequestToken(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = accessToken, ExpiresIn = 3600 });

            await FluentActions
                .Awaiting(() => sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>()
                .WithMessage("Received empty access token.");

            _cache.GetString(GetCacheKey(Scope)).Should().BeNull();
        }

        private void SetupToken(string accessToken, int? expiresIn)
        {
            _tokenHelper
                .Setup(x => x.RequestToken(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = accessToken, ExpiresIn = expiresIn });
        }

        private static string GetCacheKey(string scope)
            => $"{OAuthConstants.CacheKeyPrefix}:{typeof(ITestClient).FullName}:{scope}";

        private sealed class FakeDistributedCache : IDistributedCache
        {
            private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

            public DistributedCacheEntryOptions? LastSetOptions { get; private set; }

            public void Seed(string key, string value)
                => _store[key] = Encoding.UTF8.GetBytes(value);

            public string? GetString(string key)
                => _store.TryGetValue(key, out var value) ? Encoding.UTF8.GetString(value) : null;

            public byte[]? Get(string key)
                => _store.TryGetValue(key, out var value) ? value : null;

            public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
                => Task.FromResult(Get(key));

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                LastSetOptions = options;
                _store[key] = value;
            }

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }

            public void Refresh(string key)
            {
            }

            public Task RefreshAsync(string key, CancellationToken token = default)
                => Task.CompletedTask;

            public void Remove(string key)
                => _store.TryRemove(key, out _);

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }
    }
}
