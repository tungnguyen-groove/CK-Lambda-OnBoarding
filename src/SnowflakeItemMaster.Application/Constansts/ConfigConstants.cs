using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnowflakeItemMaster.Application.Constansts
{
    public static class ConfigConstants
    {
        public static class SnowflakeConfigConstants
        {
            public const string Account = "SNOWFLAKE_ACCOUNT";
            public const string User = "SNOWFLAKE_USERNAME";
            public const string Password = "SNOWFLAKE_PASSWORD";
            public const string Database = "SNOWFLAKE_DATABASE";
            public const string Schema = "SNOWFLAKE_SCHEMA";
            public const string Warehouse = "SNOWFLAKE_WAREHOUSE";
            public const string Role = "SNOWFLAKE_ROLE";
        }

        public static class DatabaseConfigConstants
        {
            public const string Host = "DATABASE_HOST";
            public const string Database = "DATABASE_DATABASE";
            public const string User = "DATABASE_USER";
            public const string Password = "DATABASE_PASSWORD";
        }
    }
}
