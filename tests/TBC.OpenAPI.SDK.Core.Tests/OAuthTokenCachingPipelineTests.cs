using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using TBC.OpenAPI.SDK.Core.Authentication;
using TBC.OpenAPI.SDK.Core.Extensions;
using TBC.OpenAPI.SDK.Core.Models;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    /// <summary>
    /// Covers the <c>configurePipeline</c> hook on <c>AddOAuthTokenCaching&lt;TClient&gt;</c>. Core
    /// ships no retry logic and takes no dependency on Polly or any resilience library; the hook only
    /// has to guarantee that a caller-supplied handler is placed <em>outside</em>
    /// <see cref="OAuthDelegatingHandler{TClient}"/>, which is the only position from which a retried
    /// attempt re-enters token handling and benefits from the token eviction a <c>401</c> triggers.
    /// </summary>
    public class OAuthTokenCachingPipelineTests : IDisposable
    {
        private const string Scope = "read:some-resource";
        private const string AccessToken = "the-access-token";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        private readonly WireMockServer _server = WireMockServer.Start();

        [Fact]
        public async Task AddOAuthTokenCaching_WhenConfigurePipelineAddsHandler_ShouldRunItOutsideOAuthHandler()
        {
            // Arrange - the server returns 401 once per scenario, then 200. A retry only recovers if
            // it re-enters the OAuth handler after the 401 evicts the token, which only happens when
            // the caller-supplied handler sits outside OAuthDelegatingHandler<TClient>.
            StubTokenEndpoint();
            StubResourceEndpoint_UnauthorizedThenSuccess();

            var retryHandler = new SingleRetryOnUnauthorizedHandler();

            var services = new ServiceCollection();
            services.AddOpenApiClient<IBindingTestClient, BindingTestClient, BindingTestClientOptions>(
                new BindingTestClientOptions
                {
                    BaseUrl = $"{_server.Urls[0]}/",
                    ApiKey = "the-api-key"
                });
            services
                .AddOAuthTokenCaching<BindingTestClient>(configurePipeline: pipeline =>
                    pipeline.AddHttpMessageHandler(() => retryHandler))
                .UseInMemoryCache();

            using var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IBindingTestClient>();

            // Act
            var response = await client.GetResourceAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                response.IsSuccess.Should().BeTrue();
                retryHandler.RetryCount.Should().Be(1);

                // Two attempts reached the resource endpoint: the 401 and the retry that followed
                // the token eviction.
                RequestsTo("/some-resource").Should().HaveCount(2);
            }
        }

        [Fact]
        public async Task AddOAuthTokenCaching_WhenConfigurePipelineIsNull_ShouldBehaveAsBeforeAndNotRetry()
        {
            // Arrange - default behaviour: a 401 is returned to the caller unchanged, with no retry.
            StubTokenEndpoint();
            StubResourceEndpoint_AlwaysUnauthorized();

            var services = new ServiceCollection();
            services.AddOpenApiClient<IBindingTestClient, BindingTestClient, BindingTestClientOptions>(
                new BindingTestClientOptions
                {
                    BaseUrl = $"{_server.Urls[0]}/",
                    ApiKey = "the-api-key"
                });
            services.AddOAuthTokenCaching<BindingTestClient>().UseInMemoryCache();

            using var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IBindingTestClient>();

            // Act
            var response = await client.GetResourceAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                response.IsSuccess.Should().BeFalse();
                RequestsTo("/some-resource").Should().ContainSingle();
            }
        }

        [Fact]
        public async Task OpenApiClientFactoryBuilder_AddOAuthTokenCaching_WhenConfigurePipelineAddsHandler_ShouldRunItOutsideOAuthHandler()
        {
            // Arrange
            StubTokenEndpoint();
            StubResourceEndpoint_UnauthorizedThenSuccess();

            var retryHandler = new SingleRetryOnUnauthorizedHandler();

            var factory = new OpenApiClientFactoryBuilder()
                .AddClient<IBindingTestClient, BindingTestClient, BindingTestClientOptions>(
                    new BindingTestClientOptions
                    {
                        BaseUrl = $"{_server.Urls[0]}/",
                        ApiKey = "the-api-key"
                    })
                .AddOAuthTokenCaching<BindingTestClient>(configurePipeline: pipeline =>
                    pipeline.AddHttpMessageHandler(() => retryHandler))
                    .UseInMemoryCache()
                .Build();

            var client = factory.GetOpenApiClient<IBindingTestClient>();

            // Act
            var response = await client.GetResourceAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                response.IsSuccess.Should().BeTrue();
                retryHandler.RetryCount.Should().Be(1);
                RequestsTo("/some-resource").Should().HaveCount(2);
            }
        }

        public void Dispose()
        {
            _server.Stop();
            _server.Dispose();
            GC.SuppressFinalize(this);
        }

        private void StubTokenEndpoint()
        {
            _server
                .Given(Request.Create().WithPath("/oauth/token").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody($"{{\"access_token\":\"{AccessToken}\",\"token_type\":\"Bearer\",\"expires_in\":3600}}"));
        }

        private void StubResourceEndpoint_UnauthorizedThenSuccess()
        {
            _server
                .Given(Request.Create().WithPath("/some-resource").UsingGet())
                .InScenario("resource-token-refresh")
                .WillSetStateTo("Refreshed")
                .RespondWith(Response.Create().WithStatusCode(401));

            _server
                .Given(Request.Create().WithPath("/some-resource").UsingGet())
                .InScenario("resource-token-refresh")
                .WhenStateIs("Refreshed")
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{\"id\":1}"));
        }

        private void StubResourceEndpoint_AlwaysUnauthorized()
        {
            _server
                .Given(Request.Create().WithPath("/some-resource").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(401));
        }

        private IEnumerable<IRequestMessage> RequestsTo(string path)
            => _server.LogEntries
                .Select(x => x.RequestMessage)
                .Where(x => string.Equals(x.Path, path, StringComparison.Ordinal));

        /// <summary>
        /// A minimal clone-capable retry handler standing in for a caller's own retry mechanism
        /// (Polly or otherwise). Core does not ship this - it exists only to prove the pipeline hook
        /// places handlers where a retry can actually recover.
        /// </summary>
        private sealed class SingleRetryOnUnauthorizedHandler : DelegatingHandler
        {
            private int _retryCount;

            public int RetryCount => _retryCount;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var buffered = request.Content is null
                    ? null
                    : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

                using (var firstAttempt = await CloneAsync(request, buffered).ConfigureAwait(false))
                {
                    var response = await base.SendAsync(firstAttempt, cancellationToken).ConfigureAwait(false);

                    if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                    {
                        return response;
                    }

                    response.Dispose();
                }

                Interlocked.Increment(ref _retryCount);

                var retry = await CloneAsync(request, buffered).ConfigureAwait(false);
                return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
            }

            private static Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, byte[]? bufferedContent)
            {
                var clone = new HttpRequestMessage(request.Method, request.RequestUri)
                {
                    Version = request.Version
                };

                if (bufferedContent is not null)
                {
                    clone.Content = new ByteArrayContent(bufferedContent);
                    foreach (var header in request.Content!.Headers)
                    {
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                foreach (var header in request.Headers)
                {
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                return Task.FromResult(clone);
            }
        }
    }
}
