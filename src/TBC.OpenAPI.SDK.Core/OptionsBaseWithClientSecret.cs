using System;
using System.Collections.Generic;
using System.Text;

namespace TBC.OpenAPI.SDK.Core
{
    public abstract class OptionsBaseWithClientSecret : OptionsBase
    {
        public string ClientSecret { get; set; } = null!;
    }
}
