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
* If you need client secret in options, inherit from "TBC.OpenAPI.SDK.Core.OptionsBaseWithClientSecret"

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
    "BaseUrl": "https://run.mocky.io/v3/7690b5f0-cc43-4c03-b07f-2240b4448931/",
    "ApiKey": "abc"
  } 
}
```
* In case you need client secret

Program.cs
```c#
builder.Services.AddExampleClient(builder.Configuration.GetSection("ExampleClient").Get<OptionsBaseWithClientSecret>());
```
appsettings.json
```json
{
  "ExampleClient": {
    "BaseUrl": "https://run.mocky.io/v3/7690b5f0-cc43-4c03-b07f-2240b4448931/",
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
  "name": "one"
}
```