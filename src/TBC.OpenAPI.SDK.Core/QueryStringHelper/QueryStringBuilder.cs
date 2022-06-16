using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;

namespace TBC.OpenAPI.SDK.Core.QueryStringHelper
{
    internal class QueryStringBuilder:IAddParameterable, IFinisher
    {
        protected StringBuilder sb = new StringBuilder();
        private QueryStringBuilder(string startingString)
        {
            sb.Append(startingString);
        }
        public static IAddParameterable StartBuild(string rootaddress = null)
        {
            return new QueryStringBuilder(rootaddress == null?"?":$"{rootaddress}?");
        }
        public IAddParameterable AddParameter(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(name))
            {
                sb.Append($"{UrlEncoder.Default.Encode(name)}={UrlEncoder.Default.Encode(value)}&");
            }
            
            return this;
        }
        public IFinisher Finish()
        {
            if(sb[sb.Length - 1] == '&' || sb[sb.Length - 1] == '?')
            {
                sb.Remove(sb.Length - 1, 1);
            }
            return this;
        }
        public string GetBuildedQuery()
        {
            return sb.ToString();
        }
    }
}
