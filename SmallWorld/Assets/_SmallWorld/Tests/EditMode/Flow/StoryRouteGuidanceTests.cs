#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class StoryRouteGuidanceTests
    {
        [TestCase(StoryChapterId.Prologue, "프롤로그")]
        [TestCase(StoryChapterId.Chapter1, "네 번째 자리")]
        [TestCase(StoryChapterId.Chapter2, "마지막 승강장")]
        [TestCase(StoryChapterId.Chapter3, "완벽한 하루")]
        [TestCase(StoryChapterId.Chapter4, "얼굴 없는 사무실")]
        [TestCase(StoryChapterId.Chapter5, "장례식 없는 묘지")]
        [TestCase(StoryChapterId.Chapter6, "창문 안의 도시")]
        [TestCase(StoryChapterId.FinalChapter, "아무것도 남지 않은 하얀 방")]
        public void EveryArrivalNamesItsCanonicalSpace(StoryChapterId chapter, string expected)
        {
            Assert.That(StoryRouteGuidance.Location(chapter), Does.Contain(expected));
            Assert.That(StoryRouteGuidance.ArrivalObjective(chapter), Is.Not.Empty);
        }

        [Test]
        public void AcceptedPuzzleStepPointsToNextMarker()
        {
            string next = StoryRouteGuidance.NextObjective(StoryChapterId.Chapter2,
                OpeningStoryAction.ReverseAnnouncement3, true);
            Assert.That(next, Does.Contain("목적지"));
            Assert.That(next, Does.Contain("안전 구역"));
        }

        [Test]
        public void RejectedActionExplainsHowToRecoverWithoutBlockingProgress()
        {
            string next = StoryRouteGuidance.NextObjective(StoryChapterId.Chapter4,
                OpeningStoryAction.AlignMirrorSeat3, false);
            Assert.That(next, Does.Contain("잠김 사유"));
            Assert.That(next, Does.Contain("직전"));
        }

        [Test]
        public void RelationshipProducesSmallArrivalDialogueBranch()
        {
            var progress = new StoryProgress();
            string warm = StoryRouteGuidance.ArrivalDialogue(progress, 10);
            string wary = StoryRouteGuidance.ArrivalDialogue(progress, -1);
            Assert.That(warm, Is.Not.EqualTo(wary));
            Assert.That(warm, Does.Contain("유나"));
            Assert.That(wary, Does.Contain("유나"));
        }

        [Test]
        public void FinalPreparationStopsBeforeEndingExecution()
        {
            string next = StoryRouteGuidance.NextObjective(StoryChapterId.FinalChapter,
                OpeningStoryAction.PrepareFinalChoice, true);
            Assert.That(next, Does.Contain("여기서 멈춘다"));
            Assert.That(next, Does.Contain("실행하지 않는다"));
        }
    }
}
#endif
