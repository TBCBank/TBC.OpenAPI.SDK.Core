# TBC.OpenAPI.Core  
[![NuGet version (TBC.OpenAPI.Core)](https://img.shields.io/nuget/v/TBC.OpenAPI.Core.svg?label=TBC.OpenAPI.Core)](https://www.nuget.org/packages/TBC.OpenAPI.Core/) [![CI](https://github.com/TBCBank/TBC.OpenAPI.Core/actions/workflows/main.yml/badge.svg?branch=master)](https://github.com/TBCBank/TBC.OpenAPI.Core/actions/workflows/main.yml)  
Core functionality for TBC Open API SDKs


## HTTP გამოძახებების დამხმარე
რეპოზიტორი [TBC.OpenAPI.Core](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core) შექმნილია HTTP გამოძახებების გამარტივებისთვის.
მისი გამოყენებით აღარ გვიწევს დიდი კოდის წერა HTTP გამოძახებებისას და გვეხმარება დროის დაზოგვაში.
რეპოზიტორი შეიცავს ერთ ძირითად პროექტს და რამდენიმე მაგალითს.

## პროექტი TBCTBC.OpenAPI.Core
ეს არის ბიბლიოთეკა, რომლის დახმარებით მოთხოვნის მარტივად შეგვიძლია შევქმნათ [GET](https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/GET), [PUT](https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/PUT), [POST](https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/POST) გამოძახებები, მივიღოთ და დავამუშაოთ დაბრუნებული პასუხი.

ბიბლიოთეკა დაწერილია C#-ის პროგრამირების ენაზე და თავსებადია .netstandard2.0-სა და .net6.0-თან. დამოკიდებულია მხოლოდ კომპანია Microsoft-ის წარმოებულ კომპონენტებზე.

ბიბლიოთეკა შეიცავს ორ ძირითად კლასს:

* class HttpHelper - რომელშიც რეალიზებულია HTTP გამოძახებები.
* class OpenApiClientFactory - გამოიყენება სერვის პროვაიდერის შესაქმნელად და სამართავად.

## დამხმარე მეთოდები
პროექტი შეიცავს კლასებს დამხმარე მეთოდებით:

* class HeaderParamCollection - შეიცავს მეთოდს ApplyHeaders, გამოიყენება HTTP გამოძახების Header-ების დამატებისთვის.
* class ProblemDetailsJsonConverter - შეიცავს შეცდომის დამუშავების მეთოდებს.
* class QueryParamCollection - შეიცავს მეთოდს ToQueryString, გამოიყენება Query პარამეტრების დამატებისთვის.

## გამოძახების პარამეტრები
გამოძახების პარამეტრები უნდა გადაეცეს OpenApiClientFactoryBuilder-ის მეთოდს AddClient, OptionsBase კლასის საშუალებით.


## მაგალითების პროექტები
რეპოზიტორიში მოცემულია [სამი მაგალითი](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/tree/master/examples):
* UsageExample1 - .net Core API აპლიკაცია
გამოყენების მაგალითი:
```c#
 [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IExampleClient _exampleClient;

        public TestController(IExampleClient exampleClient)
        {
            _exampleClient = exampleClient;
        }

        [HttpGet]
        public async Task<ActionResult<SomeObject>> GetSomeObject(CancellationToken cancellationToken = default)
        {
            var result = await _exampleClient.GetSomeObjectAsync(cancellationToken);
            return Ok(result);
        }
    }
```

* UsageExample2 - კონსოლ აპლიკაცია
```c#
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
```

* UsageExample3 - .net WebApi აპლიკაცია
```c#
public class ValuesController : ApiController
    {
        // GET api/values
        public async Task<IHttpActionResult> Get()
        {
            var exampleClient = OpenApiClientFactory.Instance.GetExampleClient();

            var result = await exampleClient.GetSomeObjectAsync();

            //var result = "ok";


            return Ok(result);
        }
    }
```

## Unit ტესტების პროექტი [TBC.OpenAPI.Core.Tests](https://github.com/TBCBank/TBC.OpenAPI.SDK.Core/tree/master/tests/TBC.OpenAPI.SDK.Core.Tests)
TBC.OpenAPI.Core ბიბლიოთეკის ტესტირებისთვის TBC.OpenAPI.Core.Tests პროექტში წარმოდგენილია Unit ტესტები, რომლებიც უზრუნველყოფს ბიბლიოთეკისა და მასში არსებული მეთოდების მუშაობის ტესტირებას.