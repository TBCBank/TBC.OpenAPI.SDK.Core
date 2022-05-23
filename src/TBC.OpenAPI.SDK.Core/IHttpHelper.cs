using TBC.OpenAPI.SDK.Core.Models;

namespace TBC.OpenAPI.SDK.Core
{
    public interface IHttpHelper<TClient>
        where TClient : class, IOpenApiClient
    {
        #region Get
        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, CancellationToken cancellationToken = default);

        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, QueryParamCollection? query, CancellationToken cancellationToken = default);

        public Task<ApiResponse<TResponseData>> GetJsonAsync<TResponseData>(string path, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default);
        #endregion

        #region Post
        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, CancellationToken cancellationToken = default);

        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default);

        public Task<ApiResponse<TResponseData>> PostJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default);

        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, CancellationToken cancellationToken = default);

        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default);

        public Task<ApiResponseBase> PostJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default);
        #endregion

        #region Put
        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, CancellationToken cancellationToken = default);

        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default);

        public Task<ApiResponse<TResponseData>> PutJsonAsync<TRequestData, TResponseData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default);

        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, CancellationToken cancellationToken = default);

        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, CancellationToken cancellationToken = default);

        public Task<ApiResponseBase> PutJsonAsync<TRequestData>(string path, TRequestData data, QueryParamCollection? query = null, HeaderParamCollection? headers = null, CancellationToken cancellationToken = default);
        #endregion
    }
}