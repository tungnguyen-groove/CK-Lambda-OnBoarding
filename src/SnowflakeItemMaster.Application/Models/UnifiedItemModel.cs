using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class UnifiedItemModel
    {
        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("chinaHtsCode")]
        public string? ChinaHtsCode { get; set; }

        [JsonPropertyName("countryOfOriginCode")]
        public string? CountryOfOriginCode { get; set; }

        [JsonPropertyName("gs1Barcode")]
        public string? Gs1Barcode { get; set; }

        [JsonPropertyName("alternateBarcodes")]
        public List<AlternateBarcode>? AlternateBarcodes { get; set; }

        [JsonPropertyName("htsTariffCode")]
        public string? HtsTariffCode { get; set; }

        [JsonPropertyName("hsCommodityCode")]
        public string? HsCommodityCode { get; set; }

        [JsonPropertyName("prices")]
        public List<Price>? Prices { get; set; }

        [JsonPropertyName("costs")]
        public List<Cost>? Costs { get; set; }

        [JsonPropertyName("categories")]
        public List<Category>? Categories { get; set; }

        [JsonPropertyName("attributes")]
        public List<Attribute>? Attributes { get; set; }

        [JsonPropertyName("links")]
        public List<Link>? Links { get; set; }

        [JsonPropertyName("images")]
        public List<Image>? Images { get; set; }

        [JsonPropertyName("dates")]
        public List<DateInfo>? Dates { get; set; }
    }
}