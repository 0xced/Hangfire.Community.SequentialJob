using System;
using AwesomeAssertions;
using Xunit;

namespace Hangfire.SequentialJob.Tests;

public class SequentialJobAttributeTest
{
    [Fact]
    public void Constructor_SequenceIdIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new SequentialJobAttribute(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("sequenceId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_SequenceIdEmptyOrWhitespaces_Throws(string sequenceId)
    {
        // Arrange

        // Act
        var action = () => new SequentialJobAttribute(sequenceId);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName(nameof(sequenceId)).WithMessage("The value cannot be an empty string or composed entirely of whitespace.*");
    }

    [Fact]
    public void SequenceId_CreatedWithValidId_ReturnsId()
    {
        // Arrange
        var target = new SequentialJobAttribute("id");

        // Act
        var sequenceId = target.SequenceId;

        // Assert
        sequenceId.Should().Be("id");
    }
}