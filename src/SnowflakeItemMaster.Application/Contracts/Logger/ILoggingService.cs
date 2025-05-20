using Amazon.Lambda.Core;
using System.Runtime.CompilerServices;

namespace SnowflakeItemMaster.Application.Contracts.Logger
{
    public interface ILoggingService
    {
        void LogError(string msg, [CallerMemberName] string? caller = null);
        void LogInfo(string msg, [CallerMemberName] string? caller = null);
        void LogWarning(string msg, [CallerMemberName] string? caller = null);
        void SetLoggerContext(ILambdaLogger logger);
    }
}