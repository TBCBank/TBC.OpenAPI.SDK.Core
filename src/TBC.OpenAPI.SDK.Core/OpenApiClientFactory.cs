using Microsoft.Extensions.DependencyInjection;
using TBC.OpenAPI.SDK.Core.Authentication;
using TBC.OpenAPI.SDK.Core.Extensions;

namespace TBC.OpenAPI.SDK.Core
{
    public class OpenApiClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        private static OpenApiClientFactory? _instance;
        public static OpenApiClientFactory Instance
        {
            get => _instance ?? throw new InvalidOperationException("OpenApiClientFactory is not built. Please use OpenApiClientFactoryBuilder to build OpenApiClientFactory.");
            internal set => _instance = value;
        }

        internal OpenApiClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TClient GetOpenApiClient<TClient>()
            where TClient : class, IOpenApiClient
        {
            return _serviceProvider.GetRequiredService<TClient>();
        }
    }

    public class OpenApiClientFactoryBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        private IServiceProvider? _serviceProvider;
        private OpenApiClientFactory? _clientFactory;

        public OpenApiClientFactoryBuilder()
        {
            _serviceCollection = new ServiceCollection();
        }

        public OpenApiClientFactoryBuilder AddClient<TClientInterface, TClientImplementation, TOptions>(TOptions options,
            Action<HttpClient>? configureClient = null,
            Func<HttpClientHandler>? configureHttpMessageHandler = null)
            where TClientInterface : class, IOpenApiClient
            where TClientImplementation : class, TClientInterface
            where TOptions : OptionsBase
        {
            _serviceCollection.AddOpenApiClient<TClientInterface, TClientImplementation, TOptions>(options, configureClient, configureHttpMessageHandler);
            return this;
        }

        /// <summary>
        /// Enables transparent OAuth client-credentials token acquisition and per-scope caching for
        /// <typeparamref name="TClient"/>. Call after the matching
        /// <see cref="AddClient{TClientInterface,TClientImplementation,TOptions}"/>, then select a
        /// cache on the returned builder with exactly one of
        /// <see cref="OpenApiClientOAuthTokenCachingBuilder{TClient}.UseInMemoryCache"/>,
        /// <see cref="OpenApiClientOAuthTokenCachingBuilder{TClient}.UseRegisteredDistributedCache"/> or
        /// <see cref="OpenApiClientOAuthTokenCachingBuilder{TClient}.UseDistributedCache(Microsoft.Extensions.Caching.Distributed.IDistributedCache)"/>.
        /// There is no implicit fallback: until a cache is selected, resolving the client throws an
        /// error naming the available options.
        /// <para>
        /// <typeparamref name="TClient"/> must be the <em>implementation</em> type of the client -
        /// the same type argument the client passes to <see cref="IHttpHelper{TClient}"/>. Passing
        /// the client interface throws.
        /// </para>
        /// <para>
        /// There is no token refresh: a <c>401 Unauthorized</c> response evicts the cached token and
        /// is returned to the caller unchanged. The request that hit the <c>401</c> is not retried.
        /// </para>
        /// </summary>
        /// <returns>A builder used to select where access tokens are cached.</returns>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="TClient"/> is the interface of a registered client instead of its
        /// implementation type.
        /// </exception>
        public OpenApiClientOAuthTokenCachingBuilder<TClient> AddOAuthTokenCaching<TClient>()
            where TClient : class, IOpenApiClient
        {
            _serviceCollection.AddOAuthTokenCaching<TClient>();
            return new OpenApiClientOAuthTokenCachingBuilder<TClient>(_serviceCollection, this);
        }

        public OpenApiClientFactory Build()
        {
            _serviceProvider ??= _serviceCollection.BuildServiceProvider();
            _clientFactory ??= new OpenApiClientFactory(_serviceProvider);
            OpenApiClientFactory.Instance = _clientFactory;

            return _clientFactory;
        }
    }
}
