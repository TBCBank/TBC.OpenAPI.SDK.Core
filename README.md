# TBC.OpenAPI.SDK.Core  
[![NuGet version (TBC.OpenAPI.SDK.Core)](https://img.shields.io/nuget/v/TBC.OpenAPI.SDK.Core.svg?label=TBC.OpenAPI.SDK.Core)](https://www.nuget.org/packages/TBC.OpenAPI.SDK.Core/) [![CI](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/actions/workflows/main.yml/badge.svg?branch=master)](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/actions/workflows/main.yml)  
Core functionality for TBC Open API SDKs


## CORE functionality for working with Open API SDKs
Repository contains the basic functionality used to work with Open Api SDKs.

Library is written in the C # programming language and is compatible with .netstandard2.0 and .net6.0. Depends only on the components manufactured by Microsoft.


## Example of using "ExampleClient" for creating SDK Client 

* Create interface "IExampleClient" and inherit from "TBC.OpenAPI.SDK.Core.IOpenApiClient"
```c#
public interface IExampleClient : IOpenApiClient
{
    Task<SomeObject> GetSomeObjectAsync(CancellationToken cancellationToken = default);
}
```
* Create class "ExampleClient" and inherit from "IExampleClient"
```c#
public class ExampleClient : IExampleClient
{
    private readonly IHttpHelper<ExampleClient> _http;

    public ExampleClient(HttpHelper<ExampleClient> http)
    {
        _http = http;
    }

    public async Task<SomeObject> GetSomeObjectAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetJsonAsync<SomeObject>("/", cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
            throw new OpenApiException(result.Problem?.Title ?? "Unexpected error occurred", result.Exception);

        return result.Data!;
    }
}
```
* Create property ```private readonly IHttpHelper<ExampleClient> _http``` and assign it from the constructor by dependency injection
```c#
public ExampleClient(HttpHelper<ExampleClient> http)
{
    _http = http;
}
```
* Create class "ExampleClientOptions" and inherit from "TBC.OpenAPI.SDK.Core.OptionsBase"
* If you need client secret in options, inherit from "TBC.OpenAPI.SDK.Core.BasicAuthOptions"

```c#
public class ExampleClientOptions : OptionsBase{}
```
* Create class "ServiceCollectionExtensions" with extension method "AddExampleClient" for "Microsoft.Extensions.DependencyInjection.IServiceCollection", used for adding client to middleware
```c#
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
```
* Create class "FactoryExtensions" with extension method "AddExampleClient" for "TBC.OpenAPI.SDK.Core.OpenApiClientFactoryBuilder", used for passing options "ExampleClientOptions" into "OpenApiClientFactoryBuilder"
```c#
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
```
## Examples of projects
Repository contains three [example projects](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/tree/master/examples):

* UsageExample1 - .net Core API Application
* UsageExample2 - Console Application
* UsageExample3 - .net WebApi Application


## Example of using "UsageExample1"

### Add "AddExampleClient" to Program.cs file with Dependency Injection and read settings for "ExampleClientOptions" from appsettings.json file

Program.cs
```c#
builder.Services.AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<ExampleClientOptions>());
```
appsettings.json
```json
{
  "ExampleClient": {
    "BaseUrl": "https://jsonplaceholder.typicode.com/users/1",
    "ApiKey": "abc"
  } 
}
```
* In case you need client secret

Program.cs
```c#
builder.Services.AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<BasicAuthOptions>());
```
appsettings.json
```json
{
  "ExampleClient": {
    "BaseUrl": "https://jsonplaceholder.typicode.com/users/1",
    "ApiKey": "abc",
    "ClientSecret": "abc"
  } 
}
```

#### Create variable "_exampleClient" of type "IExampleClient" in controller and initialize it using dependency injection
```c#
private readonly IExampleClient _exampleClient;

public TestController(IExampleClient exampleClient)
{
    _exampleClient = exampleClient;
}
```

#### Call "TestController" method "GetSomeObject"
```c#
[HttpGet]
public async Task<ActionResult<SomeObject>> GetSomeObject(CancellationToken cancellationToken = default)
{
    var result = await _exampleClient.GetSomeObjectAsync(cancellationToken);
    return Ok(result);
}
```

#### Returned Response
```json
{
  "id": 1,
  "name": "Leanne Graham"
}
```

## Token Caching (OAuth client credentials)

The Core library can transparently acquire and cache OAuth access tokens using the
`client_credentials` grant. Once enabled, tokens are requested on demand, cached per
scope in an `IDistributedCache`, attached to outgoing requests as an
`Authorization: Bearer` header, and automatically refreshed when the server responds
with `401 Unauthorized`.

The whole flow is handled by an `HttpClient` message handler
(`OAuthDelegatingHandler<TClient>`), so your client code does not have to fetch, store,
or renew tokens itself.

### Enable token caching

Call `AddOAuthTokenCaching<TClient>` **after** registering the client with
`AddOpenApiClient` / `AddExampleClient`. Register it once per client.

Program.cs
```c#
builder.Services
    .AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<ExampleClientOptions>())
    .AddOAuthTokenCaching<IExampleClient>();
```

If no `IDistributedCache` is registered, an in-memory distributed cache is registered
automatically as a fallback. To use a shared cache (e.g. Redis or SQL Server), register
it **before** calling `AddOAuthTokenCaching` and it will be used instead:

```c#
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

builder.Services
    .AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<ExampleClientOptions>())
    .AddOAuthTokenCaching<IExampleClient>();
```

When you build clients through `OpenApiClientFactoryBuilder`, an equivalent builder
method is available:

```c#
var factory = new OpenApiClientFactoryBuilder()
    .AddExampleClient(options)
    .AddOAuthTokenCaching<IExampleClient>()
    .Build();
```

### Sending a scoped request

The scope is supplied per request through the `X-TBC-OAuth-Scope` marker header
(`OAuthConstants.ScopeHeaderName`). The handler reads and removes that header, resolves a
token for the scope, and injects the `Authorization: Bearer <token>` header. Requests
that do not carry the marker header are passed through untouched (for example the token
endpoint call itself), which prevents recursion.

```c#
public async Task<SomeObject> GetSomeObjectAsync(CancellationToken cancellationToken = default)
{
    var headers = new HeaderParamCollection
    {
        [OAuthConstants.ScopeHeaderName] = "read:some-object"
    };

    var result = await _http.GetJsonAsync<SomeObject>("/", query: null, headers, cancellationToken).ConfigureAwait(false);

    if (!result.IsSuccess)
        throw new OpenApiException(result.Problem?.Title ?? "Unexpected error occurred", result.Exception);

    return result.Data!;
}
```

### How it works

1. The outgoing request carries the `X-TBC-OAuth-Scope` header with the required scope.
2. `OAuthDelegatingHandler<TClient>` extracts and removes that header.
3. A cached token for the scope is looked up in the `IDistributedCache`. If none exists,
   a new token is requested via `POST oauth/token` using `grant_type=client_credentials`
   and the given scope, then cached.
4. The `Authorization: Bearer <access_token>` header is added and the request is sent.
5. If the response is `401 Unauthorized`, the cached token is invalidated so the next
   request obtains a fresh one.

### Cache behavior

* **Cache key** — composed as `TbcOpenApiOAuthToken:{ClientTypeName}:{scope}`, so tokens
  are isolated per client type and per scope.
* **Lifetime** — a token is cached for its `expires_in` value minus a 30-second grace
  period (`OAuthConstants.TokenTimeoutGracePeriodSec`), with a minimum of 30 seconds
  (`OAuthConstants.MinCacheTtlSec`). Tokens without an `expires_in` value use the minimum.
* **Concurrency** — concurrent requests for the same scope are de-duplicated so only a
  single token request is made while the others await the same result.