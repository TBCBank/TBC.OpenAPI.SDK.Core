using TBC.OpenAPI.SDK.Core.Exceptions;
using TBC.OpenAPI.SDK.Core.Models;

namespace TBC.OpenAPI.SDK.Core.Authentication
{
    public sealed class OAuthTokenHelper<TClient> : IOAuthTokenHelper<TClient>
        where TClient : class, IOpenApiClient
    {
        private readonly IHttpHelper<TClient> _http;

        public OAuthTokenHelper(IHttpHelper<TClient> http)
        {
            _http = http;
        }

        public async Task<OAuthTokenResponse> RequestToken(string scope, CancellationToken cancellationToken)
        {
            var form = new UrlFormCollection
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            };

            var response = await _http
               .PostUrlFormAsync<OAuthTokenResponse>("oauth/token", form, cancellationToken)
               .ConfigureAwait(false);

            if (!response.IsSuccess ||
                response.Data is null ||
                string.IsNullOrEmpty(response.Data.AccessToken))
            {
                throw new OpenApiException(
                    response.Problem?.Title ?? "Token request was unsuccessful",
                    response.Exception);
            }

            return response.Data;
        }
    }
}
