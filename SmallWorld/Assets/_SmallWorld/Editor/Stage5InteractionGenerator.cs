using System;
using SmallWorld.Core;
using SmallWorld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage5InteractionGenerator
    {
        private const string InteractionUiName = "Interaction UI";

        [MenuItem("Small World/Stage 5/Generate Interactions")]
        public static void GenerateFromMenu()
        {
            try
            {
                GenerateAndValidate();
                Debug.Log("[SmallWorld] Stage 5 interactions generated successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void GenerateFromBatchMode()
        {
            try
            {
                GenerateAndValidate();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void GenerateAndValidate()
        {
            string scenePath = SceneCatalog.GetPath(SceneId.RealityRoom);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemoveExistingInteractionComponents();

            InteractionPromptView view = CreatePromptView();
            PlayerInteractionDetector detector = UnityEngine.Object.FindFirstObjectByType<PlayerInteractionDetector>();
            if (detector == null) throw new InvalidOperationException("Player interaction detector is missing.");
            detector.ConfigureView(view);

            ConfigureDoor();
            ConfigureInspectable("Empty Frame", "액자 조사", "사진은 없다. 유리에는 방만 희미하게 비친다.", 0f);
            ConfigureInspectable("Midnight Clock", "시계 조사", "시곗바늘은 00:00에서 멈춰 있다.", 12f);
            ConfigureInspectable("Model House Table", "모형 집 조사", "작은 집의 구조가 이 방과 묘하게 닮았다.", 18f);
            ConfigurePickup();
            ConfigureComputer();
            Validate(view, detector);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException("Could not save Stage 5 interactions.");
        }

        private static void ConfigureDoor()
        {
            GameObject panel = GameObject.Find("Door");
            if (panel == null) throw new InvalidOperationException("Door is missing.");

            GameObject hingeObject = GameObject.Find("Door Hinge");
            if (hingeObject == null)
            {
                hingeObject = new GameObject("Door Hinge");
                hingeObject.transform.SetParent(panel.transform.parent, true);
                hingeObject.transform.position = new Vector3(-1.45f, 0f, -4.92f);
                panel.transform.SetParent(hingeObject.transform, true);
            }

            DoorInteractable door = hingeObject.AddComponent<DoorInteractable>();
            door.ConfigureDoor("문 열기", hingeObject.transform, -95f, 0.45f);
        }

        private static void ConfigureInspectable(string objectName, string prompt, string description, float rotation)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null) throw new InvalidOperationException($"Inspectable object is missing: {objectName}");
            InspectableInteractable interactable = target.AddComponent<InspectableInteractable>();
            interactable.ConfigureInspection(prompt, description, rotation == 0f ? null : target.transform, rotation);
        }

        private static void ConfigurePickup()
        {
            GameObject telephone = GameObject.Find("Old Telephone");
            if (telephone == null) throw new InvalidOperationException("Old Telephone is missing.");
            PickupInteractable pickup = telephone.AddComponent<PickupInteractable>();
            pickup.ConfigurePickup("전화기 줍기", "reality.old_phone", "낡은 전화기를 주웠다. 수화기 너머에서 숨소리가 들린다.");
        }

        private static void ConfigureComputer()
        {
            GameObject screen = GameObject.Find("Monitor Screen");
            GameObject glowObject = GameObject.Find("Monitor Glow");
            if (screen == null || glowObject == null) throw new InvalidOperationException("Computer interaction targets are missing.");
            ToggleUseInteractable use = screen.AddComponent<ToggleUseInteractable>();
            use.ConfigureUse("컴퓨터 사용", glowObject.GetComponent<Light>(),
                "화면이 켜졌다. '둘만의 작은 세계'가 이미 실행 중이다.", "화면을 껐다.");
        }

        private static InteractionPromptView CreatePromptView()
        {
            GameObject hud = GameObject.Find("Player HUD");
            if (hud == null) throw new InvalidOperationException("Player HUD is missing.");

            GameObject old = GameObject.Find(InteractionUiName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            var root = new GameObject(InteractionUiName, typeof(RectTransform));
            root.transform.SetParent(hud.transform, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            InteractionPromptView view = root.AddComponent<InteractionPromptView>();

            Text prompt = CreateText("Interaction Prompt", root.transform, new Vector2(0.5f, 0.33f), 25,
                TextAnchor.MiddleCenter, new Color(0.78f, 1f, 0.96f));
            prompt.rectTransform.sizeDelta = new Vector2(640f, 50f);
            Text feedback = CreateText("Interaction Feedback", root.transform, new Vector2(0.5f, 0.72f), 27,
                TextAnchor.MiddleCenter, Color.white);
            feedback.rectTransform.sizeDelta = new Vector2(820f, 120f);
            view.Configure(prompt, feedback);
            return view;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchor, int size,
            TextAnchor alignment, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            Text text = gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void RemoveExistingInteractionComponents()
        {
            foreach (InteractableBase component in UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(component);
            GameObject old = GameObject.Find(InteractionUiName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }

        private static void Validate(InteractionPromptView view, PlayerInteractionDetector detector)
        {
            InteractableBase[] targets = UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            if (targets.Length != 6) throw new InvalidOperationException($"Expected 6 interactables, found {targets.Length}.");
            if (view == null || detector == null) throw new InvalidOperationException("Interaction UI connection is incomplete.");
            foreach (InteractableBase target in targets)
            {
                if (target.GetComponentsInChildren<Collider>(true).Length == 0)
                    throw new InvalidOperationException($"Interactable has no collider: {target.name}");
                if (string.IsNullOrWhiteSpace(target.Prompt))
                    throw new InvalidOperationException($"Interactable has no prompt: {target.name}");
            }
        }
    }
}
