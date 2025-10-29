using System;

namespace Hangfire.SequentialJob;

/// <summary>
/// Defines the base functionalities of a job that must be executed sequentially.
/// </summary>
[AttributeUsage(validOn: AttributeTargets.Class)]
public sealed class SequentialJobAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of the <see cref="SequentialJobAttribute"/> class.
    /// </summary>
    /// <param name="sequenceId"> The value that uniquely identifies the serial execution sequence. All jobs with the same ID are executed sequentially, in enqueueing order.</param>
    public SequentialJobAttribute(string sequenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sequenceId);
        SequenceId = sequenceId;
    }

    /// <summary>
    /// The value that uniquely identifies the serial execution sequence. All jobs with the same ID are executed sequentially, in enqueueing order.
    /// </summary>
    public string SequenceId { get; }
}