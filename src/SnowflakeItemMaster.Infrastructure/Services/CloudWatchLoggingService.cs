using Amazon.Lambda.Core;
using SnowflakeItemMaster.Application.Contracts.Logger;
using System.Runtime.CompilerServices;

namespace SnowflakeItemMaster.Infrastructure.Services
{
    public class CloudWatchLoggingService : ILoggingService
    {
        private ILambdaLogger? _logger;

        public void SetLoggerContext(ILambdaLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInfo(string msg, [CallerMemberName] string? caller = null)
        {
            WriteLog($"[info] [caller: {caller}] - {msg}");
        }

        public void LogWarning(string msg, [CallerMemberName] string? caller = null)
        {
            WriteLog($"[warning] [caller: {caller}] - {msg}");
        }

        public void LogError(string msg, [CallerMemberName] string? caller = null)
        {
            WriteLog($"[error] [caller: {caller}] - {msg}");
        }

        private void WriteLog(string msg)
        {
            if (_logger != null)
            {
                _logger.LogLine(msg);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(msg);
            }
        }
    }
}