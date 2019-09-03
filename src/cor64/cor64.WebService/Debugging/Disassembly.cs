using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.WebService.Debugging
{
    [JsonObject]
    public class Disassembly
    {
        [JsonProperty(PropertyName = "lines")]
        public String[] Lines { get; set; }
    }
}
