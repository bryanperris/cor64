using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace cor64.WebService
{
    [JsonObject]
    public abstract class ResponseAction
    {
        [JsonProperty(PropertyName = "type")]
        public abstract string Type { get; }
    }

}
