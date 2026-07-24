namespace TBC.OpenAPI.SDK.Core.Authentication
{
    public static class OAuthConstants
    {
        /// <summary>
        /// Minimum number of seconds to cache an access token.
        /// Tokens with no or shorter lifetime will be cached for this duration.
        /// </summary>
        public const int MinCacheTtlSec = 30;

        /// <summary>
        /// Number of seconds to subtract from the token's <c>expires_in</c> value when caching it,
        /// to avoid using a token that is about to expire.
        /// </summary>
        public const int TokenTimeoutGracePeriodSec = 30;

        /// <summary>
        /// Name of the per-request marker header used to convey the OAuth scope to
        /// <see cref="OAuthDelegatingHandler{TClient}"/>. The handler reads and removes this
        /// header, then injects an <c>Authorization: Bearer</c> header for the resolved scope.
        /// Requests without this header bypass token handling (e.g. the token endpoint itself).
        /// </summary>
        public const string ScopeHeaderName = "X-TBC-OAuth-Scope";

        /// <summary>
        /// Prefix used when composing the distributed cache key for a cached access token.
        /// The full key also includes the client type name and the scope.
        /// </summary>
        public const string CacheKeyPrefix = "TbcOpenApiOAuthToken";
    }
}
