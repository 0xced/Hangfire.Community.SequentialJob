using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire.Storage.SQLite;

namespace Hangfire.SequentialJob.Tests;

public class SqliteFixture : HangfireFixture
{
    private const string DbFileName = "Hangfire.SequentialJob.Tests.db";

    protected override void ConfigureStorage(IGlobalConfiguration hangfire)
    {
        var storageOptions = new SQLiteStorageOptions
        {
            QueuePollInterval = TimeSpan.FromSeconds(1),
            JournalMode = SQLiteStorageOptions.JournalModes.MEMORY,
        };
        hangfire.UseSQLiteStorage(DbFileName, storageOptions);
    }

    protected override Task InitializeDbAsync() => ClearDbAsync();

    protected override Task DisposeDbAsync() => ClearDbAsync();

    private static Task ClearDbAsync()
    {
        File.Delete(DbFileName);
        return Task.CompletedTask;
    }
}