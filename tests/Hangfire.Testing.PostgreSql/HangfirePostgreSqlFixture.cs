using System;
using Hangfire.PostgreSql;
using Hangfire.Testing.Testcontainers;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.Testing.PostgreSql;

public class HangfirePostgreSqlFixture : HangfireContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>
{
    public HangfirePostgreSqlFixture(IMessageSink messageSink) : base(messageSink, CreatePostgreSqlFixture(messageSink, out var fixture))
    {
        fixture.ConfigureContainer = ConfigureContainer;
    }

    private static PostgreSqlFixture CreatePostgreSqlFixture(IMessageSink messageSink, out PostgreSqlFixture fixture)
    {
        fixture = new PostgreSqlFixture(messageSink);
        return fixture;
    }

    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UsePostgreSqlStorage(postgres => postgres.UseNpgsqlConnection(Container.GetConnectionString()));
        return "Postgres";
    }

    protected virtual PostgreSqlBuilder ConfigureContainer(PostgreSqlBuilder builder) => builder.WithImage("postgres:18");

    private class PostgreSqlFixture(IMessageSink messageSink) : ContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink)
    {
        public Func<PostgreSqlBuilder, PostgreSqlBuilder> ConfigureContainer { get; set; } = b => b;
        protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder) => ConfigureContainer(builder);
    }
}