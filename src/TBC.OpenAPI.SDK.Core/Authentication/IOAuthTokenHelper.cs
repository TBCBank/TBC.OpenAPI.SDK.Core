namespace TBC.OpenAPI.SDK.Core.Authentication
{
    public interface IOAuthTokenHelper<TClient>
        where TClient : class, IOpenApiClient
    {
        Task<OAuthTokenResponse> RequestToken(string scope, CancellationToken cancellationToken);
    }
}
