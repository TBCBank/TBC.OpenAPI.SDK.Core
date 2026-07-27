using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TBC.OpenAPI.SDK.Core.Authentication;
using TBC.OpenAPI.SDK.Core.Extensions;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class OAuthTokenCachingBuilderTests
    {
        private const string Scope = "some-scope";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public void AddOAuthTokenCaching_WhenNoCacheSelected_ShouldThrowWithActionableMessage()
        {
            // Arrange
            var services = CreateServices();
            services.AddOAuthTokenCaching<ITestClient>();
            using var provider = services.BuildServiceProvider();

            // Act
            Action act = () => provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Assert
            using (new AssertionScope())
            {
                var assertion = act.Should().Throw<InvalidOperationException>();
                assertion.Which.Message.Should().Contain("No OAuth token cache has been selected");
                assertion.Which.Message.Should().Contain("UseInMemoryCache()");
                assertion.Which.Message.Should().Contain("UseRegisteredDistributedCache()");
                assertion.Which.Message.Should().Contain("UseDistributedCache(");
                assertion.Which.Message.Should().Contain(typeof(ITestClient).FullName);
            }
        }

        [Fact]
        public void UseInMemoryCache_ShouldNotRegisterDistributedCacheInContainer()
        {
            // Arrange
            var services = CreateServices();
            services.AddOAuthTokenCaching<ITestClient>().UseInMemoryCache();
            using var provider = services.BuildServiceProvider();

            // Act
            var registeredCache = provider.GetService<IDistributedCache>();

            // Assert
            registeredCache.Should().BeNull();
        }

        [Fact]
        public async Task UseInMemoryCache_ShouldResolveWorkingTokenCacheHelper()
        {
            // Arrange
            var services = CreateServices(out var tokenHelper);
            tokenHelper
                .Setup(x => x.RequestToken(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = "token", ExpiresIn = 3600 });
            services.AddOAuthTokenCaching<ITestClient>().UseInMemoryCache();
            using var provider = services.BuildServiceProvider();
            var sut = provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Act
            var first = await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);
            var second = await sut.GetTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                first.AccessToken.Should().Be("token");
                second.AccessToken.Should().Be("token");
                tokenHelper.Verify(
                    x => x.RequestToken(Scope, It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public void UseRegisteredDistributedCache_WhenNoCacheRegistered_ShouldThrow()
        {
            // Arrange
            var services = CreateServices();
            services.AddOAuthTokenCaching<ITestClient>().UseRegisteredDistributedCache();
            using var provider = services.BuildServiceProvider();

            // Act
            Action act = () => provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .Which.Message.Should().Contain("no IDistributedCache is registered in the container");
        }

        [Fact]
        public void UseRegisteredDistributedCache_WhenMemoryDistributedCacheRegistered_ShouldThrow()
        {
            // Arrange
            var services = CreateServices();
            services.AddDistributedMemoryCache();
            services.AddOAuthTokenCaching<ITestClient>().UseRegisteredDistributedCache();
            using var provider = services.BuildServiceProvider();

            // Act
            Action act = () => provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .Which.Message.Should().Contain("not shared across processes or instances");
        }

        [Fact]
        public async Task UseRegisteredDistributedCache_WhenDistributedCacheRegistered_ShouldUseIt()
        {
            // Arrange
            var cache = new RecordingDistributedCache();
            var services = CreateServices();
            services.AddSingleton<IDistributedCache>(cache);
            services.AddOAuthTokenCaching<ITestClient>().UseRegisteredDistributedCache();
            using var provider = services.BuildServiceProvider();
            var sut = provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Act
            await sut.RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            cache.RemovedKeys.Should().ContainSingle().Which.Should().Be(GetCacheKey<ITestClient>(Scope));
        }

        [Fact]
        public async Task UseDistributedCache_WithInstance_ShouldUseProvidedInstance()
        {
            // Arrange
            var cache = new RecordingDistributedCache();
            var services = CreateServices();
            services.AddOAuthTokenCaching<ITestClient>().UseDistributedCache(cache);
            using var provider = services.BuildServiceProvider();
            var sut = provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Act
            await sut.RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                cache.RemovedKeys.Should().ContainSingle().Which.Should().Be(GetCacheKey<ITestClient>(Scope));
                provider.GetService<IDistributedCache>().Should().BeNull();
            }
        }

        [Fact]
        public async Task UseDistributedCache_WithFactory_ShouldUseFactoryResult()
        {
            // Arrange
            var cache = new RecordingDistributedCache();
            var services = CreateServices();
            services.AddSingleton(cache);
            services.AddOAuthTokenCaching<ITestClient>()
                .UseDistributedCache(sp => sp.GetRequiredService<RecordingDistributedCache>());
            using var provider = services.BuildServiceProvider();
            var sut = provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Act
            await sut.RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            cache.RemovedKeys.Should().ContainSingle().Which.Should().Be(GetCacheKey<ITestClient>(Scope));
        }

        [Fact]
        public void UseDistributedCache_WhenCacheIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var builder = CreateServices().AddOAuthTokenCaching<ITestClient>();

            // Act
            Action act = () => builder.UseDistributedCache((IDistributedCache)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void UseDistributedCache_WhenFactoryIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var builder = CreateServices().AddOAuthTokenCaching<ITestClient>();

            // Act
            Action act = () => builder.UseDistributedCache((Func<IServiceProvider, IDistributedCache>)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void UseDistributedCache_WhenFactoryReturnsNull_ShouldThrow()
        {
            // Arrange
            var services = CreateServices();
            services.AddOAuthTokenCaching<ITestClient>().UseDistributedCache(_ => null!);
            using var provider = services.BuildServiceProvider();

            // Act
            Action act = () => provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .Which.Message.Should().Contain("returned null");
        }

        [Fact]
        public async Task AddOAuthTokenCaching_WhenTwoClientsSelectDifferentCaches_ShouldKeepThemIsolated()
        {
            // Arrange
            var sharedCache = new RecordingDistributedCache();
            var services = CreateServices();
            services.AddSingleton(Mock.Of<IOAuthTokenHelper<IOtherTestClient>>());
            services.AddOAuthTokenCaching<ITestClient>().UseDistributedCache(sharedCache);
            services.AddOAuthTokenCaching<IOtherTestClient>().UseInMemoryCache();
            using var provider = services.BuildServiceProvider();

            // Act
            await provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>()
                .RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);
            await provider.GetRequiredService<IOAuthTokenCacheHelper<IOtherTestClient>>()
                .RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            sharedCache.RemovedKeys.Should().ContainSingle().Which.Should().Be(GetCacheKey<ITestClient>(Scope));
        }

        [Fact]
        public async Task AddOAuthTokenCaching_WhenSelectedTwice_ShouldUseLastSelection()
        {
            // Arrange
            var first = new RecordingDistributedCache();
            var second = new RecordingDistributedCache();
            var services = CreateServices();
            services.AddOAuthTokenCaching<ITestClient>().UseDistributedCache(first);
            services.AddOAuthTokenCaching<ITestClient>().UseDistributedCache(second);
            using var provider = services.BuildServiceProvider();
            var sut = provider.GetRequiredService<IOAuthTokenCacheHelper<ITestClient>>();

            // Act
            await sut.RemoveTokenAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                first.RemovedKeys.Should().BeEmpty();
                second.RemovedKeys.Should().ContainSingle();
            }
        }

        [Fact]
        public void OpenApiClientFactoryBuilder_AddOAuthTokenCaching_ShouldReturnFactoryBuilderFromTerminal()
        {
            // Arrange
            var factoryBuilder = new OpenApiClientFactoryBuilder();

            // Act
            var result = factoryBuilder.AddOAuthTokenCaching<ITestClient>().UseInMemoryCache();

            // Assert
            result.Should().BeSameAs(factoryBuilder);
        }

        private static IServiceCollection CreateServices()
            => CreateServices(out _);

        private static IServiceCollection CreateServices(out Mock<IOAuthTokenHelper<ITestClient>> tokenHelper)
        {
            tokenHelper = new Mock<IOAuthTokenHelper<ITestClient>>();

            var services = new ServiceCollection();
            services.AddSingleton(tokenHelper.Object);

            return services;
        }

        private static string GetCacheKey<TClient>(string scope)
            => $"{OAuthConstants.CacheKeyPrefix}:{typeof(TClient).FullName}:{scope}";

        private sealed class RecordingDistributedCache : IDistributedCache
        {
            private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);
            private readonly ConcurrentQueue<string> _removedKeys = new();

            public IEnumerable<string> RemovedKeys => _removedKeys;

            public byte[]? Get(string key)
                => _store.TryGetValue(key, out var value) ? value : null;

            public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
                => Task.FromResult(Get(key));

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
                => _store[key] = value;

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
            {
                _store.TryRemove(key, out _);
                _removedKeys.Enqueue(key);
            }

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }
    }

    public interface IOtherTestClient : IOpenApiClient
    {
    }
}
