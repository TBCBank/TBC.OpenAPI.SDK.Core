using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using TBC.OpenAPI.SDK.Core.Models;
using TBC.OpenAPI.SDK.Core.Tests.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class HttpHelperMocks : IDisposable
    {
        private readonly WireMockServer _mockServer;
        private readonly HttpClient _httpClient;

        public HttpClient HttpClient => _httpClient;

        public HttpHelperMocks()
        {
            _mockServer = WireMockServer.Start();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri($"{_mockServer.Urls[0]}/");

            AddGetMocks();
        }

        private void AddGetMocks()
        {
            _mockServer
                .Given(
                    Request.Create()
                    .WithPath("/some-resource")
                    .UsingMethod("GET")
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("some-header", "some value")
                        .WithHeader("another-header", "another value")
                        .WithBodyAsJson(new HttpTestResponseModel
                        {
                            Id = 1,
                            Name = "One",
                            Date = new DateTime(2001, 1, 1),
                            Numbers = new List<int>(3) { 1, 2, 3 }
                        })
                );

            _mockServer
                .Given(
                    Request.Create()
                        .WithPath("/some-resource/5")
                        .UsingMethod("GET")
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(404)
                        .WithHeader("some-header", "some value")
                        .WithHeader("another-header", "another value")
                        .WithBody(x =>
                        {
                            var problem = new ProblemDetails
                            {
                                Type = "ResourceNotFound",
                                Code = "ResourceNotFound",
                                Title = "Requested Resource Not Found",
                                Detail = "some-resource with ID=5 not found",
                                Status = 404,
                                Instance = "/some-resource/5",
                                TraceId = "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01"
                            };
                            return JsonSerializer.Serialize(problem);
                        })
                );
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _mockServer.Dispose();
        }
    }
}
