using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire.Storage.SQLite;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class SqliteFixture(IMessageSink messageSink) : HangfireFixture(messageSink)
{
    private const string DbFileName = "Hangfire.SequentialJob.Tests.db";

    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        var storageOptions = new SQLiteStorageOptions
        {
            QueuePollInterval = TimeSpan.FromSeconds(1),
            JournalMode = SQLiteStorageOptions.JournalModes.MEMORY,
        };
        hangfire.UseSQLiteStorage(DbFileName, storageOptions);
        return "Sqlite";
    }

    protected override Task InitializeDbAsync() => ClearDbAsync();

    protected override Task DisposeDbAsync() => ClearDbAsync();

    private static Task ClearDbAsync()
    {
        File.Delete(DbFileName);
        return Task.CompletedTask;
    }
}