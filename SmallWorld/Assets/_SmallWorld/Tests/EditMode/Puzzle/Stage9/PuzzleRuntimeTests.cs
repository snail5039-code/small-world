using System.Collections.Generic;
using NUnit.Framework;

namespace SmallWorld.Puzzle.Stage9.Tests
{
    public sealed class PuzzleRuntimeTests
    {
        [Test]
        public void StartAndCorrectSubmissions_AdvanceStepsThenComplete()
        {
            var runtime = CreateRuntime(new PuzzleDefinition("photo", stepCount: 2));

            Assert.That(runtime.Start("photo"), Is.EqualTo(PuzzleActionResult.Accepted));
            Assert.That(runtime.Submit("photo", true), Is.EqualTo(PuzzleActionResult.Accepted));
            Assert.That(runtime.TryGetState("photo", out PuzzleState progress), Is.True);
            Assert.That(progress.Status, Is.EqualTo(PuzzleStatus.InProgress));
            Assert.That(progress.CurrentStep, Is.EqualTo(1));

            runtime.Submit("photo", true);
            runtime.TryGetState("photo", out PuzzleState completed);
            Assert.That(completed.Status, Is.EqualTo(PuzzleStatus.Completed));
            Assert.That(completed.CurrentStep, Is.EqualTo(2));
        }

        [Test]
        public void IncorrectSubmission_PreservesStepAndRevealsEachHintOnceAtThreshold()
        {
            var definition = new PuzzleDefinition("photo", 2, new[] { new HintRule("look-edge", 2) });
            var runtime = CreateRuntime(definition);
            var hints = new List<string>();
            runtime.HintAvailable += message => hints.Add(message.HintId);
            runtime.Start("photo");

            Assert.That(runtime.Submit("photo", false), Is.EqualTo(PuzzleActionResult.Incorrect));
            Assert.That(runtime.Submit("photo", false), Is.EqualTo(PuzzleActionResult.Incorrect));
            Assert.That(runtime.Submit("photo", false), Is.EqualTo(PuzzleActionResult.Incorrect));

            runtime.TryGetState("photo", out PuzzleState state);
            Assert.That(state.CurrentStep, Is.Zero);
            Assert.That(state.IncorrectAttempts, Is.EqualTo(3));
            Assert.That(hints, Is.EqualTo(new[] { "look-edge" }));
        }

        [Test]
        public void Completion_EmitsSpatialCommandsAndNotifiesExternalSink()
        {
            var sink = new RecordingCompletionSink();
            var change = new SpatialChangeCommand("model-house.wall", "SetActive", "false");
            var runtime = new PuzzleRuntime(new[] { new PuzzleDefinition("model-house", completionChanges: new[] { change }) }, sink);
            SpatialChangeRequestedEvent request = null;
            runtime.SpatialChangeRequested += message => request = message;

            runtime.Start("model-house");
            runtime.Submit("model-house", true);

            Assert.That(request, Is.Not.Null);
            Assert.That(request.Command, Is.SameAs(change));
            Assert.That(sink.CompletedIds, Is.EqualTo(new[] { "model-house" }));
        }

        [Test]
        public void RestoredCompletion_CannotStartOrRewardAgain()
        {
            var original = CreateRuntime(new PuzzleDefinition("photo"));
            original.Start("photo");
            original.Submit("photo", true);
            PuzzleSnapshot snapshot = original.CaptureSnapshot();
            var sink = new RecordingCompletionSink();
            var restored = new PuzzleRuntime(new[] { new PuzzleDefinition("photo") }, sink);

            restored.RestoreSnapshot(snapshot);

            Assert.That(restored.Start("photo"), Is.EqualTo(PuzzleActionResult.AlreadyCompleted));
            Assert.That(restored.Submit("photo", true), Is.EqualTo(PuzzleActionResult.AlreadyCompleted));
            Assert.That(sink.CompletedIds, Is.Empty);
        }

        [Test]
        public void RestoredCompletion_ReappliesSpatialChangesThroughRestorePathOnly()
        {
            var change = new SpatialChangeCommand("room.wall", "SetActive", "false");
            var sink = new RecordingCompletionSink();
            var runtime = new PuzzleRuntime(
                new[] { new PuzzleDefinition("photo", completionChanges: new[] { change }) }, sink);
            var requests = new List<SpatialChangeRequestedEvent>();
            runtime.SpatialChangeRequested += requests.Add;
            var snapshot = new PuzzleSnapshot(new[]
            {
                new PuzzleSnapshotEntry("photo", PuzzleStatus.Completed, 1, 0)
            });

            runtime.RestoreSnapshot(snapshot);
            runtime.RestoreSnapshot(snapshot);

            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(requests, Has.All.Matches<SpatialChangeRequestedEvent>(request => request.IsRestore));
            Assert.That(requests[0].Command, Is.SameAs(change));
            Assert.That(sink.CompletedIds, Is.Empty);
        }

        [Test]
        public void RestoreSnapshot_ValidatesAllEntriesBeforeAnyMutationOrEvent()
        {
            var invalidSnapshots = new[]
            {
                new PuzzleSnapshot(new[]
                {
                    new PuzzleSnapshotEntry("first", PuzzleStatus.Completed, 1, 0),
                    new PuzzleSnapshotEntry("second", PuzzleStatus.InProgress, 2, 0)
                }),
                new PuzzleSnapshot(new[]
                {
                    new PuzzleSnapshotEntry("first", PuzzleStatus.Completed, 1, 0),
                    new PuzzleSnapshotEntry("second", PuzzleStatus.InProgress, 0, -1)
                }),
                new PuzzleSnapshot(new[]
                {
                    new PuzzleSnapshotEntry("first", PuzzleStatus.Completed, 1, 0),
                    new PuzzleSnapshotEntry("second", (PuzzleStatus)999, 0, 0)
                }),
                new PuzzleSnapshot(new[]
                {
                    new PuzzleSnapshotEntry("first", PuzzleStatus.Completed, 1, 0),
                    new PuzzleSnapshotEntry("first", PuzzleStatus.Completed, 1, 0)
                })
            };

            foreach (PuzzleSnapshot snapshot in invalidSnapshots)
            {
                var runtime = CreateRuntime(new PuzzleDefinition("first"), new PuzzleDefinition("second"));
                int events = 0;
                runtime.StateChanged += _ => events++;

                Assert.That(() => runtime.RestoreSnapshot(snapshot), Throws.ArgumentException);
                runtime.TryGetState("first", out PuzzleState first);
                runtime.TryGetState("second", out PuzzleState second);
                Assert.That(first.Status, Is.EqualTo(PuzzleStatus.NotStarted));
                Assert.That(second.Status, Is.EqualTo(PuzzleStatus.NotStarted));
                Assert.That(events, Is.Zero);
            }
        }

        [Test]
        public void RestoreSnapshot_NullEntriesHasExplicitArgumentContract()
        {
            var runtime = CreateRuntime(new PuzzleDefinition("photo"));
            var snapshot = new PuzzleSnapshot { Entries = null };

            Assert.That(() => runtime.RestoreSnapshot(snapshot), Throws.ArgumentException);
        }

        [Test]
        public void Snapshot_RestoresInProgressStepAndIncorrectAttempts()
        {
            var definition = new PuzzleDefinition("steps", 3);
            var original = CreateRuntime(definition);
            original.Start("steps");
            original.Submit("steps", true);
            original.Submit("steps", false);
            var restored = CreateRuntime(definition);

            restored.RestoreSnapshot(original.CaptureSnapshot());

            restored.TryGetState("steps", out PuzzleState state);
            Assert.That(state.Status, Is.EqualTo(PuzzleStatus.InProgress));
            Assert.That(state.CurrentStep, Is.EqualTo(1));
            Assert.That(state.IncorrectAttempts, Is.EqualTo(1));
        }

        [Test]
        public void RestoredState_ExposesAllPreviouslyUnlockedHintsForUiRebuild()
        {
            var definition = new PuzzleDefinition("photo", hintRules: new[]
            {
                new HintRule("first-hint", 1),
                new HintRule("second-hint", 3)
            });
            var runtime = CreateRuntime(definition);

            runtime.RestoreSnapshot(new PuzzleSnapshot(new[]
            {
                new PuzzleSnapshotEntry("photo", PuzzleStatus.InProgress, 0, 3)
            }));

            runtime.TryGetState("photo", out PuzzleState state);
            Assert.That(state.RevealedHintIds, Is.EqualTo(new[] { "first-hint", "second-hint" }));
        }

        [Test]
        public void InvalidTransitionsAndUnknownIds_AreRejectedWithoutMutation()
        {
            var runtime = CreateRuntime(new PuzzleDefinition("known"));

            Assert.That(runtime.Submit("known", true), Is.EqualTo(PuzzleActionResult.NotStarted));
            Assert.That(runtime.Start("missing"), Is.EqualTo(PuzzleActionResult.UnknownPuzzle));
            Assert.That(runtime.TryGetState("missing", out _), Is.False);
            runtime.TryGetState("known", out PuzzleState state);
            Assert.That(state.Status, Is.EqualTo(PuzzleStatus.NotStarted));
        }

        private static PuzzleRuntime CreateRuntime(params PuzzleDefinition[] definitions) => new PuzzleRuntime(definitions);

        private sealed class RecordingCompletionSink : IPuzzleCompletionSink
        {
            public readonly List<string> CompletedIds = new List<string>();
            public void OnPuzzleCompleted(string puzzleId) => CompletedIds.Add(puzzleId);
        }
    }
}
