using System;
using System.Collections.Generic;
using Hangfire.States;

namespace Hangfire.SequentialJob;

/// <summary>
/// When applied to a job, serializes the execution of all the jobs with the same sequence id.
/// </summary>
/// <remarks>After creation, it is mandatory to set the <see cref="SequenceId"/> property, otherwise, the filter will fail.</remarks>
public class SequentialExecutionFilter : IElectStateFilter
{
    /// <summary>
    /// Creates a new instance of the <see cref="SequentialExecutionFilter"/> class.
    /// </summary>
    /// <param name="sequenceId">The value that uniquely identifies the serial execution sequence. All jobs with the same ID are executed sequentially, in enqueueing order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sequenceId"/> is <c>null</c></exception>
    /// <exception cref="ArgumentException"><paramref name="sequenceId"/> is null or composed of white-spaces only.</exception>
    public SequentialExecutionFilter(string sequenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sequenceId);
        SequenceId = sequenceId;
    }

    /// <summary>
    /// The value that uniquely identifies the serial execution sequence. All jobs with the same ID are executed sequentially, in enqueueing order.
    /// </summary>
    public string SequenceId { get; }

    /// <summary>
    /// The name of the Hangfire distributed lock used to synchronize access.
    /// </summary>
    /// <remarks>Defaults to <c>SequentialExecutionLock</c>.</remarks>
    public string DistributedLockName { get; init; } = "SequentialExecutionLock";

    /// <summary>
    /// The name of the parameter added to the Hangfire job that exposes the ID of the execution serialization sequence.
    /// </summary>
    /// <remarks>Defaults to <c>SequenceId</c>.</remarks>
    public string SequenceIdParameterName { get; init; } = "SequenceId";

    /// <summary>
    /// The name of the Hangfire hash that is used to store that last processed job id.
    /// </summary>
    /// <remarks>Defaults to <c>SequentialExecutionLastId</c>.</remarks>
    public string LastJobIdHashName { get; init; } = "SequentialExecutionLastId";

    /// <inheritdoc/>
    public void OnStateElection(ElectStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Skip filter if the job is not transitioning to the enqueued state.
        if (context.CandidateState is not EnqueuedState)
            return;

        // Do everything within a distributed lock to avoid concurrency issues.
        using (context.Connection.AcquireDistributedLock(DistributedLockName, TimeSpan.FromSeconds(10)))
        {
            // Skip filter if the job was already processed.
            var jobCurrentSequenceId = context.Connection.GetJobParameter(context.BackgroundJob.Id, SequenceIdParameterName);
            if (!string.IsNullOrEmpty(jobCurrentSequenceId))
                return;

            // Fetch the last job ID for the queue.
            var hashData = context.Connection.GetAllEntriesFromHash(LastJobIdHashName);

            // Change the state if required.
            if (hashData != null && hashData.TryGetValue(SequenceId, out var lastEnqueuedId) && !string.IsNullOrEmpty(lastEnqueuedId))
            {
                // Successful jobs are deleted after some period of time so we must check if it still exists (i.e. jobData != null) because an AwaitingState can't be dependent on a non-existent job.
                var jobData = context.Connection.GetJobData(lastEnqueuedId);
                if (jobData != null)
                {
                    // Options to continue on any finished state is important. It will allow the job to run when the state becomes Succeeded or Deleted.
                    var reason = $"Serial execution on sequence {SequenceId}";
                    context.CandidateState = new AwaitingState(parentId: lastEnqueuedId, nextState: context.CandidateState, options: JobContinuationOptions.OnAnyFinishedState) { Reason = reason };
                }
            }

            // Mark the job as processed. This also exposes the serialization sequence id for the dashboard.
            context.Connection.SetJobParameter(context.BackgroundJob.Id, SequenceIdParameterName, SequenceId);

            // Update the last queued job id for the next job in the queue.
            context.Connection.SetRangeInHash(LastJobIdHashName, [KeyValuePair.Create(SequenceId, context.BackgroundJob.Id)]);
        }
    }
}