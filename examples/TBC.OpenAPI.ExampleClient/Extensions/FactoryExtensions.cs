using TBC.OpenAPI.Core;

namespace TBC.OpenAPI.ExampleClient.Extensions
{
    public static class FactoryExtensions
    {
        public static OpenApiClientFactoryBuilder AddExampleClient(this OpenApiClientFactoryBuilder builder,
            ExampleClientOptions options) => AddExampleClient(builder, options, null, null);

        public static OpenApiClientFactoryBuilder AddExampleClient(this OpenApiClientFactoryBuilder builder,
            ExampleClientOptions options,
            Action<HttpClient>? configureClient = null,
            Func<HttpClientHandler>? configureHttpMessageHandler = null)
        {
            return builder.AddClient<IExampleClient, ExampleClient, ExampleClientOptions>(options, configureClient, configureHttpMessageHandler);
        }

        public static IExampleClient GetExampleClient(this OpenApiClientFactory factory) =>
            factory.GetOpenApiClient<IExampleClient>();

    }
}