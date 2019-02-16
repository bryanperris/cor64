using System;
using Newtonsoft.Json;

namespace RunN64
{
    [JsonObject]
    public class Config
    {
        [JsonProperty(PropertyName = "romFile")]
        public String RomFilepath { get; set; }
    }
}
