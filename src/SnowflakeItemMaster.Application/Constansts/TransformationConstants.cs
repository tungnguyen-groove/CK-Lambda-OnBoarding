namespace SnowflakeItemMaster.Application.Constansts
{
    public static class TransformationConstants
    {
        public static class Price
        {
            public const string DefaultCurrency = "USD";
            public const string ListType = "list";
            public const string UnitType = "unit";
            public const string LandedType = "landed";
        }

        public static class Category
        {
            public const string BrandSource = "brand";
            public const string AkaSource = "aka";
        }

        public static class Attribute
        {
            public const string SizeId = "size";
            public const string ColorId = "color";
            public const string BrandNameId = "brand_name";
            public const string FabricContentId = "fabric_content";
            public const string FabricCompositionId = "fabric_composition";
            public const string GenderId = "gender";
            public const string VelocityCodeId = "velocity_code";
            public const string FastMoverId = "fast_mover";
            public const string BrandEntityId = "brand_entity";
            public const string InventorySyncEnabledId = "inventory_sync_enabled";
        }

        public static class Link
        {
            public const string ShopifyUsSource = "Shopify US";
        }

        public static class Image
        {
            public const string OriginalSizeType = "original_size";
        }

        public static class DateSystem
        {
            public const string Shopify = "Shopify";
            public const string Snowflake = "snowflake";
        }

        public static class HtsCode
        {
            public const int TariffCodeLength = 10;
            public const int CommodityCodeLength = 6;
        }

        public static class Barcode
        {
            public const string AlternateBarcodeType = "alternate";
        }
    }
}