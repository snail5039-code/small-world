using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmallWorld.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SmallWorld.Player.PlayMode.Tests
{
    public sealed class InteractionPromptLifecyclePlayModeTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private readonly List<string> exceptions = new List<string>();
        private GameObject fixture;

        [SetUp]
        public void SetUp()
        {
            exceptions.Clear();
            Application.logMessageReceived += CaptureException;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureException;
            if (fixture != null) Object.DestroyImmediate(fixture);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SuppressingAfterFeedbackHostWasDisabledDoesNotStopANullCoroutine()
        {
            fixture = new GameObject("Interaction Prompt Lifecycle Fixture");
            var promptObject = new GameObject("Prompt", typeof(RectTransform), typeof(Text));
            promptObject.transform.SetParent(fixture.transform, false);
            var feedbackObject = new GameObject("Feedback", typeof(RectTransform), typeof(Text));
            feedbackObject.transform.SetParent(fixture.transform, false);
            InteractionPromptView view = fixture.AddComponent<InteractionPromptView>();
            view.Configure(promptObject.GetComponent<Text>(), feedbackObject.GetComponent<Text>());

            view.ShowFeedback("장면 전환 직전 피드백", 30f);
            yield return null;
            fixture.SetActive(false);
            view.SetSuppressed(true);
            yield return null;

            Assert.That(view.IsSuppressed, Is.True);
            Assert.That(exceptions, Is.Empty,
                "InteractionPromptView.SetSuppressed must tolerate Unity cancelling its coroutine on disable.");
        }

        [UnityTest]
        public IEnumerator EnteringAndDisablingStoryRouteInteractionUiEmitsNoExceptions()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(StoryRouteScene, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            while (!load.isDone) yield return null;
            yield return null;

            InteractionPromptView[] views = Object.FindObjectsByType<InteractionPromptView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(views, Is.Not.Empty, "StoryRoute must expose its interaction prompt view.");
            InteractionPromptView active = views.FirstOrDefault(view => view.gameObject.activeInHierarchy);
            Assert.That(active, Is.Not.Null);
            active.ShowFeedback("StoryRoute lifecycle probe", 30f);
            yield return null;
            active.gameObject.SetActive(false);
            active.SetSuppressed(true);
            yield return null;

            Assert.That(exceptions, Is.Empty,
                "StoryRoute entry/interaction UI shutdown emitted a runtime exception:\n" +
                string.Join("\n", exceptions));
        }

        private void CaptureException(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
                exceptions.Add(condition + "\n" + stackTrace);
        }
    }
}
