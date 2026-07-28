namespace TBC.OpenAPI.SDK.Core.Authentication
{
    public interface IOAuthTokenCacheHelper<TClient>
        where TClient : class, IOpenApiClient
    {
        /// <summary>
        /// Gets a cached access token for the specified <paramref name="scope"/>, requesting and
        /// caching a fresh one if none is cached (or if the cached one has expired).
        /// </summary>
        Task<OAuthTokenResponse> GetTokenAsync(string scope, CancellationToken cancellationToken);

        /// <summary>
        /// Removes any cached access token for the specified <paramref name="scope"/> so that the
        /// next call to <see cref="GetTokenAsync"/> requests a fresh one. Used to recover from a
        /// <c>401 Unauthorized</c> response caused by a stale/revoked token.
        /// </summary>
        Task RemoveTokenAsync(string scope, CancellationToken cancellationToken);
    }
}
