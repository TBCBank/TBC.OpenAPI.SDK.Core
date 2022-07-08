using Microsoft.AspNetCore.Mvc;
using TBC.OpenAPI.SDK.ExampleClient;
using TBC.OpenAPI.SDK.ExampleClient.Models;

namespace UsageExample4.Controllers
{
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
}