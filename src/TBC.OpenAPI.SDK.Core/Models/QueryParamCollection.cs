using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Web;
using System.Text;
using TBC.OpenAPI.SDK.Core.QueryStringHelper;

namespace TBC.OpenAPI.SDK.Core.Models
{
    public sealed class QueryParamCollection : Dictionary<string, ParamValue>
    { 
        public string ToQueryString()
        {
            var queryStringBulder = QueryStringBuilder.StartBuild();
            foreach (var item in this)
            {
                if (item.Value?.Value is not null)
                {
                    queryStringBulder.AddParameter(item.Key, item.Value.Value);
                }
                else if (item.Value?.Values is not null)
                {
                    foreach (var v in item.Value.Values)
                    {
                        queryStringBulder.AddParameter(item.Key, v.ToString());
                    }
                }
            }
            return queryStringBulder.Finish().GetBuildedQuery();
        }
    }
}
