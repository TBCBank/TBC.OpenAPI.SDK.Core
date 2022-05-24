# TBC.OpenAPI.SDK.Core  
[![NuGet version (TBC.OpenAPI.SDK.Core)](https://img.shields.io/nuget/v/TBC.OpenAPI.SDK.Core.svg?label=TBC.OpenAPI.SDK.Core)](https://www.nuget.org/packages/TBC.OpenAPI.SDK.Core/) [![CI](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/actions/workflows/main.yml/badge.svg?branch=master)](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/actions/workflows/main.yml)  
Core functionality for TBC Open API SDKs


## CORE functionality for working with Open API SDKs
Repository contains the basic functionality used to work with Open Api SDKs.

Library is written in the C # programming language and is compatible with .netstandard2.0 and .net6.0. Depends only on the components manufactured by Microsoft.


## Consider example of using "ExampleClient" for creating SDK Client 

* Create interface "IExampleClient" and inherit from "TBC.OpenAPI.SDK.Core.IOpenApiClient"
* Create class "ExampleClient" and inherit from "IExampleClient"
* Create property ```private readonly IHttpHelper<ExampleClient> _http``` and assign it from constructor by dephendency injection
```c#
public ExampleClient(HttpHelper<ExampleClient> http)
{
    _http = http;
}
```
* Create class "ExampleClientOptions" and inherit from "TBC.OpenAPI.SDK.Core.OptionsBase"
* Create class "ServiceCollectionExtensions" with extension method "AddExampleClient" for "Microsoft.Extensions.DependencyInjection.IServiceCollection", used for add client to middleware
* Create class "FactoryExtensions" with extension method "AddExampleClient" for "TBC.OpenAPI.SDK.Core.OpenApiClientFactoryBuilder", used for pass options "ExampleClientOptions" into "OpenApiClientFactoryBuilder"

## Examples of projects
Repository contains three [example projects](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/tree/master/examples):

* UsageExample1 - .net Core API Application
* UsageExample2 - Console Application
* UsageExample3 - .net WebApi Application


## Consider example of using "UsageExample1"

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