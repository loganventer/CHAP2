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
    private IChorusGitHubSync _sync = null!;
    private ChorusGitSyncOrchestrator _sut = null!;
    private List<ChorusGitSyncProgress> _progressEvents = null!;
    private IProgress<ChorusGitSyncProgress> _progress = null!;
    private const string LocalDir = "/tmp/chap2-tests-orchestrator";

    [SetUp]
    public void SetUp()
    {
        _sync = Substitute.For<IChorusGitHubSync>();
        _sut = new ChorusGitSyncOrchestrator(_sync, LocalDir, "edits", "main", NullLogger<ChorusGitSyncOrchestrator>.Instance);
        _progressEvents = new List<ChorusGitSyncProgress>();
        _progress = new Progress<ChorusGitSyncProgress>(p => _progressEvents.Add(p));
    }

    [Test]
    public async Task SyncNowAsync_ChangesPushed_ReportsPushedTrueAndFileCount()
    {
        _sync.MirrorAsync(LocalDir, Arg.Any<string>(), Arg.Any<IProgress<ChorusGitSyncProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new ChorusMirrorResult(Created: 2, Updated: 1, Deleted: 1, CommitSha: "abc", Error: null));

        var result = await _sut.SyncNowAsync(_progress);

        result.Succeeded.Should().BeTrue();
        result.Pushed.Should().BeTrue();
        result.FilesCommitted.Should().Be(4);
    }

    [Test]
    public async Task SyncNowAsync_NoChanges_ReportsPushedFalse()
    {
        _sync.MirrorAsync(LocalDir, Arg.Any<string>(), Arg.Any<IProgress<ChorusGitSyncProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ChorusMirrorResult.NoChange("abc"));

        var result = await _sut.SyncNowAsync(progress: null);

        result.Succeeded.Should().BeTrue();
        result.Pushed.Should().BeFalse();
        result.FilesCommitted.Should().Be(0);
    }

    [Test]
    public async Task SyncNowAsync_MirrorFails_ReportsFailedAndErrorResult()
    {
        _sync.MirrorAsync(LocalDir, Arg.Any<string>(), Arg.Any<IProgress<ChorusGitSyncProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<Task<ChorusMirrorResult>>(_ => throw new InvalidOperationException("boom"));

        var result = await _sut.SyncNowAsync(_progress);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("boom");
        _progressEvents.Should().Contain(p => p.Stage == ChorusGitSyncStage.Failed);
    }

    [Test]
    public async Task SyncNowAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _sync.MirrorAsync(LocalDir, Arg.Any<string>(), Arg.Any<IProgress<ChorusGitSyncProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<Task<ChorusMirrorResult>>(_ => throw new OperationCanceledException());

        Func<Task> act = async () => await _sut.SyncNowAsync(progress: null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
