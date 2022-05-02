using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBC.OpenAPI.Core.Tests.Models
{
    public class HttpTestRequestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime Date { get; set; }
        public List<int>? Numbers { get; set; }
    }
}
