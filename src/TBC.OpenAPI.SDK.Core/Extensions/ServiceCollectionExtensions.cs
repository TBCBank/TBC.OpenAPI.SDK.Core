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
        /// Enables transparent OAuth client-credentials token acquisition and per-scope distributed
        /// caching for <typeparamref name="TClient"/>. Callers convey the scope per request through
        /// the <see cref="OAuthConstants.ScopeHeaderName"/> marker header; the registered
        /// <see cref="OAuthDelegatingHandler{TClient}"/> exchanges it for an
        /// <c>Authorization: Bearer</c> header and handles <c>401</c> refresh.
        /// <para>
        /// Call this after <see cref="AddOpenApiClient{TClientInterface,TClientImplementation,TOptions}"/>
        /// for the same client. If no <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>
        /// is registered, an in-memory distributed cache is registered as a fallback.
        /// </para>
        /// </summary>
        public static IServiceCollection AddOAuthTokenCaching<TClient>(this IServiceCollection services)
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

            services.AddDistributedMemoryCache();

            services.TryAddSingleton(typeof(IOAuthTokenHelper<>), typeof(OAuthTokenHelper<>));
            services.TryAddSingleton(typeof(IOAuthTokenCacheHelper<>), typeof(OAuthTokenCacheHelper<>));

            services.TryAddTransient<OAuthDelegatingHandler<TClient>>();

            services.AddHttpClient(typeof(TClient).FullName!)
                .AddHttpMessageHandler<OAuthDelegatingHandler<TClient>>();

            return services;
        }
    }
}
