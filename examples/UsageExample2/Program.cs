// See https://aka.ms/new-console-template for more information

using TBC.OpenAPI.SDK.Core;
using TBC.OpenAPI.SDK.ExampleClient;
using TBC.OpenAPI.SDK.ExampleClient.Extensions;

var factory = new OpenApiClientFactoryBuilder()
    .AddExampleClient(new ExampleClientOptions
    {
        BaseUrl = "https://run.mocky.io/v3/7690b5f0-cc43-4c03-b07f-2240b4448931/",
        ApiKey = "abc"
    })
    .Build();


var client = factory.GetExampleClient();

var result = client.GetSomeObjectAsync().GetAwaiter().GetResult();

Console.WriteLine($"Result: {result.Name}");

Console.ReadLine();