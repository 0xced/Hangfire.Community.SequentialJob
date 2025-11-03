using System;
using AwesomeAssertions;
using Hangfire.Common;
using Xunit;

namespace Hangfire.SequentialJob.Tests;

public class SequentialExecutionFilterProviderTest
{
    private readonly SequentialExecutionFilterProvider _target = new();

    [Fact]
    public void GetFilters_JobIsNull_ReturnsNoFilters()
    {
        // Arrange

        // Act
        var result = _target.GetFilters(null);

        // Assert
        result.Should().BeEmpty();
    }

    [SequentialJob("my-sequence-id")]
    private class TestSequentialJob
    {
        public void Run() => throw new NotImplementedException();
    }

    [Fact]
    public void GetFilters_HasSequentialJobAttribute_ReturnsSequentialFilter()
    {
        // Arrange
        var job = Job.FromExpression<TestSequentialJob>(o => o.Run());

        // Act
        var result = _target.GetFilters(job);

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new JobFilter(new SequentialExecutionFilter("my-sequence-id"), JobFilterScope.Method, -1));
    }

    private class TestJob
    {
        public void Run() => throw new NotImplementedException();
    }

    [Fact]
    public void GetFilters_HasNoAttribute_ReturnsNothing()
    {
        // Arrange
        var job = Job.FromExpression<TestJob>(o => o.Run());

        // Act
        var result = _target.GetFilters(job);

        // Assert
        result.Should().BeEmpty();
    }
}