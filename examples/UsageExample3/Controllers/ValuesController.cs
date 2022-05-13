using System.Threading.Tasks;
using System.Web.Http;
using TBC.OpenAPI.SDK.Core;
using TBC.OpenAPI.SDK.ExampleClient.Extensions;

namespace UsageExample3.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public async Task<IHttpActionResult> Get()
        {
            var exampleClient = OpenApiClientFactory.Instance.GetExampleClient();

            var result = await exampleClient.GetSomeObjectAsync();

            return Ok(result);
        }
    }
}
