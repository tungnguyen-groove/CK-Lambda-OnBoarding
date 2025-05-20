using System.Reflection;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Interfaces;

namespace SnowflakeItemMaster.Lambda.Tests
{
    /// <summary>
    /// TestFunction is a test class that inherits from FunctionBase.
    /// Provides a way to set up the Function class for unit testing.
    /// </summary>
    public class TestFunction : Function
    {
        public TestFunction(ISkuProcessor skuProcessor, ILoggingService logger)
            : base()
        {
            // Set the mock processor
            var skuProcessorField = typeof(Function)
                .GetField("_skuProcessor", BindingFlags.NonPublic | BindingFlags.Instance);
            skuProcessorField.SetValue(this, skuProcessor);

            // Set the logger
            var loggerField = typeof(FunctionBase)
                .GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
            loggerField.SetValue(this, logger);
        }
    }
}