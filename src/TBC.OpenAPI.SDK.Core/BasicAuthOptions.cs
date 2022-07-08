using System;
using System.Collections.Generic;
using System.Text;

namespace TBC.OpenAPI.SDK.Core
{
    public abstract class BasicAuthOptions : OptionsBase
    {
        public string ClientSecret { get; set; } = null!;
    }
}
