using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.WebService.Debugging
{
    [JsonObject]
    public class UpdateDisassemblyAction : ResponseAction
    {
        [JsonProperty(PropertyName = "disassembly")]
        public Disassembly Disassembly { get; set; }

        public override string Type => "RECV_UPDATE_DISASSEMBLY";
    }
}
