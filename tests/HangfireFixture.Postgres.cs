using System.Data.Common;
using Hangfire.PostgreSql;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class PostgresFixture(IMessageSink messageSink) : HangfireContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(new DbFixture(messageSink))
{
    protected override void ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UsePostgreSqlStorage(postgres => postgres.UseNpgsqlConnection(Container.GetConnectionString()));
    }

    private class DbFixture(IMessageSink messageSink) : DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink)
    {
        public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;
        protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder) => builder.WithImage("postgres:18");
    }
}