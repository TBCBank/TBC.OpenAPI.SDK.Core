# TBC.OpenAPI.SDK.Core  
[![NuGet version (TBC.OpenAPI.SDK.Core)](https://img.shields.io/nuget/v/TBC.OpenAPI.SDK.Core.svg?label=TBC.OpenAPI.SDK.Core)](https://www.nuget.org/packages/TBC.OpenAPI.SDK.Core/) [![CI](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/actions/workflows/main.yml/badge.svg?branch=master)](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/actions/workflows/main.yml)  
Core functionality for TBC Open API SDKs


## CORE functionality for working with Open API SDKs
Repository contains the basic functionality used to work with Open Api SDKs.

Library is written in the C # programming language and is compatible with .netstandard2.0 and .net6.0. Depends only on the components manufactured by Microsoft.


## Example of using "ExampleClient" for creating SDK Client 

> [!NOTE]
> The `Example*` types are **not part of this package**; they ship in the separate
> [`TBC.OpenAPI.SDK.ExampleClient`](https://www.nuget.org/packages/TBC.OpenAPI.SDK.ExampleClient/)
> reference package as a template for your own client. Core provides the primitives:
> `AddOpenApiClient<TInterface, TImplementation, TOptions>` for DI, and
> `OpenApiClientFactoryBuilder.AddClient<...>` / `OpenApiClientFactory.GetOpenApiClient<TInterface>()`
> for the factory. `AddExampleClient` is a thin wrapper you write yourself.

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

    public ExampleClient(IHttpHelper<ExampleClient> http)
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
> [!IMPORTANT]
> Inject the `IHttpHelper<T>` abstraction (only the interface is registered) and
> parameterize it with the **implementation** type — `IHttpHelper<ExampleClient>`, not
> `IHttpHelper<IExampleClient>`. That is the named `HttpClient` `AddOpenApiClient`
> configures, and the same type argument `AddOAuthTokenCaching<T>` expects.

* Create class "ExampleClientOptions" and inherit from "TBC.OpenAPI.SDK.Core.OptionsBase"
* If you need client secret in options, inherit from "TBC.OpenAPI.SDK.Core.BasicAuthOptions"

```c#
public class ExampleClientOptions : OptionsBase{}

// Both base classes are abstract, so a client secret needs its own concrete class:
public class ExampleClientBasicAuthOptions : BasicAuthOptions{}
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
builder.Services.AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<ExampleClientBasicAuthOptions>());
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

Core can transparently acquire and cache OAuth access tokens using the
`client_credentials` grant. Tokens are requested on demand, cached per scope in an
`IDistributedCache` and attached as an `Authorization: Bearer` header by an `HttpClient`
message handler (`OAuthDelegatingHandler<TClient>`), so client code never fetches or
stores tokens itself.

> [!IMPORTANT]
> There is **no token refresh**. Tokens are acquired lazily and renewed only when they
> expire out of the cache or are evicted. See
> [What happens on 401 Unauthorized](#what-happens-on-401-unauthorized).

### Enable token caching

Call `AddOAuthTokenCaching<TClient>` **after** registering the client with
`AddOpenApiClient`, then finish the chain with exactly one of the terminal methods below
— the cache choice is not optional and is made per client. The SDK never registers a
cache backend on your behalf; if the terminal call is missing, resolving the client
throws an `InvalidOperationException` naming the available options instead of silently
falling back to a per-process cache.

> [!IMPORTANT]
> `TClient` must be the client's **implementation** type (`ExampleClient`), the same type
> argument the client passes to `IHttpHelper<T>`. Passing an interface throws an
> `InvalidOperationException` at registration time.

| Terminal call | Where tokens live | Use when |
| --- | --- | --- |
| `.UseInMemoryCache()` | A private in-memory cache owned by this SDK, per process | Single-instance apps, local development, tests |
| `.UseRegisteredDistributedCache()` | The `IDistributedCache` registered in your container | You already configure Redis / SQL Server / etc. centrally |
| `.UseDistributedCache(cache)`<br>`.UseDistributedCache(sp => ...)` | A cache instance you supply directly | You want a dedicated cache for tokens, separate from the app's |

```c#
builder.Services
    .AddOpenApiClient<IExampleClient, ExampleClient, ExampleClientOptions>(
        builder.Configuration.GetSection("ExampleClient").Get<ExampleClientOptions>())
    .AddOAuthTokenCaching<ExampleClient>()
        .UseInMemoryCache();

// reuse the IDistributedCache registered in the container (Redis, SQL Server, ...)
builder.Services
    .AddOpenApiClient<IExampleClient, ExampleClient, ExampleClientOptions>(options)
    .AddOAuthTokenCaching<ExampleClient>()
        .UseRegisteredDistributedCache();

// or hand over an instance directly, without registering it
builder.Services
    .AddOpenApiClient<IExampleClient, ExampleClient, ExampleClientOptions>(options)
    .AddOAuthTokenCaching<ExampleClient>()
        .UseDistributedCache(sp => sp.GetRequiredKeyedService<IDistributedCache>("tokens"));
```

> [!WARNING]
> `.UseInMemoryCache()` is **per process and not shared**: in a multi-instance deployment
> every instance keeps its own token cache. Pick a distributed option if that is not
> acceptable.

Registration order does not matter — `.UseRegisteredDistributedCache()` resolves the
cache lazily, the first time a token is needed. If nothing is registered by then, or if
the registered implementation is the non-shared `MemoryDistributedCache` (what
`AddDistributedMemoryCache()` registers), it throws; use `.UseInMemoryCache()` if a
per-process cache is what you want.

The same terminal methods are available on `OpenApiClientFactoryBuilder` and return the
builder so the chain continues:

```c#
var factory = new OpenApiClientFactoryBuilder()
    .AddClient<IExampleClient, ExampleClient, ExampleClientOptions>(options)
    .AddOAuthTokenCaching<ExampleClient>()
        .UseInMemoryCache()
    .Build();

var client = factory.GetOpenApiClient<IExampleClient>();
```

### Migrating from 3.x

`AddOAuthTokenCaching<TClient>()` used to fall back to an in-memory distributed cache
when no `IDistributedCache` was registered, which made the caching topology depend on
registration order. A terminal call is now required, and `TClient` must be the
implementation type — `AddOAuthTokenCaching<IExampleClient>()` never worked (the handler
was attached to a named `HttpClient` nothing resolved, so requests went out without an
`Authorization` header) and now throws instead of failing silently.

```diff
  builder.Services
      .AddOpenApiClient<IExampleClient, ExampleClient, ExampleClientOptions>(options)
-     .AddOAuthTokenCaching<IExampleClient>();
+     .AddOAuthTokenCaching<ExampleClient>()
+         .UseInMemoryCache();                // previous behaviour without a registered IDistributedCache
+     //  .UseRegisteredDistributedCache();   // previous behaviour with a registered IDistributedCache
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
   request obtains a fresh one. The `401` itself is returned to the caller.

### What happens on 401 Unauthorized

The handler **evicts, it does not refresh**: the failed request is not retried, only the
*next* request for that scope gets a fresh token. The SDK never retries with a new token,
never uses the `refresh_token` grant (no refresh token is stored) and never renews
proactively. The only protection against sending an about-to-expire token is the
30-second grace period subtracted from `expires_in`, so treat a `401` as a normal failed
response — retry and backoff are the caller's responsibility.

### Cache behavior

* **Cache key** — composed as `TbcOpenApiOAuthToken:{ClientTypeName}:{scope}`, so tokens
  are isolated per client type and per scope.
* **Lifetime** — a token is cached for its `expires_in` value minus a 30-second grace
  period (`OAuthConstants.TokenTimeoutGracePeriodSec`), with a minimum of 30 seconds
  (`OAuthConstants.MinCacheTtlSec`). Tokens without an `expires_in` value use the minimum.
* **Stored value** — only the access-token string is persisted. A cache hit therefore
  yields a token response whose `TokenType`, `ExpiresIn` and `Scope` are null.
* **Concurrency** — concurrent requests for the same scope are de-duplicated so only a
  single token request is made while the others await the same result. This de-duplication
  is **per process**: in a multi-instance deployment each instance still issues its own
  initial token request per scope, after which the shared cache takes over.