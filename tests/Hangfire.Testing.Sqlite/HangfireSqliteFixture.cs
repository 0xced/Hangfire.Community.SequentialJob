using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire.Storage.SQLite;
using Xunit.Abstractions;

namespace Hangfire.Testing.Sqlite;

public class HangfireSqliteFixture(IMessageSink messageSink) : HangfireFixture(messageSink)
{
    private const string DbFileName = "HangfireSqliteFixture.db";

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

    protected override Task InitializeStorageAsync() => ClearDbAsync();

    protected override Task DisposeStorageAsync() => ClearDbAsync();

    private static Task ClearDbAsync()
    {
        File.Delete(DbFileName);
        return Task.CompletedTask;
    }
}