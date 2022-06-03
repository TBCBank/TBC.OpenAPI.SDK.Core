using System;
using System.Collections.Generic;
using System.Text;

namespace TBC.OpenAPI.SDK.Core.QueryStringHelper
{
    internal interface IAddParameterable
    {

        IAddParameterable AddParameter(string name, string value);
        IFinisher Finish();

    }
}
