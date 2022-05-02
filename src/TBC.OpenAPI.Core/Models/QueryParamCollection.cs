using System.Web;

namespace TBC.OpenAPI.Core.Models
{
    public sealed class QueryParamCollection : Dictionary<string, ParamValue>
    {
        public string ToQueryString()
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (var item in this)
            {
                var value = item.Value;
                if (value.Value is not null)
                {
                    query.Add(item.Key, value.ToString());
                }
                else
                if (value.Values is not null)
                {
                    foreach (var v in value.Values)
                    {
                        query.Add(item.Key, v.ToString());
                    }
                }
            }

            return query.ToString() ?? string.Empty;
        }
    }
}
