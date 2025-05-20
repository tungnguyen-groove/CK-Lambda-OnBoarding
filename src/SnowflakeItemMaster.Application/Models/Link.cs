using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class Link
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}