namespace TBC.OpenAPI.SDK.Core.Extensions
{
    public static class HttpClientFactoryExtensions
    {
        public static HttpClient CreateOpenApiClient<TClient>(this IHttpClientFactory httpClientFactory)
            where TClient : class, IOpenApiClient
        {
            if(httpClientFactory is null) 
                throw new ArgumentNullException(nameof(httpClientFactory));

            return httpClientFactory.CreateClient(typeof(TClient).FullName);
        }
    }
}
