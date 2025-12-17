using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Community.SequentialJob;

/// <summary>
/// Ensures that Hangfire jobs decorated with this attribute are executed sequentially, in enqueueing order.
/// </summary>
public sealed class SequentialJobAttribute : JobFilterAttribute, IElectStateFilter
{
    /// <summary>
    /// Creates a new instance of the <see cref="SequentialJobAttribute"/> class.
    /// </summary>
    /// <param name="sequenceId"> The value that uniquely identifies the execution sequence. All jobs with the same sequence identifier are executed sequentially, in enqueueing order.</param>
    /// <param name="sequenceIdParameterName">The name of the Hangfire job parameter used to store the last processed job id. Defaults to <c>SequenceId</c>.</param>
    /// <param name="lastJobIdHashName">The name of the Hangfire hash used to store the last processed job id by sequence id. Defaults to <c>SequentialExecutionLastId</c>.</param>
    /// <param name="timeoutInSeconds">The timeout (in seconds) for acquiring the distributed lock. Defaults to 10 seconds.</param>
    public SequentialJobAttribute(string sequenceId,
        string sequenceIdParameterName = "SequenceId",
        string lastJobIdHashName = "SequentialExecutionLastId",
        int timeoutInSeconds = 10)
    {
        SequenceId = sequenceId ?? throw new ArgumentNullException(nameof(sequenceId));
        SequenceIdParameterName = sequenceIdParameterName ?? throw new ArgumentNullException(nameof(sequenceIdParameterName));
        LastJobIdHashName = lastJobIdHashName ?? throw new ArgumentNullException(nameof(lastJobIdHashName));
        Timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        if (string.IsNullOrWhiteSpace(sequenceId))
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", nameof(sequenceId));
        if (string.IsNullOrWhiteSpace(sequenceIdParameterName))
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", nameof(sequenceIdParameterName));
        if (string.IsNullOrWhiteSpace(lastJobIdHashName))
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", nameof(lastJobIdHashName));
        if (timeoutInSeconds < 0)
            throw new ArgumentException("The timeout value must be greater than zero.", nameof(timeoutInSeconds));
    }

    /// <summary>
    /// The value that uniquely identifies the execution sequence. All jobs with the same sequence identifier are executed sequentially, in enqueueing order.
    /// </summary>
    public string SequenceId { get; }

    /// <summary>
    /// The name of the Hangfire job parameter used to store the last processed job id.
    /// </summary>
    /// <remarks>Defaults to <c>SequenceId</c>.</remarks>
    public string SequenceIdParameterName { get; }

    /// <summary>
    /// The name of the Hangfire hash used to store the last processed job id by sequence id.
    /// </summary>
    /// <remarks>Defaults to <c>SequentialExecutionLastId</c>.</remarks>
    public string LastJobIdHashName { get; }

    /// <summary>
    /// The timeout for acquiring the distributed lock.
    /// <remarks>Defaults to 10 seconds.</remarks>
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <inheritdoc/>
    void IElectStateFilter.OnStateElection(ElectStateContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Skip filter if the job is not transitioning to the enqueued state.
        if (context.CandidateState is not EnqueuedState)
            return;

        // Do everything within a distributed lock to avoid concurrency issues.
        using (context.Connection.AcquireDistributedLock($"SequentialJob:{SequenceId}", Timeout))
        {
            // Skip filter if the job was already processed.
            var jobCurrentSequenceId = context.Connection.GetJobParameter(context.BackgroundJob.Id, SequenceIdParameterName);
            if (!string.IsNullOrEmpty(jobCurrentSequenceId))
                return;

            // Fetch the last job id.
            var hashData = context.Connection.GetAllEntriesFromHash(LastJobIdHashName);

            // Change the state if required.
            if (hashData != null && hashData.TryGetValue(SequenceId, out var lastEnqueuedId) && !string.IsNullOrEmpty(lastEnqueuedId))
            {
                // Successful jobs are deleted after some period of time, so we must check if it still exists (i.e., jobData != null) because an AwaitingState can't be dependent on a non-existent job.
                var jobData = context.Connection.GetJobData(lastEnqueuedId);
                if (jobData != null)
                {
                    // Options to continue on any finished state are important. It will allow the job to run when the state becomes Succeeded or Deleted.
                    var reason = $"Sequential execution of {SequenceId}";
                    context.CandidateState = new AwaitingState(parentId: lastEnqueuedId, nextState: context.CandidateState, options: JobContinuationOptions.OnAnyFinishedState) { Reason = reason };
                }
            }

            // Mark the job as processed. This also exposes the sequence id for the dashboard.
            context.Connection.SetJobParameter(context.BackgroundJob.Id, SequenceIdParameterName, SequenceId);

            // Update the last enqueued job id for the next job.
            context.Connection.SetRangeInHash(LastJobIdHashName, [new KeyValuePair<string, string>(SequenceId, context.BackgroundJob.Id)]);
        }
    }
}