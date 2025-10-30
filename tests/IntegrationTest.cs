using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public abstract class IntegrationTest : IDisposable
{
    public class InMemory(HangfireFixture.InMemory fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.InMemory>;
    public class Mongo(HangfireFixture.Mongo fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Mongo>;
    public class Postgres(HangfireFixture.Postgres fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Postgres>;
    public class Redis(HangfireFixture.Redis fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Redis>;
    public class Sqlite(HangfireFixture.Sqlite fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Sqlite>;
    public class SqlServer(HangfireFixture.SqlServer fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.SqlServer>;

    private readonly HangfireFixture _fixture;

    private IBackgroundJobClientV2 BackgroundJobClient => _fixture.BackgroundJobClient;
    private IMonitoringApi MonitoringApi => BackgroundJobClient.Storage.GetMonitoringApi();

    protected IntegrationTest(HangfireFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.Output = output;
    }

    public void Dispose()
    {
        _fixture.Output = null;
    }

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

    private List<string> GetStates(string jobId)
    {
        return MonitoringApi.JobDetails(jobId).History.Select(e => e.StateName).ToList();
    }

    private async Task WaitAsync(Predicate<StatisticsDto> predicate, TimeSpan timeout, [CallerArgumentExpression(nameof(predicate))] string? predicateDescription = null)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!predicate(MonitoringApi.GetStatistics()))
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException($"Waited \"{predicateDescription}\" for {timeout.TotalSeconds:N0} seconds");
            }

            await Task.Delay(millisecondsDelay: 50);
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
