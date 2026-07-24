using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TBC.OpenAPI.SDK.Core.Authentication;
using TBC.OpenAPI.SDK.Core.Exceptions;
using TBC.OpenAPI.SDK.Core.Models;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class OAuthTokenHelperTests
    {
        private const string Scope = "some-scope";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private readonly Mock<IHttpHelper<ITestClient>> _http = new(MockBehavior.Strict);

        private OAuthTokenHelper<ITestClient> CreateSut()
            => new(_http.Object);

        [Fact]
        public async Task RequestToken_WhenSuccessful_PostsClientCredentialsFormAndReturnsData()
        {
            var expected = new OAuthTokenResponse { AccessToken = "fresh-token", ExpiresIn = 3600 };
            UrlFormCollection? capturedForm = null;
            string? capturedPath = null;

            _http
                .Setup(x => x.PostUrlFormAsync<OAuthTokenResponse>(
                    It.IsAny<string>(),
                    It.IsAny<UrlFormCollection>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, UrlFormCollection, CancellationToken>((path, form, _) =>
                {
                    capturedPath = path;
                    capturedForm = form;
                })
                .ReturnsAsync(new ApiResponse<OAuthTokenResponse> { IsSuccess = true, Data = expected });

            var sut = CreateSut();

            var result = await sut.RequestToken(Scope, CancellationToken.None).WaitAsync(Timeout);

            result.Should().BeSameAs(expected);
            capturedPath.Should().Be("oauth/token");
            capturedForm.Should().NotBeNull();
            capturedForm!["grant_type"].Should().Be("client_credentials");
            capturedForm["scope"].Should().Be(Scope);
        }

        [Fact]
        public async Task RequestToken_WhenNotSuccessful_ThrowsWithProblemTitle()
        {
            _http
                .Setup(x => x.PostUrlFormAsync<OAuthTokenResponse>(
                    It.IsAny<string>(),
                    It.IsAny<UrlFormCollection>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse<OAuthTokenResponse>
                {
                    IsSuccess = false,
                    Problem = new ProblemDetails { Title = "invalid_client" }
                });

            var sut = CreateSut();

            await FluentActions
                .Awaiting(() => sut.RequestToken(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>()
                .WithMessage("invalid_client");
        }

        [Fact]
        public async Task RequestToken_WhenNotSuccessfulAndNoProblem_ThrowsWithFallbackMessage()
        {
            _http
                .Setup(x => x.PostUrlFormAsync<OAuthTokenResponse>(
                    It.IsAny<string>(),
                    It.IsAny<UrlFormCollection>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse<OAuthTokenResponse> { IsSuccess = false, Problem = null });

            var sut = CreateSut();

            await FluentActions
                .Awaiting(() => sut.RequestToken(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>()
                .WithMessage("Token request was unsuccessful");
        }

        [Fact]
        public async Task RequestToken_WhenNotSuccessful_PropagatesInnerException()
        {
            var inner = new InvalidOperationException("transport-failure");
            _http
                .Setup(x => x.PostUrlFormAsync<OAuthTokenResponse>(
                    It.IsAny<string>(),
                    It.IsAny<UrlFormCollection>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse<OAuthTokenResponse> { IsSuccess = false, Exception = inner });

            var sut = CreateSut();

            var thrown = await FluentActions
                .Awaiting(() => sut.RequestToken(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>();

            thrown.Which.InnerException.Should().BeSameAs(inner);
        }

        [Fact]
        public async Task RequestToken_WhenDataNull_Throws()
        {
            _http
                .Setup(x => x.PostUrlFormAsync<OAuthTokenResponse>(
                    It.IsAny<string>(),
                    It.IsAny<UrlFormCollection>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse<OAuthTokenResponse> { IsSuccess = true, Data = null });

            var sut = CreateSut();

            await FluentActions
                .Awaiting(() => sut.RequestToken(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RequestToken_WhenAccessTokenMissing_Throws(string? accessToken)
        {
            _http
                .Setup(x => x.PostUrlFormAsync<OAuthTokenResponse>(
                    It.IsAny<string>(),
                    It.IsAny<UrlFormCollection>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse<OAuthTokenResponse>
                {
                    IsSuccess = true,
                    Data = new OAuthTokenResponse { AccessToken = accessToken }
                });

            var sut = CreateSut();

            await FluentActions
                .Awaiting(() => sut.RequestToken(Scope, CancellationToken.None).WaitAsync(Timeout))
                .Should().ThrowAsync<OpenApiException>();
        }
    }
}
