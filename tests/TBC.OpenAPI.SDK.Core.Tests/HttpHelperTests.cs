using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using TBC.OpenAPI.SDK.Core.Tests.Models;
using Xunit;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class HttpHelperTests : IClassFixture<HttpHelperMocks>
    {
        private readonly HttpHelper<TestClient> _http; 
        public HttpHelperTests(HttpHelperMocks mocks)
        {
            var mock = new Mock<IHttpClientFactory>();
            mock.Setup(x => x.CreateClient(typeof(TestClient).FullName)).Returns(mocks.HttpClient);
            _http = new HttpHelper<TestClient>(mock.Object);
        }

        [Fact]
        public async Task GetJsonAsync_WhenResourceExists_ShouldRespondOkMessageWithData()
        {
            var response = await _http.GetJsonAsync<HttpTestResponseModel>("/some-resource", CancellationToken.None);

            using (new AssertionScope())
            {
                response.IsSuccess.Should().BeTrue();

                response.Data.Should().NotBeNull();
                response.Data.Id.Should().Be(1);
                response.Data.Name.Should().Be("One");
                response.Data.Date.Should().Be(new DateTime(2001, 1, 1));
                response.Data.Numbers.Should().Equal(new List<int>(3) { 1, 2, 3 });

                response.Headers.Should().NotBeNull();
                response.Headers.Should().Contain(x => x.Key == "some-header" && x.Value.Contains("some value"));
                response.Headers.Should().Contain(x => x.Key == "another-header" && x.Value.Contains("another value"));
            }
        }

        [Fact]
        public async Task GetJsonAsync_WhenResourceDoesNotExists_ShouldRespondNotFoundMessageWithProblemDetails()
        {
            var response = await _http.GetJsonAsync<HttpTestResponseModel>("/some-resource/5", CancellationToken.None);

            using (new AssertionScope())
            {
                response.IsSuccess.Should().BeFalse();

                response.Problem.Should().NotBeNull();
                response.Problem.Code.Should().Be("ResourceNotFound");
                response.Problem.Type.Should().Be("ResourceNotFound");
                response.Problem.Title.Should().Be("Requested Resource Not Found");
                response.Problem.Detail.Should().Contain("ID=5");
                response.Problem.Status.Should().Be(404);
                response.Problem.Instance.Should().Be("/some-resource/5");
                response.Problem.TraceId.Should().NotBeEmpty();

                response.Headers.Should().NotBeNull();
                response.Headers.Should().Contain(x => x.Key == "some-header" && x.Value.Contains("some value"));
                response.Headers.Should().Contain(x => x.Key == "another-header" && x.Value.Contains("another value"));
            }
        }
    }
}
