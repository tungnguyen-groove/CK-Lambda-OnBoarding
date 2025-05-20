namespace SnowflakeItemMaster.Provider.Snowflake.Constants
{
    public static class SqlConstants
    {
        private const string ItemPetalTable = "SANDBOX_US_DB.XB_DEV_DB.ITEM_PETAL_US";

        private const string ItemPetalSelectColumns = @"
                BRAND AS Brand,
                REGION AS Region,
                SKU AS Sku,
                STATUS AS Status,
                BARCODE AS Barcode,
                SECONDARY_BARCODE AS SecondaryBarcode,
                PRODUCT_TITLE AS ProductTitle,
                COLOR AS Color,
                SIZE AS Size,
                WEIGHT AS Weight,
                VOLUME AS Volume,
                HEIGHT AS Height,
                WIDTH AS Width,
                LENGTH AS Length,
                PRODUCT_TYPE AS ProductType,
                CATEGORY AS Category,
                GENDER AS Gender,
                FABRIC_CONTENT AS FabricContent,
                FABRIC_COMPOSITION AS FabricComposition,
                COUNTRY_OF_ORIGIN AS CountryOfOrigin,
                HTS AS Hts,
                CHINA_HTS AS ChinaHts,
                VELOCITY_CODE AS VelocityCode,
                FAST_MOVER AS FastMover,
                DESCRIPTION AS Description,
                PRODUCT_IMAGE_URL AS ProductImageUrl,
                PRODUCT_IMAGE_URL_POS_1 AS ProductImageUrlPos1,
                PRODUCT_IMAGE_URL_POS_2 AS ProductImageUrlPos2,
                PRODUCT_IMAGE_URL_POS_3 AS ProductImageUrlPos3,
                LANDED_COST AS LandedCost,
                COST AS Cost,
                PRICE AS Price,
                LATEST_PO_NUMBER AS LatestPoNumber,
                LATEST_PO_STATUS AS LatestPoStatus,
                LATEST_PO_CREATED_DATE AS LatestPoCreatedDate,
                LATEST_PO_EXPECTED_DATE AS LatestPoExpectedDate,
                WH_1_NAME AS Wh1Name,
                WH_1_AVAILABLE_QTY AS Wh1AvailableQty,
                WH_2_NAME AS Wh2Name,
                WH_2_AVAILABLE_QTY AS Wh2AvailableQty,
                WH_3_NAME AS Wh3Name,
                WH_3_AVAILABLE_QTY AS Wh3AvailableQty,
                CREATED_AT_SHOPIFY AS CreatedAtShopify,
                CREATED_AT_SNOWFLAKE AS CreatedAtSnowflake,
                UPDATED_AT_SNOWFLAKE AS UpdatedAtSnowflake,
                PRESENT_IN_XB_FLAG AS PresentInXbFlag,
                INVENTORY_SYNC_FLAG AS InventorySyncFlag,
                THIRD_BARCODE AS ThirdBarcode";

        public static readonly string GetItemsBySkusQuery = $@"
                SELECT
                {ItemPetalSelectColumns}
                FROM {ItemPetalTable}
                WHERE SKU IN ({{0}})";

        public static readonly string GetItemsWithLimitQuery = $@"
                SELECT
                {ItemPetalSelectColumns}
                FROM {ItemPetalTable}
                WHERE UPDATED_AT_SNOWFLAKE >= :fromDate
                  AND UPDATED_AT_SNOWFLAKE < :toDate
                ORDER BY UPDATED_AT_SNOWFLAKE DESC
                LIMIT :limit";
    }
}