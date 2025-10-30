using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Hangfire.States;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public abstract class IntegrationTest(HangfireFixture fixture, ITestOutputHelper output) : HangfireTest(fixture, output)
{
    public class InMemory(HangfireFixture.InMemory fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.InMemory>;
    public class Mongo(HangfireFixture.Mongo fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Mongo>;
    public class Postgres(HangfireFixture.Postgres fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Postgres>;
    public class Redis(HangfireFixture.Redis fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Redis>;
    public class Sqlite(HangfireFixture.Sqlite fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Sqlite>;
    public class SqlServer(HangfireFixture.SqlServer fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.SqlServer>;

    [Fact]
    public async Task Test()
    {
        var jobIds = new string[100];
        for (var i = 0; i < jobIds.Length; i++)
        {
            var text = $"{i}";
            jobIds[i] = BackgroundJobClient.Create<TestJob>(o => o.Run(text), new EnqueuedState());
        }

        await WaitAsync(stats => stats.Succeeded == jobIds.Length, timeout: TimeSpan.FromSeconds(10));

        using (new AssertionScope())
        {
            GetStates(jobIds[0]).Should().BeEquivalentTo(["Succeeded", "Processing", "Enqueued"], because: "the first job should have transitioned through 3 states");
            for (var i = 1; i < jobIds.Length; i++)
            {
                GetStates(jobIds[i]).Should().BeEquivalentTo(["Succeeded", "Processing", "Enqueued", "Awaiting", "Enqueued"], because: $"job #{i} should have transitioned through 5 states");
            }
        }
    }

    // ReSharper disable All
    [SequentialJob("my-sequence")]
    public class TestJob
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required for Hangfire")]
        public string Run(string text)
        {
            return text;
        }
    }
}
