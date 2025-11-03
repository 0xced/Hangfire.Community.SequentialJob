using System;
using Hangfire.Redis.StackExchange;
using Hangfire.Testing.Testcontainers;
using Testcontainers.Redis;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.Testing.Redis;

public class HangfireRedisFixture : HangfireContainerFixture<RedisBuilder, RedisContainer>
{
    public HangfireRedisFixture(IMessageSink messageSink) : base(messageSink, CreateRedisFixture(messageSink, out var fixture))
    {
        fixture.ConfigureContainer = ConfigureContainer;
    }

    private static RedisFixture CreateRedisFixture(IMessageSink messageSink, out RedisFixture fixture)
    {
        fixture = new RedisFixture(messageSink);
        return fixture;
    }

    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseRedisStorage(Container.GetConnectionString());
        return "Redis";
    }

    protected virtual RedisBuilder ConfigureContainer(RedisBuilder builder) => builder.WithImage("redis:8");

    private class RedisFixture(IMessageSink messageSink) : ContainerFixture<RedisBuilder, RedisContainer>(messageSink)
    {
        public Func<RedisBuilder, RedisBuilder> ConfigureContainer { get; set; } = b => b;
        protected override RedisBuilder Configure(RedisBuilder builder) => ConfigureContainer(builder);
    }
}