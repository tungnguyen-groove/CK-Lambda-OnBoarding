using System.Text.Json;
using Snowflake.Data.Client;

namespace SnowflakeItemMaster.Tests.Helpers
{
    public static class SnowflakeTestSeeder
    {
        private static string GetTestDataFilePath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var testDataPath = Path.GetFullPath(Path.Combine(
                baseDirectory,
                "..", "..", "..",
                "TestData", "item_petal_us_test_data.json"
            ));

            if (!File.Exists(testDataPath))
            {
                throw new FileNotFoundException($"Test data file not found at: {testDataPath}");
            }

            return testDataPath;
        }

        public const string TableFullName = "SANDBOX_US_DB.XB_DEV_DB.ITEM_PETAL_US";

        public static void SeedTestData(string connectionString)
        {
            var testDataFile = GetTestDataFilePath();
            var json = File.ReadAllText(testDataFile);

            var items = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

            if (items == null || items.Count == 0)
                throw new Exception("No test data found in JSON file.");

            using var conn = new SnowflakeDbConnection();
            conn.ConnectionString = connectionString;
            conn.Open();

            using var cmd = conn.CreateCommand();

            // Create table in existing schema
            cmd.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {TableFullName} (
                BRAND VARCHAR, REGION VARCHAR, SKU VARCHAR, STATUS VARCHAR, BARCODE VARCHAR, SECONDARYBARCODE VARCHAR,
                PRODUCTTITLE VARCHAR, COLOR VARCHAR, SIZE VARCHAR, WEIGHT FLOAT, VOLUME FLOAT, HEIGHT FLOAT, WIDTH FLOAT, LENGTH FLOAT,
                PRODUCTTYPE VARCHAR, CATEGORY VARCHAR, GENDER VARCHAR, FABRICCONTENT VARCHAR, FABRICCOMPOSITION VARCHAR, COUNTRYOFORIGIN VARCHAR,
                HTS VARCHAR, CHINAHTS VARCHAR, VELOCITYCODE VARCHAR, FASTMOVER VARCHAR, DESCRIPTION VARCHAR,
                PRODUCTIMAGEURL VARCHAR, PRODUCTIMAGEURLPOS1 VARCHAR, PRODUCTIMAGEURLPOS2 VARCHAR, PRODUCTIMAGEURLPOS3 VARCHAR,
                LANDEDCOST FLOAT, COST FLOAT, PRICE FLOAT, LATESTPONUMBER VARCHAR, LATESTPOSTATUS VARCHAR,
                LATESTPOCREATEDDATE TIMESTAMP_TZ, LATESTPOEXPECTEDDATE TIMESTAMP_TZ,
                WH1NAME VARCHAR, WH1AVAILABLEQTY NUMBER(38,0), WH2NAME VARCHAR, WH2AVAILABLEQTY NUMBER(38,0), WH3NAME VARCHAR, WH3AVAILABLEQTY NUMBER(38,0),
                CREATEDATSHOPIFY TIMESTAMP_TZ, CREATEDATSNOWFLAKE TIMESTAMP_TZ, UPDATEDATSNOWFLAKE TIMESTAMP_TZ,
                PRESENTINXBFLAG BOOLEAN, INVENTORYSYNCFLAG VARCHAR, THIRDBARCODE VARCHAR
            )";
            cmd.ExecuteNonQuery();

            // Xóa dữ liệu cũ
            cmd.CommandText = $"DELETE FROM {TableFullName}";
            cmd.ExecuteNonQuery();

            // Insert từng record từ JSON
            foreach (var item in items)
            {
                var columns = string.Join(", ", item.Keys);
                var values = string.Join(", ", item.Values.Select(ToSqlValue));
                cmd.CommandText = $"INSERT INTO {TableFullName} ({columns}) VALUES ({values})";
                cmd.ExecuteNonQuery();
            }
        }

        // Helper: convert JsonElement to SQL string
        private static string ToSqlValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    return "NULL";

                case JsonValueKind.String:
                    var str = value.GetString();
                    // Xử lý ngày giờ ISO 8601
                    if (DateTime.TryParse(str, out var dt))
                        return $"TO_TIMESTAMP_TZ('{dt:yyyy-MM-ddTHH:mm:ss.fffZ}')";
                    return $"'{str?.Replace("'", "''")}'";

                case JsonValueKind.Number:
                    if (value.TryGetInt64(out var l)) return l.ToString();
                    if (value.TryGetDouble(out var d)) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;

                case JsonValueKind.True:
                    return "TRUE";

                case JsonValueKind.False:
                    return "FALSE";
            }
            return "NULL";
        }
    }
}