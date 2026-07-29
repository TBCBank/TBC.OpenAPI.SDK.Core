using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;
using TBC.OpenAPI.SDK.Core.Authentication;

namespace TBC.OpenAPI.SDK.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenApiClient<TClientInterface, TClientImplementation, TOptions>(this IServiceCollection services,
            TOptions options,
            Action<HttpClient>? configureClient = null,
            Func<HttpClientHandler>? configureHttpMessageHandler = null)
        where TClientInterface : class, IOpenApiClient
        where TClientImplementation : class, TClientInterface
        where TOptions : OptionsBase
        {
            var httpClientBuilder = services.AddHttpClient(typeof(TClientImplementation).FullName!, client =>
            {
               
                if (options is BasicAuthOptions opt)
                {
                    string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                               .GetBytes(opt.ApiKey + ":" + opt.ClientSecret));
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + encoded);
                }
                else if (typeof(OptionsBase).IsAssignableFrom(typeof(TOptions)))
                {
                    client.DefaultRequestHeaders.Add("apikey", options.ApiKey);
                }
                client.BaseAddress = new Uri(options.BaseUrl);
                configureClient?.Invoke(client);
            });

            if (configureHttpMessageHandler != null)
            {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(configureHttpMessageHandler);
            }

            services.TryAddSingleton(typeof(IHttpHelper<>), typeof(HttpHelper<>));
            services.TryAddSingleton<TClientInterface, TClientImplementation>();
            services.TryAddSingleton(options);

            return services;
        }

        /// <summary>
        /// Enables transparent OAuth client-credentials token acquisition and per-scope caching for
        /// <typeparamref name="TClient"/>. Callers convey the scope per request through the
        /// <see cref="OAuthConstants.ScopeHeaderName"/> marker header; the registered
        /// <see cref="OAuthDelegatingHandler{TClient}"/> exchanges it for an
        /// <c>Authorization: Bearer</c> header.
        /// <para>
        /// <typeparamref name="TClient"/> must be the <em>implementation</em> type of the client -
        /// the same type argument the client passes to <see cref="IHttpHelper{TClient}"/> - because
        /// that is the named <see cref="HttpClient"/>
        /// <see cref="AddOpenApiClient{TClientInterface,TClientImplementation,TOptions}"/> configures.
        /// Passing the client interface throws.
        /// </para>
        /// <para>
        /// There is no token refresh: a <c>401 Unauthorized</c> response evicts the cached token and
        /// is returned to the caller unchanged. The request that hit the <c>401</c> is not retried;
        /// the next request for that scope acquires a fresh token. Tokens are otherwise renewed only
        /// when they expire out of the cache.
        /// </para>
        /// <para>
        /// To turn that eviction into an actual retry, supply <paramref name="configurePipeline"/>.
        /// The <see cref="IHttpClientBuilder"/> it receives is the client's own pipeline, and any
        /// handler it registers is placed <em>outside</em> the
        /// <see cref="OAuthDelegatingHandler{TClient}"/> - the only position from which a retried
        /// attempt re-enters token handling and, because the <c>401</c> already evicted the token,
        /// acquires a fresh one. This SDK ships no retry logic and takes no dependency on any
        /// resilience library; you plug in your own (for example
        /// <c>Microsoft.Extensions.Http.Resilience</c>'s <c>AddResilienceHandler</c>, or a custom
        /// <see cref="DelegatingHandler"/>). A retry handler <b>must clone the request per attempt</b>:
        /// the scope marker header is consumed by <see cref="OAuthDelegatingHandler{TClient}"/> and
        /// the request content is consumed when it is sent, so re-sending the same
        /// <see cref="HttpRequestMessage"/> fails. <c>Microsoft.Extensions.Http.Resilience</c> clones
        /// automatically; a custom handler must do so itself. Scope the retry to
        /// <c>401 Unauthorized</c> responses to avoid re-issuing genuinely failed requests.
        /// </para>
        /// <para>
        /// Call this after <see cref="AddOpenApiClient{TClientInterface,TClientImplementation,TOptions}"/>
        /// for the same client, then select a cache on the returned builder with exactly one of
        /// <see cref="OAuthTokenCachingBuilder{TClient}.UseInMemoryCache"/>,
        /// <see cref="OAuthTokenCachingBuilder{TClient}.UseRegisteredDistributedCache"/> or
        /// <see cref="OAuthTokenCachingBuilder{TClient}.UseDistributedCache(Microsoft.Extensions.Caching.Distributed.IDistributedCache)"/>.
        /// No cache backend is registered on your behalf, and there is no implicit fallback: until a
        /// cache is selected, resolving the client throws an error naming the available options.
        /// </para>
        /// </summary>
        /// <param name="services">The service collection to register the token handling into.</param>
        /// <param name="configurePipeline">
        /// Optional hook that configures the client's HTTP pipeline. Handlers it registers are placed
        /// outside the OAuth handler, which is where a retry has to sit to benefit from the token
        /// eviction a <c>401</c> triggers. When <see langword="null"/> the behaviour is unchanged: a
        /// <c>401</c> evicts the token and is returned to the caller without a retry.
        /// </param>
        /// <returns>A builder used to select where access tokens are cached.</returns>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="TClient"/> is an interface rather than a client implementation type.
        /// </exception>
        public static OAuthTokenCachingBuilder<TClient> AddOAuthTokenCaching<TClient>(
            this IServiceCollection services,
            Action<IHttpClientBuilder>? configurePipeline = null)
            where TClient : class, IOpenApiClient
        {
#if NET
            ArgumentNullException.ThrowIfNull(services);
#else
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
#endif

            OAuthTokenCachingRegistration.ValidateClientTypeArgument<TClient>();

            services.TryAddSingleton(typeof(IOAuthTokenHelper<>), typeof(OAuthTokenHelper<>));
            OAuthTokenCachingRegistration.AddUnconfiguredCache<TClient>(services);

            services.TryAddTransient<OAuthDelegatingHandler<TClient>>();

            var httpClientBuilder = services.AddHttpClient(typeof(TClient).FullName!);

            // Handlers are nested in registration order, outermost first. Registering the caller's
            // pipeline before the OAuth handler puts any retry it adds outside token handling, so a
            // retried attempt re-enters the OAuth handler and picks up a token freshly acquired after
            // the 401 eviction. Registering it afterwards would trap the retry inside the OAuth
            // handler, where the bearer is already set and no re-acquisition happens.
            configurePipeline?.Invoke(httpClientBuilder);

            httpClientBuilder.AddHttpMessageHandler<OAuthDelegatingHandler<TClient>>();

            return new OAuthTokenCachingBuilder<TClient>(services);
        }
    }
}
