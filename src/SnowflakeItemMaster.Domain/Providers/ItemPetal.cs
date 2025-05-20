namespace SnowflakeItemMaster.Domain.Providers
{
    public class ItemPetal
    {
        public string Brand { get; set; }

        public string Region { get; set; }

        public string Sku { get; set; }

        public string Status { get; set; }

        public string Barcode { get; set; }

        public string SecondaryBarcode { get; set; }

        public string ProductTitle { get; set; }

        public string Color { get; set; }

        public string Size { get; set; }

        public double? Weight { get; set; }

        public double? Volume { get; set; }

        public double? Height { get; set; }

        public double? Width { get; set; }

        public double? Length { get; set; }

        public string ProductType { get; set; }

        public string Category { get; set; }

        public string Gender { get; set; }

        public string FabricContent { get; set; }

        public string FabricComposition { get; set; }

        public string CountryOfOrigin { get; set; }

        public string Hts { get; set; }

        public string ChinaHts { get; set; }

        public string VelocityCode { get; set; }

        public string FastMover { get; set; }

        public string Description { get; set; }

        public string ProductImageUrl { get; set; }

        public string ProductImageUrlPos1 { get; set; }

        public string ProductImageUrlPos2 { get; set; }

        public string ProductImageUrlPos3 { get; set; }

        public double? LandedCost { get; set; }

        public double? Cost { get; set; }

        public double? Price { get; set; }

        public string LatestPoNumber { get; set; }

        public string LatestPoStatus { get; set; }

        public DateTimeOffset? LatestPoCreatedDate { get; set; }

        public DateTimeOffset? LatestPoExpectedDate { get; set; }

        public string Wh1Name { get; set; }

        public long? Wh1AvailableQty { get; set; }

        public string Wh2Name { get; set; }

        public long? Wh2AvailableQty { get; set; }

        public string Wh3Name { get; set; }

        public long? Wh3AvailableQty { get; set; }

        public DateTimeOffset? CreatedAtShopify { get; set; }

        public DateTimeOffset? CreatedAtSnowflake { get; set; }

        public DateTimeOffset? UpdatedAtSnowflake { get; set; }

        public bool? PresentInXbFlag { get; set; }

        public string InventorySyncFlag { get; set; }

        public string ThirdBarcode { get; set; }
    }
}