using System;
using Newtonsoft.Json;

namespace RunN64
{
    [JsonObject]
    public class Config
    {
        [JsonProperty(PropertyName = "romFile")]
        public String RomFilepath { get; set; }

        [JsonProperty(PropertyName = "elfFile")]
        public String ElfFilepath { get; set; }
        
        [JsonProperty(PropertyName = "useInterpreter")]
        public bool UseInterpreter {get; set; }
    }
}
