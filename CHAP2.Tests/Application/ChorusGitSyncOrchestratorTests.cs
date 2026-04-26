using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using CHAP2.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class ChorusGitSyncOrchestratorTests
{
    private IGitWorkingTree _tree = null!;
    private ChorusGitSyncOrchestrator _sut = null!;
    private List<ChorusGitSyncProgress> _progressEvents = null!;
    private IProgress<ChorusGitSyncProgress> _progress = null!;

    [SetUp]
    public void SetUp()
    {
        _tree = Substitute.For<IGitWorkingTree>();
        _sut = new ChorusGitSyncOrchestrator(_tree, NullLogger<ChorusGitSyncOrchestrator>.Instance);
        _progressEvents = new List<ChorusGitSyncProgress>();
        _progress = new Progress<ChorusGitSyncProgress>(p => _progressEvents.Add(p));
    }

    [Test]
    public async Task SyncNowAsync_HappyPath_PullsStagesCommitsPushes_InOrder()
    {
        _tree.HasStagedChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        _tree.StagedFileCountAsync(Arg.Any<CancellationToken>()).Returns(3);
        _tree.LocalIsAheadOfRemoteAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.SyncNowAsync(progress: null);

        result.Succeeded.Should().BeTrue();
        result.Pulled.Should().BeTrue();
        result.FilesCommitted.Should().Be(3);
        result.Pushed.Should().BeTrue();

        Received.InOrder(() =>
        {
            _tree.EnsureCloneAsync(Arg.Any<CancellationToken>());
            _tree.PullLocalWinsAsync(Arg.Any<CancellationToken>());
            _tree.StageAllAsync(Arg.Any<CancellationToken>());
            _tree.HasStagedChangesAsync(Arg.Any<CancellationToken>());
            _tree.StagedFileCountAsync(Arg.Any<CancellationToken>());
            _tree.CommitAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            _tree.PushAsync(Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task SyncNowAsync_NothingStaged_AndLocalNotAhead_DoesNotCommitOrPush()
    {
        _tree.HasStagedChangesAsync(Arg.Any<CancellationToken>()).Returns(false);
        _tree.LocalIsAheadOfRemoteAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.SyncNowAsync(progress: null);

        result.Succeeded.Should().BeTrue();
        result.FilesCommitted.Should().Be(0);
        result.Pushed.Should().BeFalse();

        await _tree.DidNotReceive().CommitAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tree.DidNotReceive().PushAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SyncNowAsync_LocalAheadButNothingStaged_PushesAnyway()
    {
        _tree.HasStagedChangesAsync(Arg.Any<CancellationToken>()).Returns(false);
        _tree.LocalIsAheadOfRemoteAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.SyncNowAsync(progress: null);

        result.Pushed.Should().BeTrue();
        await _tree.Received(1).PushAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SyncNowAsync_TreeThrows_ReportsFailedAndReturnsErrorResult()
    {
        _tree.PullLocalWinsAsync(Arg.Any<CancellationToken>())
             .Returns<Task>(_ => throw new InvalidOperationException("git pull failed (exit 1): some boom"));

        var result = await _sut.SyncNowAsync(_progress);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("boom");
        _progressEvents.Should().Contain(p => p.Stage == ChorusGitSyncStage.Failed);
    }

    [Test]
    public async Task SyncNowAsync_WithProgress_ReportsAllStagesInOrder()
    {
        _tree.HasStagedChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        _tree.StagedFileCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        _tree.LocalIsAheadOfRemoteAsync(Arg.Any<CancellationToken>()).Returns(true);

        await _sut.SyncNowAsync(_progress);

        var stages = _progressEvents.Select(e => e.Stage).ToList();
        stages.Should().Equal(
            ChorusGitSyncStage.Pulling,
            ChorusGitSyncStage.Staging,
            ChorusGitSyncStage.Committing,
            ChorusGitSyncStage.Pushing,
            ChorusGitSyncStage.Done);
    }

    [Test]
    public async Task SyncNowAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _tree.PullLocalWinsAsync(Arg.Any<CancellationToken>())
             .Returns<Task>(_ => throw new OperationCanceledException());

        Func<Task> act = async () => await _sut.SyncNowAsync(progress: null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
