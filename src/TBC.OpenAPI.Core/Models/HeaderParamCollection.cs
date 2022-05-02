using System.Net.Http.Headers;

namespace TBC.OpenAPI.Core.Models
{
    public class HeaderParamCollection : Dictionary<string, ParamValue>
    {
        public void ApplyHeaders(HttpRequestHeaders headers)
        {
            foreach (var item in this)
            {
                if (headers.Contains(item.Key))
                    headers.Remove(item.Key);

                var value = item.Value;
                if (value.Value is not null)
                {
                    headers.Add(item.Key, value.ToString());
                }
                else
                if (value.Values is not null)
                {
                    headers.Add(item.Key, value.Values.Select(x => x.ToString()));
                }
            }
        }
    }
}
