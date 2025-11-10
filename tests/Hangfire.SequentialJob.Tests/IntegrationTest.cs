using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Hangfire.Testing;
using Hangfire.Testing.InMemory;
using Hangfire.Testing.Mongo;
using Hangfire.Testing.PostgreSql;
using Hangfire.Testing.Redis;
using Hangfire.Testing.Sqlite;
using Hangfire.Testing.SqlServer;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public abstract class IntegrationTest(HangfireFixture fixture, ITestOutputHelper output) : HangfireTest(fixture, output)
{
    public class InMemory(HangfireInMemoryFixture fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireInMemoryFixture>;
    public class Mongo(HangfireMongoFixture fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireMongoFixture>;
    public class Postgres(HangfirePostgreSqlFixture fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfirePostgreSqlFixture>;
    public class Redis(HangfireRedisFixture fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireRedisFixture>;
    public class Sqlite(HangfireSqliteFixture fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireSqliteFixture>;
    public class SqlServer(HangfireSqlServerFixture fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireSqlServerFixture>;

    [Fact]
    public async Task TestSuccessStates()
    {
        var jobIds = new string[100];
        for (var i = 0; i < jobIds.Length; i++)
        {
            var n = i;
            jobIds[i] = BackgroundJobClient.Enqueue<AlwaysSucceed>(o => o.Run(n));
        }

        await WaitAsync(stats => stats.Succeeded == jobIds.Length, timeout: TimeSpan.FromSeconds(10));

        using (new AssertionScope())
        {
            GetStates(jobIds[0]).Should().BeEquivalentTo(["Succeeded", "Processing", "Enqueued"], because: "the first job should have transitioned through 3 states");
            for (var i = 1; i < jobIds.Length; i++)
            {
                var previousJobId = jobIds[i - 1];
                var jobId = jobIds[i];
                GetStates(jobId).Should().BeEquivalentTo(["Succeeded", "Processing", "Enqueued", "Awaiting", "Enqueued"], because: $"job #{i} should have transitioned through 5 states");
                var parentId = MonitoringApi.JobDetails(jobId).History.Single(e => e.StateName == "Awaiting").Data["ParentId"];
                parentId.Should().Be(previousJobId);
            }
        }
    }

    private List<string> GetStates(string jobId)
    {
        return MonitoringApi.JobDetails(jobId).History.Select(e => e.StateName).ToList();
    }

    // ReSharper disable All
    [SequentialJob("always-succeed")]
    public class AlwaysSucceed
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required for Hangfire")]
        public string Run(int number)
        {
            return $"{number}";
        }
    }
}
