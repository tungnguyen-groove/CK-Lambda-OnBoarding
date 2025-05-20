using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class Attribute
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }
}