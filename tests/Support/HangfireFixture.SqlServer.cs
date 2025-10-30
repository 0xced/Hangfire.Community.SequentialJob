using System.Data.Common;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class SqlServerFixture(IMessageSink messageSink) : HangfireContainerFixture<MsSqlBuilder, MsSqlContainer>(messageSink, new DbFixture(messageSink))
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseSqlServerStorage(Container.GetConnectionString());
        return "SqlServer";
    }

    private class DbFixture(IMessageSink messageSink) : DbContainerFixture<MsSqlBuilder, MsSqlContainer>(messageSink)
    {
        public override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;
        protected override MsSqlBuilder Configure(MsSqlBuilder builder) => builder.WithImage("mcr.microsoft.com/mssql/server:2022-CU21-ubuntu-22.04");
    }
}