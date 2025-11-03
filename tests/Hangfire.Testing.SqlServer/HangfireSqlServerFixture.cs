using System;
using Hangfire.Testing.Testcontainers;
using Testcontainers.MsSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.Testing.SqlServer;

public class HangfireSqlServerFixture : HangfireContainerFixture<MsSqlBuilder, MsSqlContainer>
{
    public HangfireSqlServerFixture(IMessageSink messageSink) : base(messageSink, CreateSqlServerFixture(messageSink, out var fixture))
    {
        fixture.ConfigureContainer = ConfigureContainer;
    }

    private static SqlServerFixture CreateSqlServerFixture(IMessageSink messageSink, out SqlServerFixture fixture)
    {
        fixture = new SqlServerFixture(messageSink);
        return fixture;
    }

    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseSqlServerStorage(Container.GetConnectionString());
        return "SqlServer";
    }

    protected virtual MsSqlBuilder ConfigureContainer(MsSqlBuilder builder) => builder.WithImage("mcr.microsoft.com/mssql/server:2022-CU21-ubuntu-22.04");

    private class SqlServerFixture(IMessageSink messageSink) : ContainerFixture<MsSqlBuilder, MsSqlContainer>(messageSink)
    {
        public Func<MsSqlBuilder, MsSqlBuilder> ConfigureContainer { get; set; } = b => b;
        protected override MsSqlBuilder Configure(MsSqlBuilder builder) => ConfigureContainer(builder);
    }
}