using System;
using System.Collections.Generic;
using System.IO;
using SmallWorld.Flow;
using SmallWorld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage15StoryRouteGenerator
    {
        private const string ScenePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private const string RealityRoomPath = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private static readonly string[] Ids = { "prologue", "chapter-1", "chapter-2", "chapter-3", "chapter-4", "chapter-5", "chapter-6" };
        private static readonly string[] Names = { "Prologue - The White Room", "Chapter 1 - The Fourth Place", "Chapter 2 - Last Platform", "Chapter 3 - A Perfect Day", "Chapter 4 - Faceless Office", "Chapter 5 - Cemetery Without a Funeral", "Chapter 6 - City in the Window" };

        [MenuItem("Small World/Stage 15/Generate Story Route Skeleton")]
        public static void Generate()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"Missing input actions at {InputPath}.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform player = CreatePlayer(actions);
            var routeRoot = new GameObject("Stage 15 Story Route");
            StoryRouteController route = routeRoot.AddComponent<StoryRouteController>();
            StoryRouteProgressAdapter progress = routeRoot.AddComponent<StoryRouteProgressAdapter>();
            StoryRouteNode[] nodes = CreateRoute(routeRoot.transform, route, progress);
            route.Configure(player, nodes);
            CreateLighting(routeRoot.transform);
            CreateFinalGate(routeRoot.transform, route, nodes.Length);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save Stage 15 story route.");
            IntegrateRealityRoom();
            AddBuildScene();
            AssetDatabase.SaveAssets();
        }

        private static StoryRouteNode[] CreateRoute(Transform root, StoryRouteController route, StoryRouteProgressAdapter progress)
        {
            var nodes = new StoryRouteNode[Ids.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                float z = i * 36f;
                var hub = new GameObject($"{i:00} {Names[i]}");
                hub.transform.SetParent(root, false);
                CreateBlock("Hub Floor", hub.transform, new Vector3(0f, -0.1f, z), new Vector3(30f, 0.2f, 32f));
                Transform arrival = new GameObject("Arrival").transform;
                arrival.SetParent(hub.transform, false);
                arrival.position = new Vector3(0f, 0.05f, z - 13f);
                Transform dialogue;
                Transform puzzle;
                Transform memory;
                if (i == 0)
                {
                    Transform[] anchors = CreateOpeningGameplay(hub.transform, progress, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else if (i == 1)
                {
                    Transform[] anchors = CreateFourthSeatGameplay(hub.transform, progress, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else
                {
                    dialogue = CreateMarker("Dialogue Entry", hub.transform, new Vector3(-4f, 0.75f, z), route, Ids[i], StoryRouteStep.Dialogue, "Inspect dialogue entry", $"{Names[i]} dialogue completed.");
                    puzzle = CreateMarker("Puzzle Entry", hub.transform, new Vector3(0f, 0.75f, z + 2f), route, Ids[i], StoryRouteStep.Puzzle, "Inspect puzzle entry", $"{Names[i]} puzzle completed.");
                    memory = CreateMarker("Memory Space Entry", hub.transform, new Vector3(4f, 0.75f, z), route, Ids[i], StoryRouteStep.Memory, "Inspect memory-space entry", $"{Names[i]} memory-space completed.");
                }
                nodes[i] = new StoryRouteNode { Id = Ids[i], DisplayName = Names[i], Arrival = arrival, DialogueEntry = dialogue, PuzzleEntry = puzzle, MemoryEntry = memory };
                if (i < nodes.Length - 1)
                {
                    GameObject gate = CreateBlock("Next Chapter Gate", hub.transform, new Vector3(0f, 1.25f, z + 15f), new Vector3(3f, 2.5f, 0.35f));
                    gate.AddComponent<StoryRouteInteractable>().ConfigureTravel(route, i + 1, $"Continue to {Names[i + 1]}");
                }
            }
            return nodes;
        }

        private static Transform[] CreateOpeningGameplay(Transform parent, StoryRouteProgressAdapter progress, float z)
        {
            OpeningStoryAction[] actions =
            {
                OpeningStoryAction.MeetYuna, OpeningStoryAction.PlaceSofa,
                OpeningStoryAction.FindKey, OpeningStoryAction.FindTeacup, OpeningStoryAction.FindPhotoFragment,
                OpeningStoryAction.ChooseStayPromise, OpeningStoryAction.ChooseEscapeFirst, OpeningStoryAction.ChooseUncertain,
                OpeningStoryAction.ReadScheduledMail, OpeningStoryAction.OpenMemoryDoor,
                OpeningStoryAction.QuestionMemoryDoor, OpeningStoryAction.LeaveMemoryDoorClosed
            };
            string[] prompts =
            {
                "화면 속 유나와 대화한다", "소파를 배치한다",
                "사진 속 열쇠를 찾는다", "사진 속 찻잔을 찾는다", "사진 조각을 찾는다",
                "계속 있겠다고 약속한다", "나갈 방법부터 찾겠다고 말한다", "아직 모르겠다고 답한다",
                "7년 전 예약 메일을 읽는다", "첫 기억 문을 연다", "유나에게 설명을 요구한다", "오늘은 문을 열지 않는다"
            };
            Transform[] created = CreateActionGrid(parent, progress, z, actions, prompts, PrimitiveType.Cube);
            CreateBlock("Empty Dollhouse", parent, new Vector3(11f, 1f, z - 10f), new Vector3(3f, 2f, 3f));
            CreateBlock("Placed Sofa Echo", parent, new Vector3(11f, 0.5f, z - 5f), new Vector3(3f, 1f, 1.2f));
            return new[] { created[0], created[1], created[9] };
        }

        private static Transform[] CreateFourthSeatGameplay(Transform parent, StoryRouteProgressAdapter progress, float z)
        {
            OpeningStoryAction[] actions =
            {
                OpeningStoryAction.HearFather, OpeningStoryAction.HearMother, OpeningStoryAction.HearChild,
                OpeningStoryAction.SetWrongClock, OpeningStoryAction.SetClock1942,
                OpeningStoryAction.AddBurntEgg, OpeningStoryAction.AddAppleHalf, OpeningStoryAction.AddColdSoup, OpeningStoryAction.AddEmptyBowl,
                OpeningStoryAction.ArrangePhoto1, OpeningStoryAction.ArrangePhoto2, OpeningStoryAction.ArrangePhoto3,
                OpeningStoryAction.ArrangePhoto4, OpeningStoryAction.ArrangePhoto5, OpeningStoryAction.ArrangePhoto6,
                OpeningStoryAction.OpenFalseDoor, OpeningStoryAction.OpenSilentDoor,
                OpeningStoryAction.SeatSeoyun, OpeningStoryAction.SeatPlayer, OpeningStoryAction.SeatYuna,
                OpeningStoryAction.RotateKitchenDoor, OpeningStoryAction.MoveSofa, OpeningStoryAction.TurnFrame,
                OpeningStoryAction.PullFrontDoor, OpeningStoryAction.ReturnHome
            };
            string[] prompts =
            {
                "신문 뒤 아버지의 증언을 듣는다", "어머니의 증언을 듣는다", "아이의 그림을 조사한다",
                "시계에 틀린 시각을 입력한다", "네 시계를 19:42로 맞춘다",
                "탄 달걀을 담는다", "반쪽 사과를 담는다", "식은 국을 담는다", "빈 밥그릇을 담는다",
                "첫 사진을 놓는다", "두 번째 사진을 놓는다", "세 번째 사진을 놓는다",
                "네 번째 사진을 놓는다", "다섯 번째 사진을 놓는다", "여섯 번째 사진을 놓는다",
                "가족 목소리가 나는 문을 연다", "청록색 흠집의 조용한 문을 연다",
                "서윤 이름표를 놓는다", "빈 이름표를 놓는다", "유나 이름표를 놓는다",
                "모형 집 부엌 문을 돌린다", "소파를 옮긴다", "액자를 뒤집는다",
                "현관문을 바깥으로 빼낸다", "하얀 거실로 돌아간다"
            };
            Transform[] created = CreateActionGrid(parent, progress, z, actions, prompts, PrimitiveType.Cylinder);
            CreateBlock("Four Seat Dining Table", parent, new Vector3(11f, 0.65f, z - 10f), new Vector3(4f, 1.3f, 3f));
            CreateBlock("Repeating Corridor", parent, new Vector3(11f, 1.5f, z), new Vector3(3f, 3f, 13f));
            return new[] { created[0], created[4], created[16] };
        }

        private static Transform[] CreateActionGrid(Transform parent, StoryRouteProgressAdapter progress, float z,
            OpeningStoryAction[] actions, string[] prompts, PrimitiveType primitive)
        {
            var created = new Transform[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                int column = i % 5;
                int row = i / 5;
                Vector3 position = new Vector3(-10f + column * 4f, 0.75f, z - 9f + row * 4f);
                GameObject station = GameObject.CreatePrimitive(primitive);
                station.name = $"{i + 1:00} {actions[i]}";
                station.transform.SetParent(parent, true);
                station.transform.position = position;
                station.transform.localScale = new Vector3(1.3f, 1.5f, 1.3f);
                station.AddComponent<Stage15StoryActionInteractable>().ConfigureAction(progress, actions[i], prompts[i]);
                created[i] = station.transform;
            }
            return created;
        }

        private static Transform CreateMarker(string name, Transform parent, Vector3 position,
            StoryRouteController route, string nodeId, StoryRouteStep step, string prompt, string feedback)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.2f, 0.75f, 1.2f);
            marker.AddComponent<StoryRouteInteractable>().ConfigureMarker(route, nodeId, step, prompt, feedback);
            return marker.transform;
        }

        private static void CreateFinalGate(Transform root, StoryRouteController route, int nodeCount)
        {
            float z = (nodeCount - 1) * 36f + 15f;
            GameObject gate = CreateBlock("Final Chapter Locked Gate", root, new Vector3(0f, 1.75f, z), new Vector3(5f, 3.5f, 0.5f));
            gate.AddComponent<StoryRouteInteractable>().ConfigureFinalGate(route, "Inspect the final chapter gate");
        }

        private static void IntegrateRealityRoom()
        {
            Scene scene = EditorSceneManager.OpenScene(RealityRoomPath, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("Stage 15 Story Route Entry");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            GameObject entry = CreateBlock("Stage 15 Story Route Entry", null, new Vector3(3.25f, 1f, -7.75f), new Vector3(1.5f, 2f, 0.25f));
            entry.AddComponent<StoryRouteEntryInteractable>().ConfigureEntry();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, RealityRoomPath)) throw new InvalidOperationException("Could not integrate the Stage 15 route entry.");
        }

        private static Transform CreatePlayer(InputActionAsset actions)
        {
            var playerObject = new GameObject("First Person Player");
            playerObject.transform.position = new Vector3(0f, 0.05f, -4.5f);
            CharacterController character = playerObject.AddComponent<CharacterController>();
            character.height = 1.8f; character.radius = 0.32f; character.center = new Vector3(0f, 0.9f, 0f);
            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 85f; camera.nearClipPlane = 0.05f; cameraObject.AddComponent<AudioListener>();
            AudioSource source = playerObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            PlayerFootstepEmitter footsteps = playerObject.AddComponent<PlayerFootstepEmitter>(); footsteps.Configure(source);
            PlayerInteractionDetector detector = playerObject.AddComponent<PlayerInteractionDetector>(); detector.Configure(camera.transform, 2.5f);
            Image crosshair = CreateHud(detector);
            playerObject.AddComponent<FirstPersonPlayerController>().Configure(camera, actions, detector, footsteps, crosshair);
            return playerObject.transform;
        }

        private static Image CreateHud(PlayerInteractionDetector detector)
        {
            var hud = new GameObject("Player HUD", typeof(RectTransform));
            Canvas canvas = hud.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var crosshairObject = new GameObject("Crosshair", typeof(RectTransform)); crosshairObject.transform.SetParent(hud.transform, false);
            RectTransform crosshairRect = (RectTransform)crosshairObject.transform; crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f); crosshairRect.sizeDelta = new Vector2(4f, 4f);
            Image crosshair = crosshairObject.AddComponent<Image>(); crosshair.color = new Color(1f, 1f, 1f, 0.8f); crosshair.raycastTarget = false;
            var ui = new GameObject("Interaction UI", typeof(RectTransform)); ui.transform.SetParent(hud.transform, false);
            RectTransform rect = (RectTransform)ui.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            InteractionPromptView view = ui.AddComponent<InteractionPromptView>(); view.Configure(CreateText("Prompt", ui.transform, 0.33f), CreateText("Feedback", ui.transform, 0.72f)); detector.ConfigureView(view);
            return crosshair;
        }

        private static Text CreateText(string name, Transform parent, float y)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, y); rect.sizeDelta = new Vector2(900f, 100f);
            Text text = go.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 24; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false; return text;
        }

        private static void CreateLighting(Transform root)
        {
            var lightObject = new GameObject("Route Directional Light"); lightObject.transform.SetParent(root, false); lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name; block.transform.SetParent(parent, true); block.transform.position = position; block.transform.localScale = scale; return block;
        }

        private static void AddBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
