using System.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using TBC.OpenAPI.SDK.Core;
using TBC.OpenAPI.SDK.ExampleClient;
using TBC.OpenAPI.SDK.ExampleClient.Extensions;

namespace UsageExample3
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            new OpenApiClientFactoryBuilder()
                .AddExampleClient(new ExampleClientOptions
                {
                    BaseUrl = ConfigurationManager.AppSettings["ExampleClientUrl"],
                    ApiKey = ConfigurationManager.AppSettings["ExampleClientKey"]
                })
                .Build();
        }
    }
}
