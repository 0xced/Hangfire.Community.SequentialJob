using Hangfire.Redis.StackExchange;
using Testcontainers.Redis;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class RedisFixture(IMessageSink messageSink) : HangfireContainerFixture<RedisBuilder, RedisContainer>(messageSink, new DbFixture(messageSink))
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseRedisStorage(Container.GetConnectionString());
        return "Redis";
    }

    private class DbFixture(IMessageSink messageSink) : ContainerFixture<RedisBuilder, RedisContainer>(messageSink)
    {
        protected override RedisBuilder Configure(RedisBuilder builder) => builder.WithImage("redis:8");
    }
}