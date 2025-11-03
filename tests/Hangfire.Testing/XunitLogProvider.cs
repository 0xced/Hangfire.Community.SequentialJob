using System;
using System.Text;
using Hangfire.Logging;
using static System.FormattableString;

namespace Hangfire.Testing;

internal class XunitLogProvider(string description, Action<string> log) : ILogProvider
{
    public ILog GetLogger(string name) => new XunitLog(description, log, name);

    private class XunitLog(string description, Action<string> log, string name) : ILog
    {
        public bool Log(LogLevel logLevel, Func<string>? messageFunc, Exception? exception = null)
        {
            if (messageFunc is null)
            {
                return true;
            }

            var message = new StringBuilder(Invariant($"{name} [{description}]"));
            message.AppendLine();
            message.Append(Invariant($"  {Badge(logLevel)} {messageFunc.Invoke()}"));
            if (exception != null)
            {
                message.AppendLine();
                message.Append(Invariant($"  ❌ {exception}"));
            }

            log(message.ToString());

            return true;
        }

        private static string Badge(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "⚪️",
                LogLevel.Debug => "🟤",
                LogLevel.Info => "⚫️",
                LogLevel.Warn => "🟠",
                LogLevel.Error => "🔴",
                LogLevel.Fatal => "🟣",
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, $"The value of argument '{nameof(level)}' ({level}) is invalid for enum type '{nameof(LogLevel)}'."),
            };
        }
    }
}