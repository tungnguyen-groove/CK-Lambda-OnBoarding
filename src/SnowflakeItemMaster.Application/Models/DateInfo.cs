using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class DateInfo
    {
        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("createdAt")]
        public object CreatedAt { get; set; }

        [JsonPropertyName("lastUpdatedAt")]
        public object LastUpdatedAt { get; set; }
    }
}