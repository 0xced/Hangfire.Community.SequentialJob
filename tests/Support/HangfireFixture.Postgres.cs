using System.Data.Common;
using Hangfire.PostgreSql;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class PostgresFixture(IMessageSink messageSink) : HangfireContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink, new DbFixture(messageSink))
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UsePostgreSqlStorage(postgres => postgres.UseNpgsqlConnection(Container.GetConnectionString()));
        return "Postgres";
    }

    private class DbFixture(IMessageSink messageSink) : DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink)
    {
        public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;
        protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder) => builder.WithImage("postgres:18");
    }
}