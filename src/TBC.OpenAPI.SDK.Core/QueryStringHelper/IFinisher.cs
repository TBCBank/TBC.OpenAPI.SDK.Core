using System;
using System.Collections.Generic;
using System.Text;

namespace TBC.OpenAPI.SDK.Core.QueryStringHelper
{
    internal interface IFinisher
    {
        string GetBuildedQuery();
    }
}
