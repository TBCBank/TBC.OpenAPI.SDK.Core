using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBC.OpenAPI.Core.Tests
{
    public interface ITestClient : IOpenApiClient
    {

    }

    public class TestClient : ITestClient
    {
    }
}
