using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TBC.OpenAPI.SDK.Core.Authentication;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class OAuthDelegatingHandlerTests
    {
        private const string Scope = "some-scope";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private readonly Mock<IOAuthTokenCacheHelper<ITestClient>> _tokenCacheHelper = new(MockBehavior.Strict);
        private readonly RecordingInnerHandler _inner = new();

        private HttpClient CreateClient()
        {
            var sut = new OAuthDelegatingHandler<ITestClient>(_tokenCacheHelper.Object)
            {
                InnerHandler = _inner
            };
            return new HttpClient(sut);
        }

        [Fact]
        public async Task SendAsync_WhenScopeHeaderPresent_AttachesBearerAndRemovesMarkerHeader()
        {
            _tokenCacheHelper
                .Setup(x => x.GetTokenAsync(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = "the-token" });

            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
            request.Headers.Add(OAuthConstants.ScopeHeaderName, Scope);

            using var response = await client.SendAsync(request).WaitAsync(Timeout);

            _inner.LastRequest.Should().NotBeNull();
            _inner.LastRequest!.Headers.Authorization.Should().NotBeNull();
            _inner.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            _inner.LastRequest.Headers.Authorization.Parameter.Should().Be("the-token");
            _inner.LastRequest.Headers.Contains(OAuthConstants.ScopeHeaderName).Should().BeFalse();

            _tokenCacheHelper.Verify(
                x => x.GetTokenAsync(Scope, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendAsync_WhenNoScopeHeader_PassesThroughWithoutRequestingToken()
        {
            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

            using var response = await client.SendAsync(request).WaitAsync(Timeout);

            _inner.LastRequest.Should().NotBeNull();
            _inner.LastRequest!.Headers.Authorization.Should().BeNull();

            _tokenCacheHelper.Verify(
                x => x.GetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SendAsync_WhenScopeHeaderEmpty_PassesThroughWithoutRequestingToken()
        {
            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
            request.Headers.Add(OAuthConstants.ScopeHeaderName, string.Empty);

            using var response = await client.SendAsync(request).WaitAsync(Timeout);

            _inner.LastRequest.Should().NotBeNull();
            _inner.LastRequest!.Headers.Authorization.Should().BeNull();
            _inner.LastRequest.Headers.Contains(OAuthConstants.ScopeHeaderName).Should().BeFalse();

            _tokenCacheHelper.Verify(
                x => x.GetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SendAsync_WhenResponseUnauthorized_InvalidatesCachedToken()
        {
            _tokenCacheHelper
                .Setup(x => x.GetTokenAsync(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = "the-token" });
            _tokenCacheHelper
                .Setup(x => x.RemoveTokenAsync(Scope, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _inner.ResponseStatusCode = HttpStatusCode.Unauthorized;

            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
            request.Headers.Add(OAuthConstants.ScopeHeaderName, Scope);

            using var response = await client.SendAsync(request).WaitAsync(Timeout);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _tokenCacheHelper.Verify(
                x => x.RemoveTokenAsync(Scope, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendAsync_WhenResponseUnauthorized_DoesNotRetryAndSurfacesTheUnauthorizedResponse()
        {
            _tokenCacheHelper
                .Setup(x => x.GetTokenAsync(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = "the-token" });
            _tokenCacheHelper
                .Setup(x => x.RemoveTokenAsync(Scope, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _inner.ResponseStatusCode = HttpStatusCode.Unauthorized;

            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
            request.Headers.Add(OAuthConstants.ScopeHeaderName, Scope);

            using var response = await client.SendAsync(request).WaitAsync(Timeout);

            // Eviction only: the request is sent once and the 401 reaches the caller unchanged.
            // Recovery is one request late by design.
            _inner.CallCount.Should().Be(1);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _tokenCacheHelper.Verify(
                x => x.GetTokenAsync(Scope, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendAsync_WhenResponseSuccessful_DoesNotInvalidateCachedToken()
        {
            _tokenCacheHelper
                .Setup(x => x.GetTokenAsync(Scope, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokenResponse { AccessToken = "the-token" });

            _inner.ResponseStatusCode = HttpStatusCode.OK;

            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
            request.Headers.Add(OAuthConstants.ScopeHeaderName, Scope);

            using var response = await client.SendAsync(request).WaitAsync(Timeout);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _tokenCacheHelper.Verify(
                x => x.RemoveTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SendAsync_WhenRequestNull_ThrowsArgumentNullException()
        {
            var sut = new OAuthDelegatingHandler<ITestClient>(_tokenCacheHelper.Object)
            {
                InnerHandler = _inner
            };

            var sendAsync = typeof(OAuthDelegatingHandler<ITestClient>).GetMethod(
                "SendAsync",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(HttpRequestMessage), typeof(CancellationToken) },
                modifiers: null);
            sendAsync.Should().NotBeNull();

            var act = () =>
            {
                var task = (Task<HttpResponseMessage>)sendAsync!.Invoke(
                    sut,
                    new object?[] { null, CancellationToken.None })!;
                return task;
            };

            await FluentActions
                .Awaiting(() => act())
                .Should().ThrowAsync<ArgumentNullException>();
        }

        private sealed class RecordingInnerHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            public int CallCount { get; private set; }

            public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                CallCount++;
                return Task.FromResult(new HttpResponseMessage(ResponseStatusCode)
                {
                    RequestMessage = request
                });
            }
        }
    }
}
