using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Covers which named <see cref="System.Net.Http.HttpClient"/> token caching binds to. The
    /// handler is attached to the client named after the type argument, while AddOpenApiClient
    /// configures that client under the implementation type, so only the implementation type wires
    /// the two together.
    /// </summary>
    public class OAuthTokenCachingClientBindingTests : IDisposable
    {
        private const string Scope = "read:some-resource";
        private const string AccessToken = "the-access-token";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        private readonly WireMockServer _server = WireMockServer.Start();

        [Fact]
        public async Task AddOAuthTokenCaching_WhenClientSendsScopedRequest_ShouldAttachBearerTokenAndStripScopeHeader()
        {
            // Arrange
            StubTokenEndpoint();
            StubResourceEndpoint();

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
                response.IsSuccess.Should().BeTrue();

                var resourceRequest = SingleRequestTo("/some-resource");
                FindHeader(resourceRequest, "Authorization").Should().Be($"Bearer {AccessToken}");
                FindHeader(resourceRequest, OAuthConstants.ScopeHeaderName).Should().BeNull();

                // The token endpoint call proves the token helper resolved the configured client:
                // an unbound named client would have no BaseAddress.
                var tokenRequest = SingleRequestTo("/oauth/token");
                FindHeader(tokenRequest, "apikey").Should().Be("the-api-key");
                FindHeader(tokenRequest, "Authorization").Should().BeNull();
            }
        }

        [Fact]
        public async Task AddOAuthTokenCaching_WhenSameScopeRequestedTwice_ShouldRequestTokenOnce()
        {
            // Arrange
            StubTokenEndpoint();
            StubResourceEndpoint();

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
            await client.GetResourceAsync(Scope, CancellationToken.None).WaitAsync(Timeout);
            await client.GetResourceAsync(Scope, CancellationToken.None).WaitAsync(Timeout);

            // Assert
            using (new AssertionScope())
            {
                RequestsTo("/some-resource").Should().HaveCount(2);
                RequestsTo("/oauth/token").Should().ContainSingle();
            }
        }

        [Fact]
        public void AddOAuthTokenCaching_WhenGivenTheClientInterface_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOpenApiClient<IBindingTestClient, BindingTestClient, BindingTestClientOptions>(
                new BindingTestClientOptions
                {
                    BaseUrl = "https://example.test/",
                    ApiKey = "the-api-key"
                });

            // Act
            Action act = () => services.AddOAuthTokenCaching<IBindingTestClient>();

            // Assert
            using (new AssertionScope())
            {
                var assertion = act.Should().Throw<InvalidOperationException>();
                assertion.Which.Message.Should().Contain(typeof(IBindingTestClient).FullName);
                assertion.Which.Message.Should().Contain("implementation type");
                assertion.Which.Message.Should().Contain("IHttpHelper<T>");
            }
        }

        [Fact]
        public void AddOAuthTokenCaching_WhenGivenTheImplementationType_ShouldNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOpenApiClient<IBindingTestClient, BindingTestClient, BindingTestClientOptions>(
                new BindingTestClientOptions
                {
                    BaseUrl = "https://example.test/",
                    ApiKey = "the-api-key"
                });

            // Act
            Action act = () => services.AddOAuthTokenCaching<BindingTestClient>().UseInMemoryCache();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AddOAuthTokenCaching_WhenGivenAnInterfaceAndNoClientRegistered_ShouldStillThrow()
        {
            // Arrange - an interface can never name the configured HttpClient, so the guard does not
            // depend on AddOpenApiClient having run first.
            var services = new ServiceCollection();

            // Act
            Action act = () => services.AddOAuthTokenCaching<IBindingTestClient>();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .Which.Message.Should().Contain(typeof(IBindingTestClient).FullName);
        }

        [Fact]
        public void OpenApiClientFactoryBuilder_AddOAuthTokenCaching_WhenGivenTheClientInterface_ShouldThrow()
        {
            // Arrange
            var factoryBuilder = new OpenApiClientFactoryBuilder()
                .AddClient<IBindingTestClient, BindingTestClient, BindingTestClientOptions>(
                    new BindingTestClientOptions
                    {
                        BaseUrl = "https://example.test/",
                        ApiKey = "the-api-key"
                    });

            // Act
            Action act = () => factoryBuilder.AddOAuthTokenCaching<IBindingTestClient>();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .Which.Message.Should().Contain(typeof(IBindingTestClient).FullName);
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

        private void StubResourceEndpoint()
        {
            _server
                .Given(Request.Create().WithPath("/some-resource").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{\"id\":1}"));
        }

        private IEnumerable<IRequestMessage> RequestsTo(string path)
            => _server.LogEntries
                .Select(x => x.RequestMessage)
                .Where(x => string.Equals(x.Path, path, StringComparison.Ordinal));

        private IRequestMessage SingleRequestTo(string path) => RequestsTo(path).Single();

        private static string? FindHeader(IRequestMessage request, string name)
        {
            if (request.Headers is null)
            {
                return null;
            }

            foreach (var header in request.Headers)
            {
                if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Value?.FirstOrDefault();
                }
            }

            return null;
        }
    }

    public interface IBindingTestClient : IOpenApiClient
    {
        Task<ApiResponse<BindingTestResource>> GetResourceAsync(string scope, CancellationToken cancellationToken);
    }

    public class BindingTestClient : IBindingTestClient
    {
        private readonly IHttpHelper<BindingTestClient> _http;

        public BindingTestClient(IHttpHelper<BindingTestClient> http)
        {
            _http = http;
        }

        public Task<ApiResponse<BindingTestResource>> GetResourceAsync(string scope, CancellationToken cancellationToken)
        {
            var headers = new HeaderParamCollection
            {
                [OAuthConstants.ScopeHeaderName] = scope
            };

            return _http.GetJsonAsync<BindingTestResource>("/some-resource", query: null, headers, cancellationToken);
        }
    }

    public class BindingTestClientOptions : OptionsBase
    {
    }

    public class BindingTestResource
    {
        public int Id { get; set; }
    }
}
