using Microsoft.Extensions.DependencyInjection;
using TBC.OpenAPI.SDK.Core.Extensions;

namespace TBC.OpenAPI.SDK.ExampleClient.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExampleClient(this IServiceCollection services, ExampleClientOptions options) 
            => AddExampleClient(services, options, null, null);

        public static IServiceCollection AddExampleClient(this IServiceCollection services, ExampleClientOptions options,
            Action<HttpClient>? configureClient = null,
            Func<HttpClientHandler>? configureHttpMessageHandler = null)
        {
            services.AddOpenApiClient<IExampleClient, ExampleClient, ExampleClientOptions>(options, configureClient, configureHttpMessageHandler);
            return services;
        }
    }
}
