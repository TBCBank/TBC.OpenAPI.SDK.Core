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
`AddOpenApiClient` / `AddExampleClient`, then **choose where tokens are cached**. The
choice is not optional: `AddOAuthTokenCaching<TClient>` returns a builder and you must
finish the chain with exactly one of the three terminal methods below.

The SDK never registers a cache backend on your behalf and never touches your
`IDistributedCache` registration. If you forget the terminal call, resolving the client
throws an `InvalidOperationException` that names the available options — it will not
silently fall back to a per-process cache.

| Terminal call | Where tokens live | Use when |
| --- | --- | --- |
| `.UseInMemoryCache()` | A private in-memory cache owned by this SDK, per process | Single-instance apps, local development, tests |
| `.UseRegisteredDistributedCache()` | The `IDistributedCache` registered in your container | You already configure Redis / SQL Server / etc. centrally |
| `.UseDistributedCache(cache)`<br>`.UseDistributedCache(sp => ...)` | A cache instance you supply directly | You want a dedicated cache for tokens, separate from the app's |

Program.cs
```c#
builder.Services
    .AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<ExampleClientOptions>())
    .AddOAuthTokenCaching<IExampleClient>()
        .UseInMemoryCache();
```

> [!WARNING]
> `.UseInMemoryCache()` is **per process and not shared**. In a multi-instance
> deployment every instance keeps its own token cache and requests its own tokens. Pick
> a distributed option if that is not acceptable.

To reuse the `IDistributedCache` already registered in the container:

```c#
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

builder.Services
    .AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<ExampleClientOptions>())
    .AddOAuthTokenCaching<IExampleClient>()
        .UseRegisteredDistributedCache();
```

Registration order no longer matters — the cache is resolved lazily, the first time a
token is needed. If nothing is registered at that point, or if the registered
implementation is `MemoryDistributedCache` (which `AddDistributedMemoryCache()`
registers and which is *not* shared across instances), the call throws. That is
deliberate: if you want a per-process cache, say so explicitly with
`.UseInMemoryCache()`.

To hand the SDK a cache instance directly, without registering it in the container:

```c#
builder.Services
    .AddExampleClient(options)
    .AddOAuthTokenCaching<IExampleClient>()
        .UseDistributedCache(myTokenCache);

// or resolved lazily from the container
builder.Services
    .AddExampleClient(options)
    .AddOAuthTokenCaching<IExampleClient>()
        .UseDistributedCache(sp => sp.GetRequiredKeyedService<IDistributedCache>("tokens"));
```

The cache choice is made **per client**, so different clients can use different
backends.

When you build clients through `OpenApiClientFactoryBuilder`, the same terminal methods
are available and return the factory builder so the chain continues:

```c#
var factory = new OpenApiClientFactoryBuilder()
    .AddExampleClient(options)
    .AddOAuthTokenCaching<IExampleClient>()
        .UseInMemoryCache()
    .Build();
```

### Migrating from 3.x

`AddOAuthTokenCaching<TClient>()` used to register an in-memory distributed cache as a
fallback when no `IDistributedCache` was present. That made the caching topology depend
on registration order and could silently give multi-instance deployments non-shared
caches. It no longer does anything of the sort.

Existing code stops compiling until a terminal call is added — the fix is mechanical:

```diff
  builder.Services
      .AddExampleClient(options)
-     .AddOAuthTokenCaching<IExampleClient>();
+     .AddOAuthTokenCaching<IExampleClient>()
+         .UseInMemoryCache();          // previous behaviour without a registered IDistributedCache
```

```diff
  builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
  builder.Services
      .AddExampleClient(options)
-     .AddOAuthTokenCaching<IExampleClient>();
+     .AddOAuthTokenCaching<IExampleClient>()
+         .UseRegisteredDistributedCache();   // previous behaviour with a registered IDistributedCache
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