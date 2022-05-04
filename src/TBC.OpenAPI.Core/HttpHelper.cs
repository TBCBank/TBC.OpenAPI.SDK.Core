using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TBC.OpenAPI.Core.Extensions;
using TBC.OpenAPI.Core.Models;

namespace TBC.OpenAPI.Core
{
    public class HttpHelper<TClient>
        where TClient : class, IOpenApiClient
    {
        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public HttpHelper(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _jsonSerializerOptions = DefaultJsonSerializerOptions;
        }

        #region Get

        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, CancellationToken cancellationToken = default)
            => GetJsonAsync<TResponseData>(path, null, null, cancellationToken);

        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, QueryParamCollection? query, CancellationToken cancellationToken = default)
            => GetJsonAsync<TResponseData>(path, query, null, cancellationToken);

        public async Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            var httpClient = GetHttpClient();
            var requestMessage = CreateRequestMessage(httpClient, HttpMethod.Get, path, query, headers);
            return await SendRequestMessage<TResponseData>(httpClient, requestMessage, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Post

        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PostJsonAsync<TRequestData, TResponseData>(path, data, null, null, cancellationToken);

        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PostJsonAsync<TRequestData, TResponseData>(path, data, query, null, cancellationToken);

        public async Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            var httpClient = GetHttpClient();
            var requestMessage = CreateRequestMessage(httpClient, HttpMethod.Post, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestMessage<TResponseData>(httpClient, requestMessage, cancellationToken).ConfigureAwait(false);
        }


        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PostJsonAsync(path, data, null, null, cancellationToken);

        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PostJsonAsync(path, data, query, null, cancellationToken);

        public async Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            var httpClient = GetHttpClient();
            var requestMessage = CreateRequestMessage(httpClient, HttpMethod.Post, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestMessage(httpClient, requestMessage, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Put

        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PutJsonAsync<TRequestData, TResponseData>(path, data, null, null, cancellationToken);

        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PutJsonAsync<TRequestData, TResponseData>(path, data, query, null, cancellationToken);

        public async Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            var httpClient = GetHttpClient();
            var requestMessage = CreateRequestMessage(httpClient, HttpMethod.Put, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestMessage<TResponseData>(httpClient, requestMessage, cancellationToken).ConfigureAwait(false);
        }


        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PutJsonAsync(path, data, null, null, cancellationToken);

        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PutJsonAsync(path, data, query, null, cancellationToken);

        public async Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            var httpClient = GetHttpClient();
            var requestMessage = CreateRequestMessage(httpClient, HttpMethod.Post, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestMessage(httpClient, requestMessage, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        private HttpClient GetHttpClient() => _httpClientFactory.CreateOpenApiClient<TClient>();

        private HttpRequestMessage CreateRequestMessage(HttpClient httpClient, HttpMethod method, string path, QueryParamCollection? query = null, HeaderParamCollection? headers = null)
        {
            var requestMessage = new HttpRequestMessage(method, GetUri(httpClient, path, query));
            if (headers != null)
                headers.ApplyHeaders(requestMessage.Headers);

            return requestMessage;
        }

        private Uri GetUri(HttpClient httpClient, string path, QueryParamCollection? query = null)
        {
            var builder = new UriBuilder(httpClient.BaseAddress!);
            builder.Path = $"{builder.Path.TrimEnd('/')}/{path.TrimStart('/')}";
            if (query != null)
                builder.Query = query.ToQueryString();

            return builder.Uri;
        }

        private async Task<ApiResponse<TData>> SendRequestMessage<TData>(HttpClient httpClient, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            var result = new ApiResponse<TData>();

            HttpResponseMessage response;
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Problem = new ProblemDetails
                {
                    Type = ProblemDetails.HttpRequestSendErrorCode,
                    Code = ProblemDetails.HttpRequestSendErrorCode,
                    Title = ProblemDetails.HttpRequestSendErrorTitle
                };
                result.Exception = ex;
                return result;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            result.IsSuccess = response.IsSuccessStatusCode;
            result.Headers = response.Headers.Any() ? response.Headers.ToDictionary(x => x.Key, y => y.Value) : default;

            string? responseMessage = null;
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(responseMessage))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        result.Data = JsonSerializer.Deserialize<TData>(responseMessage, _jsonSerializerOptions);
                    }
                    else
                    {
                        result.Problem = JsonSerializer.Deserialize<ProblemDetails>(responseMessage, _jsonSerializerOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Problem = new ProblemDetails
                {
                    Type = ProblemDetails.MessageDeserializationErrorCode,
                    Code = ProblemDetails.MessageDeserializationErrorCode,
                    Title = ProblemDetails.MessageDeserializationErrorTitle,
                    Detail = responseMessage,
                    Status = (int)response.StatusCode
                };
                result.Exception = ex;
            }
#pragma warning restore CA1031 // Do not catch general exception types
            return result;
        }

        private async Task<ApiResponseBase> SendRequestMessage(HttpClient httpClient, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            var result = new ApiResponseBase();

            HttpResponseMessage response;
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Problem = new ProblemDetails
                {
                    Type = ProblemDetails.HttpRequestSendErrorCode,
                    Code = ProblemDetails.HttpRequestSendErrorCode,
                    Title = ProblemDetails.HttpRequestSendErrorTitle
                };
                result.Exception = ex;
                return result;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            result.IsSuccess = response.IsSuccessStatusCode;
            result.Headers = response.Headers.Any() ? response.Headers.ToDictionary(x => x.Key, y => y.Value) : default;

            string? responseMessage = null;
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(responseMessage))
                    {
                        result.Problem = JsonSerializer.Deserialize<ProblemDetails>(responseMessage, _jsonSerializerOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Problem = new ProblemDetails
                {
                    Type = ProblemDetails.MessageDeserializationErrorCode,
                    Code = ProblemDetails.MessageDeserializationErrorCode,
                    Title = ProblemDetails.MessageDeserializationErrorTitle,
                    Detail = responseMessage,
                    Status = (int)response.StatusCode
                };
                result.Exception = ex;
            }
#pragma warning restore CA1031 // Do not catch general exception types
            return result;
        }
    }
}