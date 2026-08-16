using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Player.Tests
{
    public sealed class PlayerInteractionDetectorTests
    {
        private GameObject detectorObject;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            detectorObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void Configure_UsesTwoMeterDefaultRange()
        {
            PlayerInteractionDetector detector = CreateDetector();
            detector.Configure(detectorObject.transform);

            Assert.That(detector.Range, Is.EqualTo(2f));
        }

        [TestCase(1.5f, 0.5f, TestName = "RefreshDetection_NormalDistance_KeepsFrontInteractable")]
        [TestCase(0.3f, 0.2f, TestName = "RefreshDetection_VeryClose_KeepsFrontInteractable")]
        public void RefreshDetection_InFrontAndWithinRange_KeepsTarget(float centerZ, float depth)
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(centerZ, depth);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.True);
            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
            Assert.That(detector.TryInteract(), Is.True);
        }

        [Test]
        public void RefreshDetection_OnColliderSurface_KeepsFrontInteractable()
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(0.5f, 1f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.True);
            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
            Assert.That(detector.TryInteract(), Is.True);
        }

        [Test]
        public void RefreshDetection_InsideCollider_KeepsFrontInteractable()
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(0.25f, 1f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.True);
            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
            Assert.That(detector.TryInteract(), Is.True);
        }

        [Test]
        public void TryInteract_BeyondInteractionRange_IsRejected()
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(2.5f, 0.5f);

            detector.RefreshDetection();

            Assert.That(detector.CurrentInteractable, Is.SameAs(target),
                "The target should remain focused inside the detector's focus margin.");
            Assert.That(detector.TryInteract(), Is.False);
            Assert.That(target.InteractionCount, Is.Zero);
        }

        [Test]
        public void RefreshDetection_WallInFrontOfInteractable_BlocksTarget()
        {
            PlayerInteractionDetector detector = CreateDetector();
            CreateTarget(1.5f, 0.5f);
            CreateCollider("Interaction Detector Test Wall", 0.75f, 0.1f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.False);
            Assert.That(detector.CurrentInteractable, Is.Null);
        }

        [Test]
        public void RefreshDetection_InteractableBehindView_DoesNotBecomeTarget()
        {
            PlayerInteractionDetector detector = CreateDetector();
            CreateTarget(-1f, 0.5f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.False);
            Assert.That(detector.CurrentInteractable, Is.Null);
        }

        [Test]
        public void InteractionHud_SuppressionHidesPromptAndRejectsUiBehindModalOwners()
        {
            GameObject viewObject = new GameObject("Interaction HUD Suppression Test");
            var promptObject = new GameObject("Prompt");
            promptObject.transform.SetParent(viewObject.transform);
            Text prompt = promptObject.AddComponent<Text>();
            var feedbackObject = new GameObject("Feedback");
            feedbackObject.transform.SetParent(viewObject.transform);
            Text feedback = feedbackObject.AddComponent<Text>();
            InteractionPromptView view = viewObject.AddComponent<InteractionPromptView>();
            view.Configure(prompt, feedback);
            view.SetPrompt("조사하기");
            Assert.That(view.CurrentPrompt, Does.Contain("[E]"));

            view.SetSuppressed(true);
            view.SetPrompt("UI 뒤에서 실행되면 안 됨");
            view.ShowFeedback("UI 뒤 피드백");
            Assert.That(view.IsSuppressed, Is.True);
            Assert.That(view.CurrentPrompt, Is.Empty);
            Assert.That(promptObject.activeSelf, Is.False);
            Assert.That(feedbackObject.activeSelf, Is.False);

            view.SetSuppressed(false);
            view.SetPrompt("다시 조사하기");
            Assert.That(view.CurrentPrompt, Does.Contain("다시 조사하기"));
            Assert.That(promptObject.activeSelf, Is.True);
        }

        [Test]
        public void PromptSuppression_RepeatedlyCancelsFeedbackWithoutNullCoroutineFailure()
        {
            detectorObject = new GameObject("Interaction Detector Lifecycle Test");
            PlayerInteractionDetector detector = detectorObject.AddComponent<PlayerInteractionDetector>();
            GameObject viewObject = new GameObject("Interaction Lifecycle HUD");
            var promptObject = new GameObject("Prompt");
            promptObject.transform.SetParent(viewObject.transform);
            Text prompt = promptObject.AddComponent<Text>();
            var feedbackObject = new GameObject("Feedback");
            feedbackObject.transform.SetParent(viewObject.transform);
            Text feedback = feedbackObject.AddComponent<Text>();
            InteractionPromptView view = viewObject.AddComponent<InteractionPromptView>();
            view.Configure(prompt, feedback);
            view.ShowFeedback("첫 번째 피드백", 10f);
            Assert.DoesNotThrow(() => view.SetSuppressed(true));
            Assert.That(view.IsSuppressed, Is.True);
            Assert.That(feedbackObject.activeSelf, Is.False);

            view.SetSuppressed(false);
            Assert.That(view.IsSuppressed, Is.False);
            view.ShowFeedback("두 번째 피드백", 10f);
            Assert.DoesNotThrow(() => view.SetSuppressed(true),
                "Repeated suppression must not pass an invalid Coroutine handle to StopCoroutine.");
            Assert.That(feedbackObject.activeSelf, Is.False);
        }

        private PlayerInteractionDetector CreateDetector()
        {
            detectorObject = new GameObject("Interaction Detector Test Detector");
            detectorObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var detector = detectorObject.AddComponent<PlayerInteractionDetector>();
            detector.Configure(detectorObject.transform, 2f);
            return detector;
        }

        private static InspectableInteractable CreateTarget(float centerZ, float depth)
        {
            GameObject targetObject = CreateCollider("Interaction Detector Test Target", centerZ, depth);
            var target = targetObject.AddComponent<InspectableInteractable>();
            target.ConfigureInspection("Inspect", "Inspected");
            return target;
        }

        private static GameObject CreateCollider(string name, float centerZ, float depth)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = new Vector3(0f, 0f, centerZ);
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, depth);
            Physics.SyncTransforms();
            return gameObject;
        }
    }
}
