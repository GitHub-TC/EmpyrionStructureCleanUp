using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EmpyrionStructureCleanUp
{

    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; } = LogLevel.Message;
        public int OnlyCleanIfOlderThan { get; set; } = 14;
        public string MoveToDirectory { get; set; } = @"..\..\..\..\CleanUp";
        public bool DeletePermanent { get; set; } = false;
        public bool CleanOnStartUp { get; set; } = false;
        public string ChatCommandPrefix { get; set; } = "\\";
    }
}
