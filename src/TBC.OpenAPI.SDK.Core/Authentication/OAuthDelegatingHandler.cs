using System.Net;
using System.Net.Http.Headers;

namespace TBC.OpenAPI.SDK.Core.Authentication
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that transparently attaches an OAuth bearer token to
    /// outgoing requests for <typeparamref name="TClient"/>.
    /// <para>
    /// The scope is conveyed per request through the <see cref="OAuthConstants.ScopeHeaderName"/>
    /// marker header. The handler removes that header, resolves and caches a token for the scope
    /// via <see cref="IOAuthTokenCacheHelper{TClient}"/>, and adds an
    /// <c>Authorization: Bearer</c> header. If the server responds with
    /// <see cref="HttpStatusCode.Unauthorized"/>, the cached token is invalidated so that the next
    /// request regenerates it.
    /// </para>
    /// <para>
    /// The handler does not retry: the <see cref="HttpStatusCode.Unauthorized"/> response is
    /// returned to the caller unchanged, and only the following request for that scope benefits
    /// from the eviction. Retry and backoff are the caller's responsibility.
    /// </para>
    /// <para>
    /// Requests without the marker header (for example the token endpoint call itself) are passed
    /// through untouched, which prevents recursion when the token endpoint shares the same
    /// <see cref="HttpClient"/> pipeline.
    /// </para>
    /// </summary>
    internal sealed class OAuthDelegatingHandler<TClient> : DelegatingHandler
        where TClient : class, IOpenApiClient
    {
        private readonly IOAuthTokenCacheHelper<TClient> _tokenCacheHelper;

        public OAuthDelegatingHandler(IOAuthTokenCacheHelper<TClient> tokenCacheHelper)
        {
            _tokenCacheHelper = tokenCacheHelper;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
#if NET
            ArgumentNullException.ThrowIfNull(request);
#else
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
#endif
            
            if (!TryExtractScope(request, out var scope))
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var token = await _tokenCacheHelper.GetTokenAsync(scope, cancellationToken).ConfigureAwait(false);
            SetBearer(request, token);

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _tokenCacheHelper.RemoveTokenAsync(scope, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private static bool TryExtractScope(HttpRequestMessage request, out string scope)
        {
            scope = string.Empty;

            if (!request.Headers.TryGetValues(OAuthConstants.ScopeHeaderName, out var values))
            {
                return false;
            }

            request.Headers.Remove(OAuthConstants.ScopeHeaderName);

            scope = values.FirstOrDefault() ?? string.Empty;
            return !string.IsNullOrEmpty(scope);
        }

        private static void SetBearer(HttpRequestMessage request, OAuthTokenResponse token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        }
    }
}
