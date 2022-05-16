using System.Collections.Specialized;
using System.Net;

namespace TBC.OpenAPI.SDK.Core.Models
{
    public sealed class QueryParamCollection : Dictionary<string, ParamValue>
    {
        public string ToQueryString()
        {
             var query = new NameValueCollection();

            foreach (var item in this)
            {
                var value = item.Value;
                if (value?.Value is not null)
                {
                    query.Add(item.Key, value.ToString());
                }
                else
                if (value?.Values is not null)
                {
                    foreach (var v in value.Values)
                    {
                        query.Add(item.Key, v.ToString());
                    }
                }
            }

            if (!query.HasKeys())
                return string.Empty;

            var segments = query.AllKeys.SelectMany(key => query.GetValues(key),
                (key, value) => $"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(value)}");

            return "?" + string.Join("&", segments);
        }
    }
}
