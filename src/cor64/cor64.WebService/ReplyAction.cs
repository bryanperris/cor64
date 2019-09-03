using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.WebService
{
    [JsonObject]
    public class ReplyAction
    {
        [JsonProperty(PropertyName = "type")]
        public String Type { get; set; }
    }
}
