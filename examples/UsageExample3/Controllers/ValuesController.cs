using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TBC.OpenAPI.Core;
using TBC.OpenAPI.ExampleClient;
using TBC.OpenAPI.ExampleClient.Extensions;

namespace UsageExample3.Controllers
{
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
}
