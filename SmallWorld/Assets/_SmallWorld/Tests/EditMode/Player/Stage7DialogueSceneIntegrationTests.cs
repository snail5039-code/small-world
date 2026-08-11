using NUnit.Framework;
using SmallWorld.Dialogue.Stage7;
using System;
using System.Reflection;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage7DialogueSceneIntegrationTests
    {
        [Test]
        public void RealityRoom_PreservesStage6AndAddsResponsiveDialogueOverlay()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GameObject stage6 = GameObject.Find("Stage 6 Reality Room UI");
            GameObject stage7 = GameObject.Find("Stage 7 Dialogue UI");

            Assert.That(stage6, Is.Not.Null);
            Assert.That(stage6.GetComponent<Stage6UIController>(), Is.Not.Null);
            Assert.That(stage6.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(stage7, Is.Not.Null);
            Assert.That(stage7.GetComponent<Stage7DialogueView>(), Is.Not.Null);
            var viewProperties = new SerializedObject(stage7.GetComponent<Stage7DialogueView>());
            Assert.That(viewProperties.FindProperty("stage6UI").objectReferenceValue,
                Is.SameAs(stage6.GetComponent<Stage6UIController>()));
            Type roomType = GetRealityRoomControllerType();
            Component room = Object.FindFirstObjectByType(roomType) as Component;
            Assert.That(room, Is.Not.Null);
            var roomProperties = new SerializedObject(room);
            Assert.That(roomProperties.FindProperty("dialogueView").objectReferenceValue,
                Is.SameAs(stage7.GetComponent<Stage7DialogueView>()));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            InteractableBase[] interactables = Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            Assert.That(Array.FindAll(interactables,
                item => item.GetType().FullName != "SmallWorld.Flow.FirstMemoryEntryInteractable" &&
                    item.GetType().FullName != "SmallWorld.Flow.StoryRouteEntryInteractable"), Has.Length.EqualTo(7));
            Assert.That(Array.FindAll(interactables,
                item => item.GetType().FullName == "SmallWorld.Flow.FirstMemoryEntryInteractable"), Has.Length.EqualTo(1));
            Assert.That(System.Array.FindAll(interactables,
                item => item.GetType().FullName == "SmallWorld.Flow.StoryRouteEntryInteractable"), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("Stage 10 Save Integration"), Is.Not.Null);
        }

        [Test]
        public void DemoDialogue_ChoicesBranchAndChangeRelationship()
        {
            DialogueDefinition definition = Stage7DemoDialogue.Create();
            var trustingState = new DialogueState();
            var trusting = new DialogueSession(definition, trustingState);
            trusting.Advance();
            trusting.SelectChoice("trust");

            var doubtfulState = new DialogueState();
            var doubtful = new DialogueSession(definition, doubtfulState);
            doubtful.Advance();
            doubtful.SelectChoice("doubt");

            Assert.That(trusting.Current.NodeId, Is.EqualTo("warm"));
            Assert.That(doubtful.Current.NodeId, Is.EqualTo("cold"));
            Assert.That(trustingState.Get(Stage7DemoDialogue.RelationshipKey), Is.EqualTo(2));
            Assert.That(doubtfulState.Get(Stage7DemoDialogue.RelationshipKey), Is.EqualTo(-1));
            Assert.That(trusting.History, Has.Count.EqualTo(4));
        }

        [Test]
        public void Escape_ClosesHistoryFirst_AndKeepsDialogueActive()
        {
            var root = new GameObject("Dialogue View Test");
            var dialogue = root.AddComponent<CanvasGroup>();
            var historyObject = new GameObject("History");
            var history = historyObject.AddComponent<CanvasGroup>();
            var view = root.AddComponent<Stage7DialogueView>();

            try
            {
                view.Configure(dialogue, null, null, null, null, null, null, null,
                    history, null, null, null, null);
                view.StartDialogue(Stage7DemoDialogue.Create());
                view.ShowHistory();

                Assert.That(view.IsHistoryVisible, Is.True);
                Assert.That(view.HandleEscape(), Is.True);
                Assert.That(view.IsHistoryVisible, Is.False);
                Assert.That(view.IsDialogueActive, Is.True);
                Assert.That(view.HandleEscape(), Is.True,
                    "Active dialogue must consume Escape instead of toggling pause or completing.");
                Assert.That(view.IsDialogueActive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(historyObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Dialogue_OwnsCursorAndPlayerUntilCompletion()
        {
            FirstPersonPlayerController player = CreatePlayer(out GameObject playerObject, out InputActionAsset actions);
            var root = new GameObject("Dialogue Input Contract");
            var dialogue = root.AddComponent<CanvasGroup>();
            var historyObject = new GameObject("History");
            var history = historyObject.AddComponent<CanvasGroup>();
            var view = root.AddComponent<Stage7DialogueView>();

            try
            {
                view.Configure(dialogue, null, null, null, null, null, null, null,
                    history, null, null, null, player);
                view.StartDialogue(Stage7DemoDialogue.Create());

                Assert.That(player.enabled, Is.False);
                Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(DialogueCursorMode.RequestedVisible, Is.True);

                view.Skip();
                view.SelectChoiceAt(0);
                view.Skip();

                Assert.That(view.IsDialogueActive, Is.False);
                Assert.That(player.enabled, Is.True);
                Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(DialogueCursorMode.RequestedVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(historyObject);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(actions);
            }
        }

        [Test]
        public void DialogueCompletion_DoesNotRestoreGameplayWhilePauseOwnsInput()
        {
            FirstPersonPlayerController player = CreatePlayer(out GameObject playerObject, out InputActionAsset actions);
            var stage6Object = new GameObject("Stage 6 State");
            var stage6 = stage6Object.AddComponent<Stage6UIController>();
            stage6.ConfigureInitialState(UIState.Paused);
            var root = new GameObject("Dialogue Nested Input Contract");
            var dialogue = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<Stage7DialogueView>();

            try
            {
                view.Configure(dialogue, null, null, null, null, null, null, null,
                    null, null, null, null, player, stage6);
                view.StartDialogue(Stage7DemoDialogue.Create());
                view.Skip();
                view.SelectChoiceAt(0);
                view.Skip();

                Assert.That(view.IsDialogueActive, Is.False);
                Assert.That(player.enabled, Is.False);
                Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(DialogueCursorMode.RequestedVisible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(stage6Object);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(actions);
            }
        }

        [Test]
        public void RealityRoomEscapePolicy_PrioritizesHistoryThenDialogueThenPause()
        {
            var stage6Object = new GameObject("Stage 6 Escape Policy");
            var stage6 = stage6Object.AddComponent<Stage6UIController>();
            stage6.ConfigureInitialState(UIState.Gameplay);
            var roomObject = new GameObject("Reality Room Escape Policy");
            Component room = roomObject.AddComponent(GetRealityRoomControllerType());
            Invoke(room, "ConfigureStage6", stage6, null, null, null, null, null);
            var dialogueObject = new GameObject("Dialogue Escape Policy");
            var dialogueGroup = dialogueObject.AddComponent<CanvasGroup>();
            var historyObject = new GameObject("History Escape Policy");
            var historyGroup = historyObject.AddComponent<CanvasGroup>();
            var view = dialogueObject.AddComponent<Stage7DialogueView>();

            try
            {
                view.Configure(dialogueGroup, null, null, null, null, null, null, null,
                    historyGroup, null, null, null, null, stage6);
                Invoke(room, "ConfigureStage7", view);
                view.StartDialogue(Stage7DemoDialogue.Create());
                view.ShowHistory();

                Assert.That(Invoke<bool>(room, "HandleEscapePressed"), Is.True);
                Assert.That(view.IsHistoryVisible, Is.False);
                Assert.That(view.IsDialogueActive, Is.True);
                Assert.That(stage6.StateMachine.Current, Is.EqualTo(UIState.Gameplay));

                Assert.That(Invoke<bool>(room, "HandleEscapePressed"), Is.True);
                Assert.That(view.IsDialogueActive, Is.True);
                Assert.That(stage6.StateMachine.Current, Is.EqualTo(UIState.Gameplay));

                view.Skip();
                view.SelectChoiceAt(0);
                view.Skip();
                Assert.That(view.IsDialogueActive, Is.False);

                Assert.That(Invoke<bool>(room, "HandleEscapePressed"), Is.True);
                Assert.That(stage6.StateMachine.Current, Is.EqualTo(UIState.Paused));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(historyObject);
                Object.DestroyImmediate(dialogueObject);
                Object.DestroyImmediate(roomObject);
                Object.DestroyImmediate(stage6Object);
            }
        }

        private static FirstPersonPlayerController CreatePlayer(out GameObject playerObject,
            out InputActionAsset actions)
        {
            playerObject = new GameObject("Player Input Contract");
            playerObject.SetActive(false);
            playerObject.AddComponent<CharacterController>();
            var player = playerObject.AddComponent<FirstPersonPlayerController>();
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = actions.AddActionMap("Player");
            map.AddAction("Move");
            map.AddAction("Look");
            map.AddAction("Sprint");
            map.AddAction("Jump");
            map.AddAction("Interact");
            player.Configure(null, actions, null, null, null);
            playerObject.SetActive(true);
            return player;
        }

        private static Type GetRealityRoomControllerType()
        {
            Type type = Type.GetType("SmallWorld.Flow.RealityRoomController, Assembly-CSharp");
            Assert.That(type, Is.Not.Null);
            return type;
        }

        private static void Invoke(Component target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static T Invoke<T>(Component target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(target, null);
        }
    }
}
