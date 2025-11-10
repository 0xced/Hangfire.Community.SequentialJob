using System;
using System.Collections.Generic;
using System.Reflection;
using AwesomeAssertions;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using NSubstitute;
using Xunit;

namespace Hangfire.SequentialJob.Tests;

public class SequentialJobAttributeTest
{
    private readonly SequentialJobAttribute _target = new("my-sequence-id");
    private IElectStateFilter Filter => _target;

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

    [Fact]
    public void DistributedLockName_Created_ShouldBeSequentialExecutionLock()
    {
        // Arrange

        // Act
        var distributedLockName = _target.DistributedLockName;

        // Assert
        distributedLockName.Should().Be("SequentialExecutionLock");
    }

    [Fact]
    public void SequenceIdParameterName_Created_ShouldBeSequenceId()
    {
        // Arrange

        // Act
        var sequenceIdParameterName = _target.SequenceIdParameterName;

        // Assert
        sequenceIdParameterName.Should().Be("SequenceId");
    }

    [Fact]
    public void LastJobIdHashName_Created_ShouldBeSequentialExecutionLastId()
    {
        // Arrange

        // Act
        var sequentialExecutionLastId = _target.LastJobIdHashName;

        // Assert
        sequentialExecutionLastId.Should().Be("SequentialExecutionLastId");
    }

    [Fact]
    public void OnStateElection_CandidateStateIsEnqueued_VerifiesWhetherTheJobHasAlreadyBeenProcessed()
    {
        // Arrange
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.Received(1).GetJobParameter("job-1", _target.SequenceIdParameterName);
    }

    [Theory]
    [InlineData(typeof(ScheduledState))]
    [InlineData(typeof(AwaitingState))]
    [InlineData(typeof(ProcessingState))]
    [InlineData(typeof(SucceededState))]
    [InlineData(typeof(FailedState))]
    [InlineData(typeof(DeletedState))]
    public void OnStateElection_CandidateStateIsNotEnqueued_DoesNothing(Type stateType)
    {
        // Arrange
        var state = CreateState(stateType);
        var context = CreateElectStateContext(state);

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OnStateElection_StateIsEnqueuedAndSequenceIdParameterIsNotSet_ProcessesTheJob(string? parameterValue)
    {
        // Arrange
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");
        context.Connection.GetJobParameter(Arg.Any<string>(), _target.SequenceIdParameterName).Returns(parameterValue);

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.ReceivedWithAnyArgs().GetAllEntriesFromHash(default);
    }

    [Fact]
    public void OnStateElection_StateIsEnqueuedButSequenceIdParameterIsSet_DoesNothing()
    {
        // Arrange
        var initialCandidateState = Substitute.For<IState>();
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");
        context.CandidateState = initialCandidateState;
        context.Connection.GetJobParameter(Arg.Any<string>(), _target.SequenceIdParameterName).Returns("my-sequence");

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.DidNotReceiveWithAnyArgs().GetAllEntriesFromHash(default);
        context.Connection.DidNotReceiveWithAnyArgs().SetJobParameter(default, default, default);
        context.Connection.DidNotReceiveWithAnyArgs().SetRangeInHash(default, default);
        context.CandidateState.Should().BeSameAs(initialCandidateState);
    }

    [Fact]
    public void OnStateElection_JobNeedsProcessing_AcquiresALock()
    {
        // Arrange
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.Received(1).AcquireDistributedLock(_target.DistributedLockName, Arg.Is<TimeSpan>(x => x > TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void OnStateElection_JobNeedsProcessing_MakesAllCallsWithinTheAcquiredLock()
    {
        // Arrange
        var distributedLock = new MockDistributedLockProvider();
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");

        context.Connection.AcquireDistributedLock(_target.DistributedLockName, Arg.Any<TimeSpan>()).Returns(_ => distributedLock.Acquire());

        var calls = new List<(string method, bool hasLock)>();
        void AddCall(string storeName, string memberName) => calls.Add(($"{storeName}.{memberName}", distributedLock.IsLocked));

        context.Connection.WhenForAnyArgs(o => o.GetJobParameter(default, default)).Do(_ => AddCall(nameof(IStorageConnection), nameof(IStorageConnection.GetJobParameter)));
        context.Connection.WhenForAnyArgs(o => o.SetJobParameter(default, default, default)).Do(_ => AddCall(nameof(IStorageConnection), nameof(IStorageConnection.SetJobParameter)));
        context.Connection.WhenForAnyArgs(o => o.GetAllEntriesFromHash(default)).Do(_ => AddCall(nameof(IStorageConnection), nameof(IStorageConnection.GetAllEntriesFromHash)));
        context.Connection.WhenForAnyArgs(o => o.SetRangeInHash(default, default)).Do(_ => AddCall(nameof(IStorageConnection), nameof(IStorageConnection.SetRangeInHash)));

        // Act
        Filter.OnStateElection(context);

        // Assert
        calls.Should().AllSatisfy(c => c.hasLock.Should().BeTrue(because: $"{c.method} should have been called within a distributed lock"));
    }

    [Fact]
    public void OnStateElection_JobNeedsProcessing_LoadsTheLastIdHash()
    {
        // Arrange
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.Received(1).GetAllEntriesFromHash(_target.LastJobIdHashName);
    }

    [Theory]
    [InlineData("Hash is null")]
    [InlineData("No matching sequence")]
    [InlineData("Null last job id")]
    [InlineData("Empty last job id")]
    public void OnStateElection_JobNeedsProcessingButNoPreviousJobExistsForSequence_DoesNotAlterCandidateState(string testCase)
    {
        // Arrange
        var candidateState = new EnqueuedState();
        var context = CreateElectStateContext(candidateState, jobId: "job-1");
        context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(testCase switch
        {
            "Hash is null" => null,
            "No matching sequence" => new Dictionary<string, string> { ["not-the-correct-sequence-name"] = "some-job-id" },
            "Null last job id" => new Dictionary<string, string> { [_target.SequenceId] = null! },
            "Empty last job id" => new Dictionary<string, string> { [_target.SequenceId] = "" },
            _ => throw new ArgumentException($"Unimplemented test case: \"{testCase}\""),
        });

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.CandidateState.Should().BeSameAs(candidateState);
    }

    [Fact]
    public void OnStateElection_JobNeedsProcessingAndAPreviousJobHashExistsForSequenceAndThePreviousJobExists_AltersCandidateStateToAwaitingState()
    {
        // Arrange
        var candidateState = new EnqueuedState { Reason = "my-enqueue-reason" };
        var context = CreateElectStateContext(candidateState, jobId: "job-1");
        context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(new Dictionary<string, string> { [_target.SequenceId] = "my-last-job-id" });
        context.Connection.GetJobData("my-last-job-id").Returns(new JobData());

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.CandidateState.Should().BeEquivalentTo(
            new AwaitingState(parentId: "my-last-job-id", nextState: candidateState, options: JobContinuationOptions.OnAnyFinishedState),
            opts => opts.Excluding(o => o.Reason));
        context.CandidateState.Reason.Should().Contain(_target.SequenceId);
    }

    [Fact]
    public void OnStateElection_JobNeedsProcessingAndAPreviousJobHashExistsForSequenceAndThePreviousJobHasExpired_DoesNotAlterEnqueuedCandidateState()
    {
        // Arrange
        var candidateState = new EnqueuedState();
        var context = CreateElectStateContext(candidateState, jobId: "job-1");
        context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(new Dictionary<string, string> { [_target.SequenceId] = "my-last-job-id" });
        context.Connection.GetJobData("my-last-job-id").Returns((JobData?)null);

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.CandidateState.Should().BeSameAs(candidateState);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnStateElection_JobNeedsProcessing_SetsTheSequenceIdParameter(bool parentJobExists)
    {
        // Arrange
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");
        if (parentJobExists)
            context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(new Dictionary<string, string> { [_target.SequenceId] = "my-last-job-id" });
        else
            context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(new Dictionary<string, string>());

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.Received(1).SetJobParameter("job-1", _target.SequenceIdParameterName, _target.SequenceId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnStateElection_JobNeedsProcessing_SetsTheLastJobId(bool parentJobExists)
    {
        // Arrange
        var context = CreateElectStateContext(new EnqueuedState(), jobId: "job-1");
        if (parentJobExists)
            context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(new Dictionary<string, string> { [_target.SequenceId] = "my-last-job-id" });
        else
            context.Connection.GetAllEntriesFromHash(default).ReturnsForAnyArgs(new Dictionary<string, string>());

        var setHashRanges = new List<KeyValuePair<string, string>>();
        context.Connection.SetRangeInHash(_target.LastJobIdHashName, Arg.Do<IEnumerable<KeyValuePair<string, string>>>(setHashRanges.AddRange));

        // Act
        Filter.OnStateElection(context);

        // Assert
        context.Connection.Received(1).SetRangeInHash(_target.LastJobIdHashName, Arg.Any<IEnumerable<KeyValuePair<string, string>>>());
        setHashRanges.Should().BeEquivalentTo(new List<KeyValuePair<string, string>>
        {
            KeyValuePair.Create(_target.SequenceId, "job-1")
        });
    }

    #region Helpers

    private static ElectStateContext CreateElectStateContext(IState state, string? jobId = null)
    {
        return new ElectStateContext(new ApplyStateContext(
            storage: Substitute.For<JobStorage>(),
            connection: Substitute.For<IStorageConnection>(),
            transaction: Substitute.For<IWriteOnlyTransaction>(),
            backgroundJob: new BackgroundJob(jobId ?? "dummy-job-id", Job.FromExpression<TestJob>(x => x.DoNothing()), DateTime.Now),
            newState: state,
            oldStateName: "my-old-state"
        ));
    }

    private class MockDistributedLockProvider
    {
        private readonly object _locker = new();
        private bool _isLocked;

        public bool IsLocked
        {
            get
            {
                lock (_locker)
                {
                    return _isLocked;
                }
            }
        }

        public IDisposable Acquire()
        {
            lock (_locker)
            {
                if (_isLocked) throw new InvalidOperationException("Cannot acquire an already acquired distributed lock.");
                _isLocked = true;
                return new DisposableLock(this);
            }
        }

        private class DisposableLock(MockDistributedLockProvider owner) : IDisposable
        {
            public void Dispose()
            {
                lock (owner._locker)
                {
                    if (!owner._isLocked) throw new InvalidOperationException("Cannot release an already released distributed lock.");
                    owner._isLocked = false;
                }
            }
        }
    }

    private class TestJob
    {
        public void DoNothing()
        {
        }
    }

    private static IState CreateState(Type stateType)
    {
        if (stateType == typeof(ScheduledState))
            return new ScheduledState(default(DateTime));

        if (stateType == typeof(AwaitingState))
            return new AwaitingState("the-parentId");

        if (stateType == typeof(ProcessingState))
            return (ProcessingState)Activator.CreateInstance(typeof(ProcessingState), BindingFlags.Instance | BindingFlags.NonPublic, null, ["the-serverId", "the-workerId"], null)!;

        if (stateType == typeof(SucceededState))
            return new SucceededState(default, default, default);

        if (stateType == typeof(FailedState))
            return new FailedState(new Exception());

        if (stateType == typeof(DeletedState))
            return new DeletedState();

        throw new NotSupportedException($"IState type {stateType} is not supported.");
    }

    #endregion Helpers
}