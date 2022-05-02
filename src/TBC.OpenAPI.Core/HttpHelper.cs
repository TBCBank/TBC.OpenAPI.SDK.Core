using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TBC.OpenAPI.Core.Models;

namespace TBC.OpenAPI.Core
{
    public class HttpHelper
    {
        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public HttpHelper(HttpClient httpClient) : this(httpClient, DefaultJsonSerializerOptions)
        {
        }

        public HttpHelper(HttpClient httpClient, JsonSerializerOptions jsonSerializerOptions)
        {
            if (httpClient.BaseAddress == null)
                throw new ArgumentNullException(nameof(httpClient.BaseAddress));

            _httpClient = httpClient;
            _jsonSerializerOptions = jsonSerializerOptions;
        }

        #region Get

        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, CancellationToken cancellationToken = default)
            => GetJsonAsync<TResponseData>(path, null, null, cancellationToken);

        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, QueryParamCollection? query, CancellationToken cancellationToken = default)
            => GetJsonAsync<TResponseData>(path, query, null, cancellationToken);

        public async Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            using var requestMessage = CreateRequestMessage(HttpMethod.Get, path, query, headers);
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            return await DeserializeResponse<TResponseData>(response).ConfigureAwait(false);
        }

        #endregion

        #region Post

        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PostJsonAsync<TRequestData, TResponseData>(path, data, null, null, cancellationToken);

        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PostJsonAsync<TRequestData, TResponseData>(path, data, query, null, cancellationToken);

        public async Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            using var requestMessage = CreateRequestMessage(HttpMethod.Post, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            return await DeserializeResponse<TResponseData>(response).ConfigureAwait(false);
        }


        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PostJsonAsync(path, data, null, null, cancellationToken);

        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PostJsonAsync(path, data, query, null, cancellationToken);

        public async Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            using var requestMessage = CreateRequestMessage(HttpMethod.Post, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            return await DeserializeResponse(response).ConfigureAwait(false);
        }

        #endregion

        #region Put

        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PutJsonAsync<TRequestData, TResponseData>(path, data, null, null, cancellationToken);

        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PutJsonAsync<TRequestData, TResponseData>(path, data, query, null, cancellationToken);

        public async Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            using var requestMessage = CreateRequestMessage(HttpMethod.Put, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            return await DeserializeResponse<TResponseData>(response).ConfigureAwait(false);
        }


        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, CancellationToken cancellationToken = default)
            => PutJsonAsync(path, data, null, null, cancellationToken);

        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default)
            => PutJsonAsync(path, data, query, null, cancellationToken);

        public async Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default)
        {
            using var requestMessage = CreateRequestMessage(HttpMethod.Post, path, query, headers);
            var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            return await DeserializeResponse(response).ConfigureAwait(false);
        }

        #endregion


        private HttpRequestMessage CreateRequestMessage(HttpMethod method, string path, QueryParamCollection? query = null, HeaderParamCollection? headers = null)
        {
            var requestMessage = new HttpRequestMessage(method, GetUri(path, query));
            if (headers != null)
                headers.ApplyHeaders(requestMessage.Headers);

            return requestMessage;
        }

        private Uri GetUri(string path, QueryParamCollection? query = null)
        {
            var builder = new UriBuilder(new Uri(_httpClient.BaseAddress!, path));
            if(query != null)
                builder.Query = query.ToQueryString();

            return builder.Uri;
        }

        private async Task<ApiResponse<TData>> DeserializeResponse<TData>(HttpResponseMessage response)
        {
            var result = new ApiResponse<TData>();
            var responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            result.Headers = response.Headers.Any() ? response.Headers.ToDictionary(x => x.Key, y => y.Value) : default;
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.Data = !string.IsNullOrEmpty(responseMessage) ? JsonSerializer.Deserialize<TData>(responseMessage, _jsonSerializerOptions) : default;
                }
                else
                {
                    result.IsSuccess = false;
                    result.Problem = !string.IsNullOrEmpty(responseMessage) ? JsonSerializer.Deserialize<ProblemDetails>(responseMessage, _jsonSerializerOptions) : default;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
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
            return result;
        }

        private async Task<ApiResponseBase> DeserializeResponse(HttpResponseMessage response)
        {
            var result = new ApiResponseBase();
            var responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            result.Headers = response.Headers.Any() ? response.Headers.ToDictionary(x => x.Key, y => y.Value) : default;
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                }
                else
                {
                    result.IsSuccess = false;
                    result.Problem = !string.IsNullOrEmpty(responseMessage) ? JsonSerializer.Deserialize<ProblemDetails>(responseMessage, _jsonSerializerOptions) : default;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
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
            return result;
        }
    }
}
