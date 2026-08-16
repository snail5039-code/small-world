using System;
using System.Collections.Generic;
using System.IO;
using SmallWorld.Flow;
using SmallWorld.Player;
using SmallWorld.UI;
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
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private static readonly string[] Ids = { "prologue", "chapter-1", "chapter-2", "chapter-3", "chapter-4", "chapter-5", "chapter-6", "final-chapter" };
        private static readonly string[] Names = { "Prologue - The White Room", "Chapter 1 - The Fourth Place", "Chapter 2 - Last Platform", "Chapter 3 - A Perfect Day", "Chapter 4 - Faceless Office", "Chapter 5 - Cemetery Without a Funeral", "Chapter 6 - City in the Window", "Final Chapter - The White Room With Nothing Left" };
        private static readonly string[] WorldNames =
        {
            "프롤로그 · 하얀 방", "1장 · 네 번째 자리", "2장 · 마지막 승강장", "3장 · 완벽한 하루",
            "4장 · 얼굴 없는 사무실", "5장 · 장례식 없는 묘지", "6장 · 창문 안의 도시",
            "최종장 · 아무것도 남지 않은 하얀 방"
        };
        private static readonly Color[] FloorColors =
        {
            new Color(0.42f, 0.36f, 0.29f), new Color(0.24f, 0.29f, 0.38f),
            new Color(0.12f, 0.24f, 0.32f), new Color(0.54f, 0.36f, 0.24f),
            new Color(0.18f, 0.22f, 0.27f), new Color(0.19f, 0.25f, 0.22f),
            new Color(0.12f, 0.19f, 0.31f), new Color(0.68f, 0.69f, 0.72f)
        };
        private static readonly Color[] WallColors =
        {
            new Color(0.82f, 0.75f, 0.64f), new Color(0.35f, 0.4f, 0.51f),
            new Color(0.2f, 0.34f, 0.42f), new Color(0.75f, 0.58f, 0.4f),
            new Color(0.31f, 0.34f, 0.39f), new Color(0.31f, 0.38f, 0.33f),
            new Color(0.22f, 0.3f, 0.48f), new Color(0.9f, 0.9f, 0.92f)
        };
        private static readonly Color[] AccentColors =
        {
            new Color(1f, 0.55f, 0.25f), new Color(0.35f, 0.75f, 1f),
            new Color(0.15f, 0.85f, 1f), new Color(1f, 0.72f, 0.25f),
            new Color(0.25f, 0.95f, 0.8f), new Color(0.58f, 0.85f, 0.5f),
            new Color(0.62f, 0.48f, 1f), new Color(0.95f, 0.78f, 0.35f)
        };

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
            AddBuildScene();
            AssetDatabase.SaveAssets();
        }

        public static void GenerateFromBatchMode()
        {
            Generate();
        }

        private static StoryRouteNode[] CreateRoute(Transform root, StoryRouteController route, StoryRouteProgressAdapter progress)
        {
            var nodes = new StoryRouteNode[Ids.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                float z = i * 36f;
                var hub = new GameObject($"{i:00} {Names[i]}");
                hub.transform.SetParent(root, false);
                GameObject floor = CreateBlock("Hub Floor", hub.transform, new Vector3(0f, -0.1f, z), new Vector3(30f, 0.2f, 32f));
                ApplyMaterial(floor, CreateMaterial($"Route Room {i} Floor Material", FloorColors[i]));
                CreateRouteRoomEnvelope(hub.transform, i, z);
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
                else if (i == 2)
                {
                    Transform[] anchors = CreateLastPlatformGameplay(hub.transform, progress, route, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else if (i == 3)
                {
                    Transform[] anchors = CreatePerfectDayGameplay(hub.transform, progress, route, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else if (i == 4)
                {
                    Transform[] anchors = CreateFacelessOfficeGameplay(hub.transform, progress, route, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else if (i == 5)
                {
                    Transform[] anchors = CreateCemeteryWithoutFuneralGameplay(hub.transform, progress, route, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else if (i == 6)
                {
                    Transform[] anchors = CreateCityInTheWindowGameplay(hub.transform, progress, route, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                else
                {
                    Transform[] anchors = CreateFinalChapterGameplay(hub.transform, progress, route, z);
                    dialogue = anchors[0]; puzzle = anchors[1]; memory = anchors[2];
                }
                CreateRoomSetDressing(hub.transform, i, z);
                nodes[i] = new StoryRouteNode { Id = Ids[i], DisplayName = Names[i], Arrival = arrival, DialogueEntry = dialogue, PuzzleEntry = puzzle, MemoryEntry = memory };
                CreateRoomWayfinding(hub.transform, i, z, arrival, dialogue, puzzle, memory);
                if (i == 0)
                {
                    GameObject realityReturnGate = CreateBlock("Route Room 0 Reality Return Gate", hub.transform,
                        new Vector3(10.5f, 1.25f, z - 10.5f), new Vector3(2.2f, 2.5f, 0.3f));
                    realityReturnGate.AddComponent<StoryRouteRealityReturnInteractable>().ConfigureReturn(route,
                        "[E] 현실방으로 돌아가기");
                    CreateWorldLabel("Route Room 0 Reality Return Sign", hub.transform, "현실방으로 돌아가기",
                        new Vector3(10.5f, 3.35f, z - 10.5f), new Color(0.55f, 0.9f, 1f));
                }
                if (i > 0)
                {
                    GameObject previousGate = CreateBlock($"Route Room {i} Previous Room Gate", hub.transform,
                        new Vector3(-12.2f, 1.25f, z - 13.5f), new Vector3(2.2f, 2.5f, 0.3f));
                    previousGate.AddComponent<StoryRouteInteractable>().ConfigureTravel(route, i - 1,
                        $"[E] 이전 방으로 돌아가기: {Names[i - 1]}");
                }
                if (i < nodes.Length - 1)
                {
                    GameObject nextGate = CreateBlock($"Route Room {i} Next Room Gate", hub.transform,
                        new Vector3(12.2f, 1.25f, z + 13.5f), new Vector3(2.2f, 2.5f, 0.3f));
                    nextGate.AddComponent<StoryRouteInteractable>().ConfigureTravel(route, i + 1,
                        $"[E] 다음 방으로 이동하기: {Names[i + 1]}");
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
            CreateBlock("Empty Dollhouse", parent, new Vector3(9.5f, 1f, z - 8f), new Vector3(3f, 2f, 3f));
            CreateBlock("Placed Sofa Echo", parent, new Vector3(8.5f, 0.5f, z - 3f), new Vector3(3f, 1f, 1.2f));
            CreateBlock("Reserved Email Monitor", parent, new Vector3(11f, 1.2f, z), new Vector3(1.6f, 1.2f, 0.2f));
            CreateBlock("Loop 109 Display", parent, new Vector3(11f, 1.4f, z + 4f), new Vector3(1.8f, 0.8f, 0.2f));
            CreateWorldLabel("Prologue First Objective Label", parent, "유나 · 먼저 대화하기", new Vector3(5.5f, 4f, z - 6.5f), new Color(1f, 0.72f, 0.35f));
            CreatePointLight("Prologue Yuna Key Light", parent, new Vector3(5.5f, 3f, z - 6.5f), new Color(1f, 0.62f, 0.32f), 3.1f, 7f);
            CreatePointLight("Prologue Warm Light", parent, new Vector3(0f, 3.2f, z), new Color(1f, 0.78f, 0.58f), 2.6f, 15f);
            CreatePointLight("Prologue Route Fill Light", parent, new Vector3(0f, 3.1f, z - 6f),
                new Color(1f, 0.86f, 0.68f), 2.2f, 18f);
            for (int navigationIndex = 0; navigationIndex < 3; navigationIndex++)
            {
                CreatePointLight($"Prologue Navigation Light {navigationIndex + 1}", parent,
                    new Vector3(0f, 3.2f, z - 10f + navigationIndex * 10f),
                    new Color(1f, 0.9f, 0.76f), 2f, 14f);
            }
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
            CreateBlock("The Empty Fourth Chair", parent, new Vector3(11f, 0.75f, z - 7.5f), new Vector3(1.1f, 1.5f, 1.1f));
            CreateBlock("Manipulated Family Photo", parent, new Vector3(13f, 1.8f, z - 4f), new Vector3(0.2f, 2.1f, 2.8f));
            CreateBlock("Locked Seoyun Room", parent, new Vector3(9f, 1.5f, z + 5f), new Vector3(2.2f, 3f, 0.25f));
            CreateBlock("Basement Key Foreshadow", parent, new Vector3(12f, 0.45f, z - 2f), new Vector3(0.25f, 0.12f, 0.7f));
            CreateBlock("Nonexistent Room", parent, new Vector3(11f, 1.5f, z + 8f), new Vector3(2.2f, 3f, 0.25f));
            CreatePointLight("Rainy Apartment Light", parent, new Vector3(8f, 3.1f, z), new Color(0.48f, 0.62f, 1f), 1.8f, 16f);
            return new[] { created[0], created[4], created[16] };
        }

        private static Transform[] CreateLastPlatformGameplay(Transform parent, StoryRouteProgressAdapter progress, StoryRouteController route, float z)
        {
            CreateBlock("Last Platform Concourse", parent, new Vector3(0f, 0.05f, z), new Vector3(12f, 0.1f, 29f));
            CreateBlock("Track Bed", parent, new Vector3(9.5f, -0.45f, z), new Vector3(6f, 0.5f, 29f));
            CreateBlock("Near Rail", parent, new Vector3(7.8f, -0.05f, z), new Vector3(0.16f, 0.16f, 29f));
            CreateBlock("Far Rail", parent, new Vector3(11.2f, -0.05f, z), new Vector3(0.16f, 0.16f, 29f));
            CreateBlock("Platform Warning Edge", parent, new Vector3(6.2f, 0.12f, z), new Vector3(0.35f, 0.12f, 29f));
            CreateBlock("Tunnel Wall", parent, new Vector3(14.2f, 1.8f, z), new Vector3(0.35f, 3.6f, 30f));

            CreateBlock("Impossible Route Map", parent, new Vector3(-4.8f, 1.5f, z - 8.5f), new Vector3(0.25f, 2.6f, 4.8f));
            for (int i = 0; i < 4; i++)
                CreateBlock($"Torn Route Piece {i + 1}", parent, new Vector3(-4.55f, 0.65f + i * 0.48f, z - 10f + i), new Vector3(0.12f, 0.32f, 0.75f));
            CreateBlock("Deleted Passenger Display", parent, new Vector3(0f, 2.5f, z - 6f), new Vector3(5.5f, 1.1f, 0.25f));

            string[] lostItems = { "Employee Badge", "Child Shoe", "Hospital Wristband", "Game Cartridge" };
            for (int i = 0; i < lostItems.Length; i++)
            {
                float itemZ = z - 2.5f + i * 1.7f;
                CreateBlock(lostItems[i], parent, new Vector3(-4.4f, 0.35f, itemZ), new Vector3(0.65f, 0.25f, 0.65f));
                GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                shadow.name = $"Faceless Passenger Shadow {i + 1}";
                shadow.transform.SetParent(parent, true);
                shadow.transform.position = new Vector3(3.8f, 1f, itemZ);
                shadow.transform.localScale = new Vector3(0.65f, 1f, 0.65f);
            }

            CreateBlock("Reverse Broadcast Console", parent, new Vector3(-4.5f, 1f, z + 6f), new Vector3(1.8f, 2f, 1.2f));
            CreateBlock("Broadcast Warning Display", parent, new Vector3(-3.5f, 2.15f, z + 6f), new Vector3(0.2f, 1f, 4.5f));
            for (int i = 0; i < 3; i++)
            {
                float safeZ = z + 3f + i * 3f;
                CreateBlock($"Broadcast Safe Zone {i + 1}", parent, new Vector3(1.8f, 0.12f, safeZ), new Vector3(3.2f, 0.12f, 1.6f));
                CreatePointLight($"Broadcast Safe Light {i + 1}", parent, new Vector3(1.8f, 2.8f, safeZ), new Color(0.48f, 0.78f, 1f), 2f, 5f);
            }

            CreateBlock("Arriving Last Train", parent, new Vector3(10f, 1.25f, z + 7f), new Vector3(4.8f, 2.5f, 12f));
            CreateBlock("Open Train Door", parent, new Vector3(7.5f, 1.2f, z + 7f), new Vector3(0.2f, 2.1f, 2f));
            CreateBlock("Destination Reality Home", parent, new Vector3(-3.8f, 0.55f, z + 11f), new Vector3(1.5f, 1.1f, 1.5f));
            CreateBlock("Destination Game Home", parent, new Vector3(0f, 0.55f, z + 11f), new Vector3(1.5f, 1.1f, 1.5f));
            CreateBlock("Destination White Station", parent, new Vector3(3.8f, 0.55f, z + 11f), new Vector3(1.5f, 1.1f, 1.5f));

            CreateBlock("Reward Wall Clock", parent, new Vector3(-5f, 2f, z + 12.5f), new Vector3(0.25f, 1.1f, 1.1f));
            CreateBlock("Reward Shoe Cabinet", parent, new Vector3(-2.2f, 0.75f, z + 13f), new Vector3(2f, 1.5f, 0.8f));
            CreateBlock("Reward Small Radio", parent, new Vector3(0.5f, 0.45f, z + 13f), new Vector3(0.9f, 0.7f, 0.5f));

            Transform dialogue = CreateMarker("Dohyeon And Route Map", parent, new Vector3(-2.5f, 0.75f, z - 8.5f), route,
                "chapter-2", StoryRouteStep.Dialogue, "도현의 막차 기록과 접속 시간을 조사한다", "도현의 귀가 기록과 존재하지 않는 노선을 확인했다.");
            Transform puzzle = CreateMarker("Lost Property And Reverse Broadcast", parent, new Vector3(0f, 0.75f, z + 1f), route,
                "chapter-2", StoryRouteStep.Puzzle, "분실물을 돌려주고 역재생 안내방송을 복원한다", "귀가하지 마십시오. 집이 당신을 기억하고 있습니다.");
            Transform memory = CreateMarker("Choose The Last Destination", parent, new Vector3(0f, 0.75f, z + 10f), route,
                "chapter-2", StoryRouteStep.Memory, "현실 집, 게임 속 집, 하얀 역 중 목적지를 선택한다", "목적지가 기억되었다. 막차에서 안전 구역을 따라 빠져나간다.");
            Transform[] actions = CreateChapterActionSequence(parent, progress, z, 2,
                OpeningStoryAction.HearDohyeon, OpeningStoryAction.ReturnFromPlatform, PrimitiveType.Cylinder);
            return SelectActionAnchors(actions);
        }

        private static Transform[] CreateCemeteryWithoutFuneralGameplay(Transform parent, StoryRouteProgressAdapter progress, StoryRouteController route, float z)
        {
            CreateBlock("Fog Cemetery Ground", parent, new Vector3(0f, 0.05f, z), new Vector3(27f, 0.1f, 29f));
            for (int i = 0; i < 8; i++)
            {
                float x = -10f + (i % 4) * 4.2f;
                float graveZ = z - 10f + (i / 4) * 5f;
                CreateBlock($"Nameless Grave {i + 1}", parent, new Vector3(x, 0.8f, graveZ), new Vector3(1.5f, 1.6f, 0.35f));
            }
            CreatePointLight("Dense Fog Cemetery Light", parent, new Vector3(0f, 3f, z - 5f), new Color(0.48f, 0.58f, 0.62f), 1.2f, 25f);

            CreateBlock("Small Funeral Hall", parent, new Vector3(8.5f, 1.8f, z - 7f), new Vector3(8f, 3.6f, 8f));
            string[] causes = { "Traffic Accident", "Hospital Experiment", "Suicide", "Program Deletion" };
            for (int i = 0; i < causes.Length; i++)
            {
                float roomZ = z - 11f + i * 3f;
                CreateBlock($"Changing Cause Room {i + 1} - {causes[i]}", parent, new Vector3(8.5f, 1f, roomZ), new Vector3(5.5f, 2f, 2.4f));
                CreateBlock($"Funeral Photo {i + 1}", parent, new Vector3(11.35f, 1.45f, roomZ), new Vector3(0.15f, 1.4f, 1.1f));
                GameObject witness = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                witness.name = $"Distant Faceless Figure {i + 1}";
                witness.transform.SetParent(parent, true);
                witness.transform.position = new Vector3(6.4f, 1f, roomZ);
                witness.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            }

            for (int i = 0; i < 4; i++)
                CreateBlock($"Death Certificate {i + 1}", parent, new Vector3(-9f + i * 2.2f, 0.85f, z + 1f), new Vector3(1.5f, 0.12f, 2f));
            CreateBlock("Matching Letter Spacing And Print Error", parent, new Vector3(-5.7f, 1.5f, z + 3.2f), new Vector3(7.2f, 1.1f, 0.2f));
            CreateBlock("RESTORE HER Overlay Command", parent, new Vector3(-5.7f, 2.5f, z + 3.2f), new Vector3(5f, 0.8f, 0.25f));

            CreateBlock("Funeral Guestbook", parent, new Vector3(0f, 0.8f, z + 3f), new Vector3(2.8f, 0.2f, 2f));
            for (int i = 0; i < 4; i++)
            {
                CreateBlock($"Guestbook Signature {i + 1}", parent, new Vector3(-1.8f + i * 1.2f, 1.1f, z + 3f), new Vector3(0.8f, 0.08f, 0.25f));
                CreateBlock($"Cemetery Shadow Match {i + 1}", parent, new Vector3(-4.5f + i * 3f, 0.08f, z + 7f), new Vector3(1.1f, 0.05f, 2.8f));
            }
            CreateBlock("Same Hand Movement Proof", parent, new Vector3(0f, 1.5f, z + 6f), new Vector3(4.8f, 1f, 0.2f));

            CreateBlock("Empty Gravestone Front", parent, new Vector3(8f, 1.5f, z + 6f), new Vector3(3.2f, 3f, 0.45f));
            CreateBlock("Carve A Name Instruction", parent, new Vector3(8f, 2f, z + 5.7f), new Vector3(2.3f, 0.6f, 0.12f));
            CreateBlock("Empty Gravestone Back", parent, new Vector3(8f, 1.5f, z + 6.6f), new Vector3(3.2f, 3f, 0.2f));
            CreateBlock("Memory Installation Date", parent, new Vector3(8f, 1.5f, z + 6.8f), new Vector3(2.4f, 0.8f, 0.12f));

            CreateBlock("Final Empty Name Input", parent, new Vector3(4f, 1.2f, z + 11f), new Vector3(3.5f, 2f, 0.35f));
            CreateBlock("Confirm Blank Name Truth Branch", parent, new Vector3(1f, 0.7f, z + 12.5f), new Vector3(2.2f, 1.4f, 1.2f));
            CreateBlock("Entered Name Creates New Girl Loop Branch", parent, new Vector3(7f, 0.7f, z + 12.5f), new Vector3(2.2f, 1.4f, 1.2f));
            CreateBlock("Return Home From Cemetery", parent, new Vector3(10.5f, 1.4f, z + 12f), new Vector3(2.5f, 2.8f, 0.35f));
            CreateBlock("Chapter 6 City In The Window Connection", parent, new Vector3(12f, 1.2f, z + 9f), new Vector3(2f, 2.4f, 0.35f));

            CreateBlock("Reward Empty Picture Frame", parent, new Vector3(-11f, 1.4f, z + 12f), new Vector3(0.2f, 2f, 2.5f));
            CreateBlock("Reward Nameless Gravestone Fragment", parent, new Vector3(-8f, 0.45f, z + 12f), new Vector3(1.8f, 0.9f, 0.5f));
            CreateBlock("Reward White Flower Vase", parent, new Vector3(-5f, 0.65f, z + 12f), new Vector3(0.8f, 1.3f, 0.8f));

            Transform dialogue = CreateMarker("Investigate Changing Death Causes And Faceless Mourner", parent, new Vector3(4f, 0.75f, z - 8f), route,
                "chapter-5", StoryRouteStep.Dialogue, "안개 묘지와 장례식장에서 바뀌는 사인과 얼굴 없는 조문객을 조사한다", "사망 원인을 확정할수록 모순이 늘어난다.");
            Transform puzzle = CreateMarker("Prove All Funeral Memories False", parent, new Vector3(0f, 0.75f, z + 5f), route,
                "chapter-5", StoryRouteStep.Puzzle, "네 진단서의 오류를 겹치고 방명록 서명과 그림자를 연결한 뒤 묘비 뒷면을 조사한다", "RESTORE HER와 기억 설치 날짜가 모든 장례 기억이 거짓임을 증명했다.");
            Transform memory = CreateMarker("Confirm Blank Name Or Create Another Girl And Return Home", parent, new Vector3(4f, 0.75f, z + 10f), route,
                "chapter-5", StoryRouteStep.Memory, "이름을 비워 진실을 확인하거나 이름을 입력해 반복 분기로 간 뒤 집으로 돌아간다", "이름 선택이 기억되었다. 귀환 후 창문 안의 도시로 이어진다.");
            Transform[] actions = CreateChapterActionSequence(parent, progress, z, 5,
                OpeningStoryAction.TalkWithYunaBeforeCemetery, OpeningStoryAction.ReturnFromGravelessFuneral, PrimitiveType.Cylinder);
            return SelectActionAnchors(actions);
        }

        private static Transform[] CreatePerfectDayGameplay(Transform parent, StoryRouteProgressAdapter progress, StoryRouteController route, float z)
        {
            CreateBlock("Perfect Day Village Square", parent, new Vector3(0f, 0.05f, z), new Vector3(27f, 0.1f, 29f));

            CreateBlock("Warm Village Cafe", parent, new Vector3(-9f, 1.7f, z - 8f), new Vector3(7f, 3.4f, 6f));
            CreateBlock("Cafe Counter", parent, new Vector3(-6.2f, 0.65f, z - 8f), new Vector3(1.1f, 1.3f, 4.5f));
            CreateBlock("Menu Showing Her Favorites", parent, new Vector3(-5.55f, 1.55f, z - 8f), new Vector3(0.18f, 1.5f, 2.2f));
            CreateBlock("Flipped Menu Bitter Coffee", parent, new Vector3(-5.35f, 0.9f, z - 5.8f), new Vector3(0.12f, 0.9f, 1.4f));
            CreateBlock("Mina Bitter Coffee Cup", parent, new Vector3(-7f, 0.85f, z - 8f), new Vector3(0.55f, 0.3f, 0.55f));

            CreateBlock("Sunny Village Park", parent, new Vector3(0f, 0.15f, z - 7f), new Vector3(8f, 0.2f, 7f));
            CreateBlock("Park Bench", parent, new Vector3(0f, 0.6f, z - 8f), new Vector3(3.4f, 0.8f, 0.8f));
            CreateBlock("Choice Graffiti", parent, new Vector3(3.2f, 0.35f, z - 5f), new Vector3(2.2f, 0.12f, 0.8f));
            CreateBlock("Three Identical Answers", parent, new Vector3(0f, 1.4f, z - 4.5f), new Vector3(4.2f, 1.1f, 0.25f));
            CreateBlock("Fourth Choice I Do Not Know What You Like", parent, new Vector3(3.2f, 0.9f, z - 4.2f), new Vector3(2.5f, 0.9f, 0.25f));

            CreateBlock("Perfect Day Arcade", parent, new Vector3(9f, 1.7f, z - 8f), new Vector3(7f, 3.4f, 6f));
            for (int i = 0; i < 3; i++)
                CreateBlock($"Repeating Arcade Cabinet {i + 1}", parent, new Vector3(7f + i * 2f, 1f, z - 7f), new Vector3(1.2f, 2f, 1.2f));

            CreateBlock("Riverside Walk", parent, new Vector3(0f, 0.12f, z + 8f), new Vector3(22f, 0.18f, 5f));
            CreateBlock("Riverside Water", parent, new Vector3(0f, -0.15f, z + 12f), new Vector3(27f, 0.15f, 3f));
            for (int i = 0; i < 4; i++)
            {
                GameObject repeated = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                repeated.name = $"Repeated Person And Dialogue Mark {i + 1}";
                repeated.transform.SetParent(parent, true);
                repeated.transform.position = new Vector3(-9f + i * 6f, 1f, z + 7.5f);
            }

            for (int i = 0; i < 3; i++)
            {
                float stageZ = z - 1f + i * 2.2f;
                CreateBlock($"Movable Park Shadow Stage {i + 1}", parent, new Vector3(-3f + i * 3f, 0.14f, stageZ), new Vector3(4f, 0.08f, 0.45f));
                CreatePointLight($"Sunset Stage Light {i + 1}", parent, new Vector3(-5f + i * 5f, 3f - i * 0.45f, stageZ),
                    new Color(1f, 0.82f - i * 0.12f, 0.5f - i * 0.12f), 1.8f, 7f);
                CreateBlock($"Yuna Previous Loop Appearance {i + 1}", parent, new Vector3(4.5f, 1f, stageZ), new Vector3(0.8f, 2f, 0.8f));
            }
            CreateBlock("Evening Unlocked", parent, new Vector3(0f, 1.4f, z + 5f), new Vector3(5f, 2.8f, 0.3f));

            CreateBlock("Perfect Date Photo", parent, new Vector3(-2.2f, 1.2f, z + 10f), new Vector3(2.5f, 1.8f, 0.18f));
            CreateBlock("Preserve Photo Choice", parent, new Vector3(-5f, 0.65f, z + 11.5f), new Vector3(2.2f, 1.3f, 1.2f));
            CreateBlock("Tear Photo Choice", parent, new Vector3(0.6f, 0.65f, z + 11.5f), new Vector3(2.2f, 1.3f, 1.2f));
            CreateBlock("Mina Original Memory", parent, new Vector3(4.5f, 1.2f, z + 10f), new Vector3(2.5f, 2.4f, 0.3f));
            CreateBlock("Return Home Door", parent, new Vector3(8.5f, 1.5f, z + 10f), new Vector3(2.5f, 3f, 0.35f));

            Transform dialogue = CreateMarker("Mina Perfect Day Loop", parent, new Vector3(-3f, 0.75f, z - 10.5f), route,
                "chapter-3", StoryRouteStep.Dialogue, "반복되는 인물과 대사를 지나 민아의 완벽한 하루를 조사한다", "카페, 공원, 오락실, 강변에서 같은 하루가 반복되고 있다.");
            Transform puzzle = CreateMarker("Break The Perfect Day Rules", parent, new Vector3(0f, 0.75f, z + 2f), route,
                "chapter-3", StoryRouteStep.Puzzle, "메뉴를 뒤집고 낙서를 조사한 뒤 그림자를 석양까지 움직인다", "쓴 커피와 네 번째 대답이 반복을 깨뜨렸고 시간이 저녁으로 흐른다.");
            Transform memory = CreateMarker("Preserve Or Tear The Photo And Return Home", parent, new Vector3(5.5f, 0.75f, z + 11.5f), route,
                "chapter-3", StoryRouteStep.Memory, "완벽한 사진을 보존하거나 찢고 집으로 돌아간다", "사진에 대한 선택을 기억했다. 집 복귀 상호작용이 열렸다.");
            CreatePointLight("Perfect Day Warm Sun", parent, new Vector3(0f, 5f, z), new Color(1f, 0.76f, 0.45f), 2.4f, 28f);
            Transform[] actions = CreateChapterActionSequence(parent, progress, z, 3,
                OpeningStoryAction.TalkWithYunaAtHome, OpeningStoryAction.ReturnFromPerfectDay, PrimitiveType.Sphere);
            return SelectActionAnchors(actions);
        }

        private static Transform[] CreateFacelessOfficeGameplay(Transform parent, StoryRouteProgressAdapter progress, StoryRouteController route, float z)
        {
            CreateBlock("Windowless Developer Office", parent, new Vector3(0f, 0.05f, z), new Vector3(27f, 0.1f, 29f));
            CreateBlock("Windowless Office Ceiling", parent, new Vector3(0f, 3.2f, z), new Vector3(27f, 0.15f, 29f));
            CreateBlock("Windowless Office West Wall", parent, new Vector3(-13.4f, 1.6f, z), new Vector3(0.2f, 3.2f, 29f));
            CreateBlock("Windowless Office East Wall", parent, new Vector3(13.4f, 1.6f, z), new Vector3(0.2f, 3.2f, 29f));
            CreateBlock("Windowless Office North Wall", parent, new Vector3(0f, 1.6f, z + 14.4f), new Vector3(27f, 3.2f, 0.2f));
            CreateBlock("Windowless Office South Wall", parent, new Vector3(0f, 1.6f, z - 14.4f), new Vector3(27f, 3.2f, 0.2f));

            string[] versions = { "Prototype Girl", "Obedient Girl", "Remembering Girl", "Deleted Girl" };
            for (int i = 0; i < versions.Length; i++)
            {
                float deskX = -9f + i * 6f;
                CreateBlock($"Same Face Employee Desk {i + 1}", parent, new Vector3(deskX, 0.55f, z - 7f), new Vector3(3.8f, 1.1f, 2f));
                GameObject employee = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                employee.name = $"Same Face Employee {i + 1}";
                employee.transform.SetParent(parent, true);
                employee.transform.position = new Vector3(deskX, 1f, z - 9f);
                CreateBlock($"Girl Version Computer {i + 1} - {versions[i]}", parent,
                    new Vector3(deskX, 1.35f, z - 6.8f), new Vector3(1.6f, 1.1f, 0.25f));
            }

            CreateBlock("Employee Badge Authority Exchange", parent, new Vector3(-9f, 0.8f, z - 1.5f), new Vector3(2.2f, 1.6f, 1.4f));
            CreateBlock("Original Developer Badge", parent, new Vector3(-11f, 0.25f, z + 0.5f), new Vector3(0.7f, 0.12f, 1f));
            CreateBlock("Memory Researcher Badge", parent, new Vector3(-9f, 0.25f, z + 0.5f), new Vector3(0.7f, 0.12f, 1f));
            CreateBlock("System Administrator Badge", parent, new Vector3(-7f, 0.25f, z + 0.5f), new Vector3(0.7f, 0.12f, 1f));
            CreateBlock("Identity And Face Change Door", parent, new Vector3(-12f, 1.5f, z + 4f), new Vector3(0.35f, 3f, 3f));
            CreateBlock("Permission Locked Record Cabinet", parent, new Vector3(-9f, 1.2f, z + 4f), new Vector3(2.5f, 2.4f, 1f));

            for (int i = 0; i < 4; i++)
                CreateBlock($"Contradictory Deleted Log Fragment {i + 1}", parent,
                    new Vector3(-3f + i * 2f, 0.65f, z), new Vector3(1.3f, 1.1f, 0.2f));
            CreateBlock("Invariant System Command", parent, new Vector3(0f, 1.5f, z + 2f), new Vector3(5f, 1f, 0.25f));
            CreateBlock("Girl Deletion Record", parent, new Vector3(-3f, 1.3f, z + 4f), new Vector3(2.6f, 1.5f, 0.25f));
            CreateBlock("Girl Saved Into Developer Memory Record", parent, new Vector3(3f, 1.3f, z + 4f), new Vector3(2.6f, 1.5f, 0.25f));

            CreateBlock("Mirror Meeting Room", parent, new Vector3(8.5f, 1.6f, z + 1f), new Vector3(7f, 3.2f, 9f));
            CreateBlock("Mirror Showing Real Faces", parent, new Vector3(11.8f, 1.7f, z + 1f), new Vector3(0.18f, 2.7f, 7f));
            for (int i = 0; i < 4; i++)
            {
                CreateBlock($"Reality Employee Seat {i + 1}", parent, new Vector3(6.3f + (i % 2) * 3f, 0.45f, z - 1f + (i / 2) * 4f), new Vector3(1.2f, 0.9f, 1.2f));
                CreateBlock($"Mirror Real Face Seat {i + 1}", parent, new Vector3(11.55f, 0.7f + i * 0.5f, z - 1.5f + i), new Vector3(0.12f, 0.35f, 0.7f));
            }
            CreateBlock("Composite Identity Revelation", parent, new Vector3(8.5f, 2.3f, z + 4.6f), new Vector3(4.5f, 0.7f, 0.25f));

            CreateBlock("Trust Original Developer Record", parent, new Vector3(-5f, 0.65f, z + 9f), new Vector3(2.8f, 1.3f, 1.5f));
            CreateBlock("Trust Altered Developer Record", parent, new Vector3(0f, 0.65f, z + 9f), new Vector3(2.8f, 1.3f, 1.5f));
            CreateBlock("Check Original Server Autonomous Choice", parent, new Vector3(5f, 0.65f, z + 9f), new Vector3(2.8f, 1.3f, 1.5f));

            CreateBlock("End Of Shift Broadcast", parent, new Vector3(-9f, 2.4f, z + 11f), new Vector3(3f, 0.8f, 0.25f));
            CreateBlock("Erased Employee Faces Chase", parent, new Vector3(-4f, 1.2f, z + 12f), new Vector3(4f, 2.4f, 1.2f));
            CreateBlock("Badge Theft Chase Corridor", parent, new Vector3(2f, 0.12f, z + 12f), new Vector3(8f, 0.12f, 2f));
            CreateBlock("Office Escape Door", parent, new Vector3(7f, 1.5f, z + 12f), new Vector3(2.5f, 3f, 0.35f));
            CreateBlock("Return Home Interaction", parent, new Vector3(10.5f, 0.75f, z + 12f), new Vector3(2.2f, 1.5f, 1.5f));

            CreateBlock("Reward Study Desk", parent, new Vector3(-11f, 0.7f, z + 8f), new Vector3(3f, 1.4f, 1.5f));
            CreateBlock("Reward Development Computer", parent, new Vector3(-11f, 1.55f, z + 8f), new Vector3(1.6f, 1.1f, 0.25f));
            CreateBlock("Reward Locked File Cabinet", parent, new Vector3(-8f, 1.1f, z + 8f), new Vector3(1.6f, 2.2f, 1.4f));

            Transform dialogue = CreateMarker("Investigate Faceless Office Identities", parent, new Vector3(-6f, 0.75f, z - 4f), route,
                "chapter-4", StoryRouteStep.Dialogue, "동일한 얼굴의 직원과 서로 다른 소녀 버전을 조사한다", "창문 없는 개발사에서 이름과 얼굴이 사원증 권한에 따라 바뀐다.");
            Transform puzzle = CreateMarker("Exchange Badges Recover Logs And Match Mirror Seats", parent, new Vector3(0f, 0.75f, z + 5f), route,
                "chapter-4", StoryRouteStep.Puzzle, "사원증 권한을 교체하고 삭제 로그를 복구한 뒤 거울 속 자리를 맞춘다", "변하지 않는 명령과 실제 얼굴의 자리가 합성 인격을 드러냈다.");
            Transform memory = CreateMarker("Choose Developer Record Escape And Return Home", parent, new Vector3(5f, 0.75f, z + 11f), route,
                "chapter-4", StoryRouteStep.Memory, "세 기록 중 하나를 선택하고 사원증 추격을 피해 집으로 돌아간다", "개발자 기록 선택을 기억했다. 사무실을 탈출해 집 복귀 상호작용이 열렸다.");
            CreatePointLight("Faceless Office Fluorescent Light", parent, new Vector3(0f, 2.8f, z), new Color(0.7f, 0.86f, 1f), 2.1f, 28f);
            Transform[] actions = CreateChapterActionSequence(parent, progress, z, 4,
                OpeningStoryAction.TalkWithYunaBeforeOffice, OpeningStoryAction.ReturnFromFacelessOffice, PrimitiveType.Cube);
            return SelectActionAnchors(actions);
        }

        private static Transform[] CreateCityInTheWindowGameplay(Transform parent, StoryRouteProgressAdapter progress, StoryRouteController route, float z)
        {
            CreateBlock("Almost Complete Dollhouse Final Room", parent, new Vector3(0f, 1.8f, z), new Vector3(27f, 3.6f, 29f));
            CreateBlock("Scaled Reality City Basin", parent, new Vector3(0f, 0.15f, z), new Vector3(25f, 0.2f, 27f));

            for (int i = 0; i < 12; i++)
            {
                float x = -10f + (i % 4) * 6.6f;
                float cityZ = z - 10f + (i / 4) * 7.2f;
                float height = 1.8f + (i % 3) * 0.7f;
                CreateBlock($"Miniature City Building {i + 1}", parent, new Vector3(x, height * 0.5f, cityZ), new Vector3(4.2f, height, 3.8f));
                for (int window = 0; window < 4; window++)
                    CreateBlock($"Thousands Of Running Program Windows {i + 1}-{window + 1}", parent,
                        new Vector3(x - 1.35f + window * 0.9f, 0.9f + (window % 2) * 0.75f, cityZ - 1.95f),
                        new Vector3(0.5f, 0.45f, 0.08f));
            }

            CreateBlock("Repeated Time Clue", parent, new Vector3(-10f, 0.7f, z - 12f), new Vector3(2.2f, 1.4f, 0.3f));
            CreateBlock("Furniture Layout Clue", parent, new Vector3(-6.5f, 0.7f, z - 12f), new Vector3(2.2f, 1.4f, 0.3f));
            CreateBlock("Reverse Rain Direction Clue", parent, new Vector3(-3f, 0.7f, z - 12f), new Vector3(2.2f, 1.4f, 0.3f));
            CreateBlock("Reality Developer Room Candidate 1", parent, new Vector3(3f, 0.8f, z - 11f), new Vector3(3f, 1.6f, 2.4f));
            CreateBlock("Reality Developer Room Candidate 2", parent, new Vector3(7f, 0.8f, z - 11f), new Vector3(3f, 1.6f, 2.4f));
            CreateBlock("Reality Developer Room Correct", parent, new Vector3(11f, 0.8f, z - 11f), new Vector3(3f, 1.6f, 2.4f));

            for (int i = 0; i < 4; i++)
                CreateBlock($"Developer Monitor Sequence {i + 1}", parent,
                    new Vector3(-5.4f + i * 3.6f, 1.35f, z - 3f), new Vector3(2.6f, 1.6f, 0.25f));
            CreateBlock("Live Player Back View On Final Monitor", parent, new Vector3(5.4f, 1.35f, z - 2.7f), new Vector3(1.1f, 1.3f, 0.12f));
            GameObject playerBack = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerBack.name = "Player Back Silhouette";
            playerBack.transform.SetParent(parent, true);
            playerBack.transform.position = new Vector3(5.4f, 0.9f, z - 1.8f);
            playerBack.transform.localScale = new Vector3(0.5f, 0.9f, 0.35f);

            CreateBlock("Management AI Voice Waveform", parent, new Vector3(-4f, 1.5f, z + 2f), new Vector3(6f, 1f, 0.2f));
            CreateBlock("Girl Previous Dialogue Waveform", parent, new Vector3(4f, 1.5f, z + 2f), new Vector3(6f, 1f, 0.2f));
            CreateBlock("Perfectly Matching Future Girl Segment", parent, new Vector3(0f, 2.25f, z + 2f), new Vector3(4f, 0.45f, 0.25f));
            CreateBlock("Future Girl Management AI Revelation", parent, new Vector3(0f, 1f, z + 4f), new Vector3(6f, 1.4f, 0.3f));

            CreateBlock("Reality Link Maintain Developer Body", parent, new Vector3(-6f, 0.65f, z + 7f), new Vector3(4f, 1.3f, 1.8f));
            CreateBlock("Reality Link Cut Some Cables", parent, new Vector3(0f, 0.65f, z + 7f), new Vector3(4f, 1.3f, 1.8f));
            CreateBlock("Reality Link Cut Entire City Power", parent, new Vector3(6f, 0.65f, z + 7f), new Vector3(4f, 1.3f, 1.8f));

            CreateBlock("All City Windows Open Simultaneously", parent, new Vector3(-9f, 1.6f, z + 10f), new Vector3(4f, 3.2f, 0.3f));
            CreateBlock("All Miniature People Stare At Player", parent, new Vector3(-4f, 1.2f, z + 10f), new Vector3(4f, 2.4f, 1.2f));
            CreateBlock("Folding Buildings Form Giant House", parent, new Vector3(1f, 2f, z + 10f), new Vector3(5f, 4f, 3f));
            CreateBlock("Carry Collapsing City Chase", parent, new Vector3(6f, 0.2f, z + 10f), new Vector3(5f, 0.2f, 3f));
            CreateBlock("Return To Original House Door", parent, new Vector3(10.5f, 1.5f, z + 11f), new Vector3(2.5f, 3f, 0.35f));

            CreateBlock("Reward Completed Miniature City", parent, new Vector3(-8f, 0.55f, z + 13f), new Vector3(3f, 1.1f, 2.2f));
            CreateBlock("Reward Reality Developer Stopped Wristwatch", parent, new Vector3(-3f, 0.35f, z + 13f), new Vector3(1.2f, 0.2f, 1.2f));
            CreateBlock("Reward Final Room Front Door", parent, new Vector3(1f, 1.4f, z + 13f), new Vector3(2.2f, 2.8f, 0.35f));
            CreateBlock("Final Chapter Living House Connection", parent, new Vector3(4.5f, 1.2f, z + 13f), new Vector3(2.2f, 2.4f, 0.35f));
            CreateBlock("Final Chapter Management AI Core Connection", parent, new Vector3(7.5f, 1.2f, z + 13f), new Vector3(2.2f, 2.4f, 0.35f));

            Transform dialogue = CreateMarker("Find Reality Developer Room Among Thousands Of Windows", parent,
                new Vector3(-7f, 0.75f, z - 8f), route, "chapter-6", StoryRouteStep.Dialogue,
                "반복 시간, 가구 배치, 비의 방향으로 현실 개발자의 방을 찾는다", "수천 창문 중 현실 개발자의 방을 특정했다.");
            Transform puzzle = CreateMarker("Arrange Monitors And Match AI Girl Waveforms", parent,
                new Vector3(0f, 0.75f, z + 1f), route, "chapter-6", StoryRouteStep.Puzzle,
                "모니터를 반복 순서로 배열하고 관리 AI와 소녀의 파형을 겹친다", "마지막 모니터의 뒷모습과 일치 파형이 미래의 소녀를 드러냈다.");
            Transform memory = CreateMarker("Choose Reality Link Carry City And Return Home", parent,
                new Vector3(5f, 0.75f, z + 8.5f), route, "chapter-6", StoryRouteStep.Memory,
                "현실 연결을 선택하고 접히는 건물에서 무너지는 도시를 들고 귀환한다", "현실 연결 선택과 세 가구가 최종장으로 이어진다.");
            CreatePointLight("City In The Window Night Light", parent, new Vector3(0f, 4f, z), new Color(0.42f, 0.62f, 1f), 2.2f, 30f);
            Transform[] actions = CreateChapterActionSequence(parent, progress, z, 6,
                OpeningStoryAction.EnterWindowCityLastRoom, OpeningStoryAction.ReturnFromWindowCity, PrimitiveType.Cube);
            return SelectActionAnchors(actions);
        }

        private static Transform[] CreateFinalChapterGameplay(Transform parent, StoryRouteProgressAdapter progress, StoryRouteController route, float z)
        {
            CreateBlock("Living House Floor", parent, new Vector3(0f, 0.05f, z - 9f), new Vector3(27f, 0.1f, 11f));
            CreateBlock("Living House Wall Of Faces", parent, new Vector3(0f, 2f, z - 14f), new Vector3(27f, 4f, 0.35f));
            for (int i = 0; i < 6; i++)
            {
                float x = -10f + i * 4f;
                GameObject face = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                face.name = $"Living House Victim Face {i + 1}";
                face.transform.SetParent(parent, true);
                face.transform.position = new Vector3(x, 2.25f, z - 13.7f);
                face.transform.localScale = new Vector3(1.25f, 1.55f, 0.45f);
                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                hand.name = $"Living House Reaching Hand {i + 1}";
                hand.transform.SetParent(parent, true);
                hand.transform.position = new Vector3(x + 1f, 0.8f, z - 11f);
                hand.transform.rotation = Quaternion.Euler(0f, 0f, 75f);
                hand.transform.localScale = new Vector3(0.35f, 0.85f, 0.35f);
            }
            CreateBlock("Living Memory Furniture", parent, new Vector3(-5f, 0.7f, z - 8f), new Vector3(4f, 1.4f, 2f));
            CreateBlock("Memory Furniture Preserve Marker", parent, new Vector3(-7f, 0.3f, z - 5.5f), new Vector3(2.5f, 0.2f, 1.2f));
            CreateBlock("Memory Furniture Destroy Marker", parent, new Vector3(-3f, 0.3f, z - 5.5f), new Vector3(2.5f, 0.2f, 1.2f));

            string[] memoryCores =
            {
                "Fourth Place", "Last Platform", "Perfect Day", "Faceless Office",
                "Cemetery Without A Funeral", "City In The Window"
            };
            for (int i = 0; i < memoryCores.Length; i++)
            {
                float x = -10f + (i % 3) * 10f;
                float coreZ = z - 2f + (i / 3) * 4f;
                GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.name = $"Management AI Core - {memoryCores[i]}";
                core.transform.SetParent(parent, true);
                core.transform.position = new Vector3(x, 1.2f, coreZ);
                core.transform.localScale = Vector3.one * 1.5f;
                CreateBlock($"Core Memory Merge Indicator {i + 1}", parent,
                    new Vector3(x - 1.6f, 0.3f, coreZ), new Vector3(1.5f, 0.2f, 0.8f));
                CreateBlock($"Core Victim Loss Indicator {i + 1}", parent,
                    new Vector3(x + 1.6f, 0.3f, coreZ), new Vector3(1.5f, 0.2f, 0.8f));
            }

            CreateBlock("Reality Developer Body Silhouette", parent, new Vector3(-7f, 1f, z + 7f), new Vector3(2f, 2f, 1f));
            for (int i = 0; i < 3; i++)
                CreateBlock($"Reality Connection Cable {i + 1}", parent,
                    new Vector3(-4f + i * 4f, 0.45f, z + 7f), new Vector3(3.2f, 0.25f, 0.25f));
            CreateBlock("Cable State Maintained", parent, new Vector3(5f, 0.35f, z + 5.5f), new Vector3(3f, 0.25f, 0.8f));
            CreateBlock("Cable State Partially Cut", parent, new Vector3(5f, 0.35f, z + 7f), new Vector3(3f, 0.25f, 0.8f));
            CreateBlock("Cable State City Power Cut", parent, new Vector3(5f, 0.35f, z + 8.5f), new Vector3(3f, 0.25f, 0.8f));

            CreateBlock("Original White Room Floor", parent, new Vector3(0f, 0.05f, z + 12f), new Vector3(18f, 0.1f, 8f));
            CreateBlock("First White Room Chair - Player", parent, new Vector3(-3f, 0.75f, z + 12f), new Vector3(1.5f, 1.5f, 1.5f));
            CreateBlock("First White Room Chair - Opposite", parent, new Vector3(3f, 0.75f, z + 12f), new Vector3(1.5f, 1.5f, 1.5f));
            CreateBlock("Old Computer", parent, new Vector3(0f, 1f, z + 14f), new Vector3(2.6f, 2f, 0.8f));
            string[] forms = { "Girl Form", "Girl Developer Overlap", "Reality Developer Form" };
            for (int i = 0; i < forms.Length; i++)
            {
                GameObject form = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                form.name = $"Dialogue Transformation Stage {i + 1} - {forms[i]}";
                form.transform.SetParent(parent, true);
                form.transform.position = new Vector3(7f + i * 2f, 1f, z + 12f);
                form.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            }

            CreateFinalReadinessPanel(parent, z + 16f);
            CreateBlock("Final Choice Preparation Gate - Locked", parent,
                new Vector3(0f, 1.75f, z + 17f), new Vector3(6f, 3.5f, 0.5f));
            CreateBlock("No Ending Execution Boundary", parent,
                new Vector3(0f, 0.25f, z + 18f), new Vector3(8f, 0.2f, 1f));

            Transform dialogue = CreateMarker("Survive Living House And Review Memory Preservation", parent,
                new Vector3(-8f, 0.75f, z - 7f), route, "final-chapter", StoryRouteStep.Dialogue,
                "살아 있는 집의 기억 가구와 보존·파괴 흔적을 확인한다", "희생자의 얼굴과 손이 깨어난 집을 지나 기억 상태를 확인했다.");
            Transform puzzle = CreateMarker("Review Management Cores And Reality Cable State", parent,
                new Vector3(0f, 0.75f, z + 7f), route, "final-chapter", StoryRouteStep.Puzzle,
                "기억 공간별 관리 핵심과 현실 케이블 상태를 확인한다", "관리 핵심과 현실 연결 상태가 최종 조건 표시에 반영되었다.");
            Transform memory = CreateMarker("Complete White Room Transformation Dialogue", parent,
                new Vector3(0f, 0.75f, z + 13f), route, "final-chapter", StoryRouteStep.Memory,
                "두 의자와 낡은 컴퓨터 앞에서 소녀의 변화 대화를 끝낸다", "소녀가 현실 개발자의 모습으로 바뀌었고 최종 선택 준비 상태만 열렸다.");
            CreatePointLight("Final Chapter White Room Light", parent, new Vector3(0f, 4f, z + 10f), Color.white, 2.5f, 28f);
            Transform[] actions = CreateChapterActionSequence(parent, progress, z, 7,
                OpeningStoryAction.EnterLivingHouse, OpeningStoryAction.PrepareFinalChoice, PrimitiveType.Sphere);
            return SelectActionAnchors(actions);
        }

        private static void CreateFinalReadinessPanel(Transform parent, float z)
        {
            var panel = new GameObject("Final Choice Readiness Conditions UI", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.transform.position = new Vector3(0f, 2.8f, z);
            Canvas canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.sizeDelta = new Vector2(720f, 320f);
            panelRect.localScale = Vector3.one * 0.008f;
            Text conditions = CreateText("Readiness Conditions - Memories Nameplate Autonomy Reality Link", panel.transform, 0.5f);
            conditions.text = "최종 선택 준비\n보존된 기억 / 이름표 / 자율성 / 현실 연결\n희생자 복원 / 개발자 생존 / 소녀의 정체\n이 장면에서는 선택과 엔딩을 실행하지 않습니다";
            conditions.fontSize = 30;
            conditions.rectTransform.sizeDelta = new Vector2(700f, 300f);
        }

        private static Transform[] CreateChapterActionSequence(Transform parent, StoryRouteProgressAdapter progress,
            float z, int chapterIndex, OpeningStoryAction first, OpeningStoryAction last, PrimitiveType primitive)
        {
            int firstValue = (int)first;
            int count = (int)last - firstValue + 1;
            var created = new Transform[count];
            Color accent = AccentColors[chapterIndex];
            Material stationMaterial = CreateMaterial($"Route Room {chapterIndex} Action Material",
                Color.Lerp(FloorColors[chapterIndex], accent, 0.42f), accent * 0.35f);

            for (int i = 0; i < count; i++)
            {
                var action = (OpeningStoryAction)(firstValue + i);
                int row = i / 2;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 position = new Vector3(side * 11.5f, 0.45f, z - 11.5f + row * 1.8f);
                SemanticActionVisual visual = GetSemanticActionVisual(action, primitive);
                GameObject station = GameObject.CreatePrimitive(visual.Primitive);
                station.name = $"{visual.Label} - {action}";
                station.transform.SetParent(parent, true);
                station.transform.position = position;
                station.transform.localScale = visual.Scale;
                ApplyMaterial(station, stationMaterial);
                station.AddComponent<Stage15StoryActionInteractable>().ConfigureAction(progress, action,
                    "조사하기: " + KoreanPropPrompt(action));

                created[i] = station.transform;
            }

            return created;
        }

        private static Transform[] SelectActionAnchors(Transform[] actions)
        {
            if (actions == null || actions.Length == 0)
                throw new InvalidOperationException("A story chapter must expose at least one playable action.");
            return new[] { actions[0], actions[actions.Length / 2], actions[actions.Length - 1] };
        }

        private static void CreateRoomSetDressing(Transform parent, int room, float z)
        {
            Color wood = Color.Lerp(FloorColors[room], new Color(0.28f, 0.16f, 0.09f), 0.48f);
            Color cloth = Color.Lerp(WallColors[room], AccentColors[room], 0.22f);
            Color metal = Color.Lerp(WallColors[room], new Color(0.16f, 0.18f, 0.21f), 0.55f);
            Material woodMaterial = CreateMaterial($"Route Room {room} Wood Material", wood);
            Material clothMaterial = CreateMaterial($"Route Room {room} Cloth Material", cloth);
            Material metalMaterial = CreateMaterial($"Route Room {room} Metal Material", metal);

            // A real doorway silhouette establishes scale without blocking the two-metre central aisle.
            DecorBlock($"Route Room {room} Entry Door Left", parent, new Vector3(-2.25f, 1.55f, z - 10.2f),
                new Vector3(0.28f, 3.1f, 0.32f), woodMaterial);
            DecorBlock($"Route Room {room} Entry Door Right", parent, new Vector3(2.25f, 1.55f, z - 10.2f),
                new Vector3(0.28f, 3.1f, 0.32f), woodMaterial);
            DecorBlock($"Route Room {room} Entry Door Lintel", parent, new Vector3(0f, 3.15f, z - 10.2f),
                new Vector3(4.8f, 0.28f, 0.32f), woodMaterial);

            switch (room)
            {
                case 0:
                    CreateLivingRoomCluster(parent, z, woodMaterial, clothMaterial);
                    break;
                case 1:
                    CreateApartmentCluster(parent, z, woodMaterial, metalMaterial);
                    break;
                case 2:
                    CreateStationCluster(parent, z, metalMaterial, clothMaterial);
                    break;
                case 3:
                    CreateCafeCluster(parent, z, woodMaterial, clothMaterial);
                    break;
                case 4:
                    CreateOfficeCluster(parent, z, metalMaterial, clothMaterial);
                    break;
                case 5:
                    CreateCemeteryCluster(parent, z, woodMaterial, metalMaterial);
                    break;
                case 6:
                    CreateWindowCityCluster(parent, z, metalMaterial, clothMaterial);
                    break;
                default:
                    CreateFinalRoomCluster(parent, z, woodMaterial, clothMaterial);
                    break;
            }
        }

        private static void CreateLivingRoomCluster(Transform parent, float z, Material wood, Material cloth)
        {
            DecorBlock("Prologue Living Rug", parent, new Vector3(0f, 0.025f, z - 1f), new Vector3(7f, 0.04f, 5f), cloth, false);
            DecorBlock("Prologue Family Sofa", parent, new Vector3(-6.8f, 0.65f, z - 1f), new Vector3(3.6f, 1.3f, 1.25f), cloth);
            DecorBlock("Prologue Coffee Table", parent, new Vector3(-3.5f, 0.45f, z - 1f), new Vector3(2.3f, 0.18f, 1.4f), wood);
            for (int leg = 0; leg < 4; leg++)
                DecorBlock($"Prologue Coffee Table Leg {leg + 1}", parent,
                    new Vector3(-4.35f + (leg % 2) * 1.7f, 0.22f, z - 1.5f + (leg / 2) * 1f),
                    new Vector3(0.14f, 0.45f, 0.14f), wood);
            DecorBlock("Prologue Bookshelf", parent, new Vector3(8.8f, 1.5f, z + 3.5f), new Vector3(2.6f, 3f, 0.55f), wood);
            DecorBlock("Prologue Floor Lamp", parent, new Vector3(-8.8f, 1.25f, z - 4.5f), new Vector3(0.16f, 2.5f, 0.16f), wood);
        }

        private static void CreateApartmentCluster(Transform parent, float z, Material wood, Material metal)
        {
            DecorBlock("Chapter 1 Kitchen Counter", parent, new Vector3(-7.5f, 0.9f, z + 1f), new Vector3(5f, 1.8f, 1.2f), wood);
            for (int cabinet = 0; cabinet < 3; cabinet++)
                DecorBlock($"Chapter 1 Wall Cabinet {cabinet + 1}", parent,
                    new Vector3(-9f + cabinet * 1.45f, 2.8f, z + 1.35f), new Vector3(1.2f, 1.1f, 0.45f), wood);
            DecorBlock("Chapter 1 Hall Divider", parent, new Vector3(7.8f, 1.5f, z + 4f), new Vector3(0.28f, 3f, 7f), metal);
        }

        private static void CreateStationCluster(Transform parent, float z, Material metal, Material cloth)
        {
            float[] benchPositions = { -8f, -4.5f, 7.5f };
            for (int bench = 0; bench < 3; bench++)
                DecorBlock($"Chapter 2 Platform Bench {bench + 1}", parent,
                    new Vector3(benchPositions[bench], 0.55f, z - 2f), new Vector3(2.6f, 0.5f, 0.75f), cloth);
            for (int column = 0; column < 4; column++)
                DecorBlock($"Chapter 2 Station Column {column + 1}", parent,
                    new Vector3(column < 2 ? -6.5f : 6.5f, 2f, z - 7f + (column % 2) * 14f),
                    new Vector3(0.55f, 4f, 0.55f), metal);
        }

        private static void CreateCafeCluster(Transform parent, float z, Material wood, Material cloth)
        {
            DecorBlock("Chapter 3 Cafe Counter", parent, new Vector3(-7.5f, 0.9f, z + 3f), new Vector3(5.5f, 1.8f, 1.2f), wood);
            for (int table = 0; table < 3; table++)
            {
                float tableZ = z - 5f + table * 4.5f;
                DecorBlock($"Chapter 3 Cafe Table {table + 1}", parent, new Vector3(5.8f, 0.65f, tableZ), new Vector3(2f, 0.18f, 2f), wood);
                DecorBlock($"Chapter 3 Cafe Seat {table + 1}", parent, new Vector3(8f, 0.55f, tableZ), new Vector3(0.9f, 1.1f, 0.9f), cloth);
            }
        }

        private static void CreateOfficeCluster(Transform parent, float z, Material metal, Material cloth)
        {
            for (int desk = 0; desk < 4; desk++)
            {
                float x = desk < 2 ? -6f : 6f;
                float deskZ = z - 5f + (desk % 2) * 8f;
                DecorBlock($"Chapter 4 Work Desk {desk + 1}", parent, new Vector3(x, 0.7f, deskZ), new Vector3(3.5f, 0.18f, 1.6f), metal);
                DecorBlock($"Chapter 4 Desk Monitor {desk + 1}", parent, new Vector3(x, 1.45f, deskZ), new Vector3(1.4f, 0.85f, 0.16f), cloth);
            }
            DecorBlock("Chapter 4 Filing Wall", parent, new Vector3(-9f, 1.4f, z + 7f), new Vector3(3.2f, 2.8f, 0.75f), metal);
        }

        private static void CreateCemeteryCluster(Transform parent, float z, Material wood, Material stone)
        {
            for (int stoneIndex = 0; stoneIndex < 7; stoneIndex++)
                DecorBlock($"Chapter 5 Procession Stone {stoneIndex + 1}", parent,
                    new Vector3((stoneIndex % 2 == 0 ? -1f : 1f) * 1.45f, 0.035f, z - 8f + stoneIndex * 2.5f),
                    new Vector3(1.15f, 0.06f, 0.8f), stone, false);
            DecorBlock("Chapter 5 Chapel Bench Left", parent, new Vector3(-6f, 0.5f, z + 8f), new Vector3(4f, 1f, 0.75f), wood);
            DecorBlock("Chapter 5 Chapel Bench Right", parent, new Vector3(6f, 0.5f, z + 8f), new Vector3(4f, 1f, 0.75f), wood);
        }

        private static void CreateWindowCityCluster(Transform parent, float z, Material metal, Material glow)
        {
            DecorBlock("Chapter 6 Observation Console", parent, new Vector3(0f, 0.9f, z + 7f), new Vector3(6f, 1.8f, 1.2f), metal);
            for (int window = 0; window < 6; window++)
            {
                float x = -10f + window * 4f;
                DecorBlock($"Chapter 6 Interior Window {window + 1}", parent, new Vector3(x, 2.5f, z + 12f), new Vector3(2.7f, 2.5f, 0.18f), glow, false);
            }
        }

        private static void CreateFinalRoomCluster(Transform parent, float z, Material wood, Material cloth)
        {
            for (int rib = 0; rib < 6; rib++)
            {
                float x = -10f + rib * 4f;
                DecorBlock($"Final Living House Rib {rib + 1}", parent, new Vector3(x, 2.2f, z - 2f), new Vector3(0.35f, 4.4f, 0.5f), wood);
            }
            DecorBlock("Final White Room Carpet", parent, new Vector3(0f, 0.03f, z + 11.5f), new Vector3(11f, 0.05f, 6f), cloth, false);
        }

        private static GameObject DecorBlock(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool colliderEnabled = true)
        {
            GameObject item = CreateBlock(name, parent, position, scale);
            ApplyMaterial(item, material);
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) collider.enabled = colliderEnabled;
            return item;
        }

        private static Transform[] CreateActionGrid(Transform parent, StoryRouteProgressAdapter progress, float z,
            OpeningStoryAction[] actions, string[] prompts, PrimitiveType primitive)
        {
            var created = new Transform[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                int row = i / 2;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 position = new Vector3(side * 10.5f, 0.45f, z - 9f + row * 1.8f);
                SemanticActionVisual visual = GetSemanticActionVisual(actions[i], primitive);
                GameObject station = GameObject.CreatePrimitive(visual.Primitive);
                station.name = $"{visual.Label} - {actions[i]}";
                station.transform.SetParent(parent, true);
                station.transform.position = position;
                station.transform.localScale = visual.Scale;
                station.AddComponent<Stage15StoryActionInteractable>().ConfigureAction(progress, actions[i], prompts[i]);
                if (actions[i] == OpeningStoryAction.MeetYuna)
                {
                    station.transform.position = new Vector3(5.5f, 0.82f, z - 6.5f);
                    station.transform.localScale = new Vector3(0.58f, 1.25f, 0.58f);
                    ApplyMaterial(station, CreateMaterial("Yuna Warm Silhouette Material",
                        new Color(0.82f, 0.38f, 0.22f), new Color(0.45f, 0.12f, 0.04f)));
                    GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    head.name = "Yuna Face";
                    head.transform.SetParent(parent, true);
                    head.transform.position = station.transform.position + Vector3.up * 0.95f;
                    head.transform.localScale = Vector3.one * 0.43f;
                    ApplyMaterial(head, CreateMaterial("Yuna Face Material", new Color(0.95f, 0.72f, 0.58f)));
                    Collider headCollider = head.GetComponent<Collider>();
                    if (headCollider != null) headCollider.enabled = false;
                    Material yunaBodyMaterial = station.GetComponent<Renderer>().sharedMaterial;
                    for (int armIndex = 0; armIndex < 2; armIndex++)
                    {
                        float armSide = armIndex == 0 ? -1f : 1f;
                        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        arm.name = armIndex == 0 ? "Yuna Left Arm" : "Yuna Right Arm";
                        arm.transform.SetParent(parent, true);
                        arm.transform.position = station.transform.position + new Vector3(armSide * 0.47f, 0.05f, 0f);
                        arm.transform.localScale = new Vector3(0.18f, 0.58f, 0.18f);
                        arm.transform.rotation = Quaternion.Euler(0f, 0f, armSide * -12f);
                        ApplyMaterial(arm, yunaBodyMaterial);
                        Collider armCollider = arm.GetComponent<Collider>();
                        if (armCollider != null) armCollider.enabled = false;
                    }

                    Material eyeMaterial = CreateMaterial("Yuna Eye Material", new Color(0.08f, 0.055f, 0.045f));
                    for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                    {
                        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        eye.name = eyeIndex == 0 ? "Yuna Left Eye" : "Yuna Right Eye";
                        eye.transform.SetParent(parent, true);
                        eye.transform.position = head.transform.position + new Vector3(eyeIndex == 0 ? -0.12f : 0.12f, 0.05f, -0.2f);
                        eye.transform.localScale = Vector3.one * 0.075f;
                        ApplyMaterial(eye, eyeMaterial);
                        Collider eyeCollider = eye.GetComponent<Collider>();
                        if (eyeCollider != null) eyeCollider.enabled = false;
                    }
                }
                created[i] = station.transform;
            }
            return created;
        }

        private readonly struct SemanticActionVisual
        {
            public SemanticActionVisual(string label, PrimitiveType primitive, Vector3 scale)
            {
                Label = label;
                Primitive = primitive;
                Scale = scale;
            }

            public string Label { get; }
            public PrimitiveType Primitive { get; }
            public Vector3 Scale { get; }
        }

        private static SemanticActionVisual GetSemanticActionVisual(OpeningStoryAction action, PrimitiveType fallback)
        {
            string value = action.ToString();
            if (value.Contains("Yuna") || value.StartsWith("HearGirl") || value.Contains("Dohyeon"))
                return new SemanticActionVisual("Character", PrimitiveType.Capsule, new Vector3(0.65f, 1.2f, 0.65f));
            if (value.Contains("Sofa") || value.Contains("Furniture") || value.Contains("Chair") || value.StartsWith("Seat"))
                return new SemanticActionVisual("Furniture", PrimitiveType.Cube, new Vector3(1.5f, 0.65f, 0.9f));
            if (value.Contains("Door") || value.Contains("Checkpoint") || value.StartsWith("Enter") || value.StartsWith("Return"))
                return new SemanticActionVisual("Doorway", PrimitiveType.Cube, new Vector3(0.85f, 1.8f, 0.24f));
            if (value.Contains("Photo") || value.Contains("Certificate") || value.Contains("Menu") || value.Contains("Mail") || value.Contains("Board") || value.Contains("Frame"))
                return new SemanticActionVisual("Document", PrimitiveType.Cube, new Vector3(1.15f, 0.12f, 0.8f));
            if (value.Contains("Clock") || value.Contains("Time"))
                return new SemanticActionVisual("Clock", PrimitiveType.Cylinder, new Vector3(0.72f, 0.16f, 0.72f));
            if (value.Contains("Badge") || value.Contains("Card") || value.Contains("Band") || value.Contains("Key"))
                return new SemanticActionVisual("Identity Item", PrimitiveType.Cube, new Vector3(0.55f, 0.12f, 0.8f));
            if (value.Contains("Grave") || value.Contains("DeadName"))
                return new SemanticActionVisual("Gravestone", PrimitiveType.Cube, new Vector3(0.75f, 1.35f, 0.28f));
            if (value.Contains("Monitor") || value.Contains("Computer") || value.Contains("Server") || value.Contains("Command"))
                return new SemanticActionVisual("Terminal", PrimitiveType.Cube, new Vector3(1.2f, 0.85f, 0.24f));
            if (value.Contains("Cable") || value.Contains("Power") || value.Contains("Waveform") || value.Contains("Announcement"))
                return new SemanticActionVisual("Control Console", PrimitiveType.Cylinder, new Vector3(0.8f, 0.45f, 0.8f));
            if (value.Contains("Window") || value.Contains("City"))
                return new SemanticActionVisual("Window", PrimitiveType.Cube, new Vector3(1.2f, 1.1f, 0.18f));
            if (value.Contains("Food") || value.Contains("Egg") || value.Contains("Apple") || value.Contains("Soup") || value.Contains("Bowl") || value.Contains("Coffee") || value.Contains("Drink") || value.Contains("Teacup"))
                return new SemanticActionVisual("Tableware", PrimitiveType.Cylinder, new Vector3(0.62f, 0.3f, 0.62f));
            if (value.Contains("Core"))
                return new SemanticActionVisual("Management Core", PrimitiveType.Sphere, Vector3.one * 0.85f);
            return new SemanticActionVisual(ObjectNames.NicifyVariableName(value), fallback, new Vector3(0.75f, 0.85f, 0.75f));
        }

        private static string KoreanPropPrompt(OpeningStoryAction action)
        {
            string value = action.ToString();
            if (value.Contains("Yuna") || value.StartsWith("HearGirl") || value.Contains("Dohyeon")) return "인물의 기억";
            if (value.Contains("Sofa") || value.Contains("Furniture") || value.Contains("Chair") || value.StartsWith("Seat")) return "기억 속 가구";
            if (value.Contains("Door") || value.Contains("Checkpoint") || value.StartsWith("Enter") || value.StartsWith("Return")) return "이어지는 문";
            if (value.Contains("Photo") || value.Contains("Certificate") || value.Contains("Menu") || value.Contains("Mail") || value.Contains("Board") || value.Contains("Frame")) return "남겨진 기록";
            if (value.Contains("Clock") || value.Contains("Time")) return "멈춘 시계";
            if (value.Contains("Badge") || value.Contains("Card") || value.Contains("Band") || value.Contains("Key")) return "신원을 밝히는 물건";
            if (value.Contains("Grave") || value.Contains("DeadName")) return "이름 없는 묘비";
            if (value.Contains("Monitor") || value.Contains("Computer") || value.Contains("Server") || value.Contains("Command")) return "기억 단말기";
            if (value.Contains("Cable") || value.Contains("Power") || value.Contains("Waveform") || value.Contains("Announcement")) return "연결 제어 장치";
            if (value.Contains("Window") || value.Contains("City")) return "도시가 비치는 창문";
            if (value.Contains("Egg") || value.Contains("Apple") || value.Contains("Soup") || value.Contains("Bowl") || value.Contains("Coffee") || value.Contains("Drink") || value.Contains("Teacup")) return "식탁 위 흔적";
            if (value.Contains("Core")) return "관리 핵심";
            return "주변의 이야기 단서";
        }

        private static Transform CreateMarker(string name, Transform parent, Vector3 position,
            StoryRouteController route, string nodeId, StoryRouteStep step, string prompt, string feedback)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.2f, 0.75f, 1.2f);
            return marker.transform;
        }

        private static void CreateRouteRoomEnvelope(Transform parent, int index, float z)
        {
            Material wallMaterial = CreateMaterial($"Route Room {index} Wall Material", WallColors[index]);
            ApplyMaterial(CreateBlock($"Route Room {index} Left Sight Wall", parent,
                new Vector3(-15f, 2.5f, z), new Vector3(0.4f, 5f, 32f)), wallMaterial);
            ApplyMaterial(CreateBlock($"Route Room {index} Right Sight Wall", parent,
                new Vector3(15f, 2.5f, z), new Vector3(0.4f, 5f, 32f)), wallMaterial);
            ApplyMaterial(CreateBlock($"Route Room {index} Arrival Back Wall", parent,
                new Vector3(0f, 2.5f, z - 16f), new Vector3(30f, 5f, 0.4f)), wallMaterial);
            ApplyMaterial(CreateBlock($"Route Room {index} Forward Occlusion Wall", parent,
                new Vector3(0f, 2.5f, z + 16f), new Vector3(30f, 5f, 0.4f)), wallMaterial);
            ApplyMaterial(CreateBlock($"Route Room {index} Ceiling", parent,
                new Vector3(0f, 5f, z), new Vector3(30f, 0.3f, 32f)), wallMaterial);
            CreatePointLight($"Route Room {index} Entry Light", parent,
                new Vector3(0f, 3.5f, z - 11f), AccentColors[index], 1.6f, 12f);
        }

        private static void CreateRoomWayfinding(Transform parent, int index, float z, Transform arrival,
            Transform dialogue, Transform puzzle, Transform memory)
        {
            Color accent = AccentColors[index];
            Material accentMaterial = CreateMaterial($"Route Room {index} Accent Material", accent, accent * 0.55f);
            Material pathMaterial = CreateMaterial($"Route Room {index} Path Material",
                Color.Lerp(FloorColors[index], accent, 0.35f), accent * 0.3f);

            Vector3 signPosition = index == 0
                ? new Vector3(0f, 4.15f, z - 1.5f)
                : new Vector3(0f, 3.8f, z - 9.5f);
            CreateWorldLabel($"Route Room {index} Entrance Sign", parent, WorldNames[index], signPosition,
                ReadableTextAgainst(WallColors[index]));
            CreateHighlightFrame($"Route Room {index} Dialogue Highlight", parent, dialogue.position, accentMaterial);
            CreateHighlightFrame($"Route Room {index} Puzzle Highlight", parent, puzzle.position, accentMaterial);
            CreateHighlightFrame($"Route Room {index} Memory Highlight", parent, memory.position, accentMaterial);

            Transform[] route = { arrival, dialogue, puzzle, memory };
            for (int segment = 0; segment < route.Length - 1; segment++)
            {
                Vector3 from = route[segment].position;
                Vector3 to = route[segment + 1].position;
                for (int step = 1; step <= 3; step++)
                {
                    Vector3 position = Vector3.Lerp(from, to, step / 4f);
                    position.y = 0.025f;
                    GameObject marker = CreateBlock($"Route Room {index} Path {segment + 1}-{step}", parent,
                        position, new Vector3(0.18f, 0.035f, 0.32f));
                    marker.transform.rotation = Quaternion.LookRotation((to - from).normalized, Vector3.up);
                    ApplyMaterial(marker, pathMaterial);
                    Collider collider = marker.GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                }
            }

            Vector3 objectivePosition = dialogue.position + Vector3.up * 2.4f;
            CreatePointLight($"Route Room {index} Objective Light", parent, objectivePosition, accent, 2.4f, 7f);
        }

        private static void CreateHighlightFrame(string name, Transform parent, Vector3 target, Material material)
        {
            var frame = new GameObject(name);
            frame.transform.SetParent(parent, true);
            frame.transform.position = target;
            Vector3[] positions =
            {
                new Vector3(-0.65f, 0.65f, 0f), new Vector3(0.65f, 0.65f, 0f),
                new Vector3(0f, 1.25f, 0f), new Vector3(0f, 0.05f, 0f)
            };
            Vector3[] scales =
            {
                new Vector3(0.035f, 1.3f, 0.035f), new Vector3(0.035f, 1.3f, 0.035f),
                new Vector3(1.35f, 0.035f, 0.035f), new Vector3(1.35f, 0.035f, 0.035f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject edge = CreateBlock($"{name} Edge {i + 1}", frame.transform, target + positions[i], scales[i]);
                ApplyMaterial(edge, material);
                Collider collider = edge.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
        }

        private static void CreateWorldLabel(string name, Transform parent, string value, Vector3 position, Color color)
        {
            float plateWidth = Mathf.Clamp(value.Length * 0.34f + 1.2f, 3.4f, 9.5f);
            bool darkText = RelativeLuminance(color) < 0.18f;
            Color plateColor = darkText
                ? new Color(0.92f, 0.9f, 0.84f, 1f)
                : new Color(0.025f, 0.035f, 0.05f, 1f);
            GameObject backplate = CreateBlock(name + " Backplate", parent,
                position + Vector3.forward * 0.08f, new Vector3(plateWidth, 0.72f, 0.08f));
            ApplyMaterial(backplate, CreateMaterial(name + " Backplate Material", plateColor));
            Collider plateCollider = backplate.GetComponent<Collider>();
            if (plateCollider != null) plateCollider.enabled = false;

            var label = new GameObject(name);
            label.transform.SetParent(parent, true);
            label.transform.position = position;
            // TextMesh's readable face points toward -Z at identity. Every route arrival looks
            // toward +Z, so identity presents the front face instead of mirrored back-face text.
            label.transform.rotation = Quaternion.identity;
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = 0.09f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
        }

        private static Color ReadableTextAgainst(Color background)
        {
            return RelativeLuminance(background) > 0.3f
                ? new Color(0.025f, 0.035f, 0.05f, 1f)
                : new Color(1f, 0.98f, 0.92f, 1f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * LinearColor(color.r) + 0.7152f * LinearColor(color.g) + 0.0722f * LinearColor(color.b);
        }

        private static float LinearColor(float value)
        {
            return value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static Material CreateMaterial(string name, Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static void CreateFinalGate(Transform root, StoryRouteController route, int nodeCount)
        {
            float z = (nodeCount - 1) * 36f + 20f;
            GameObject gate = CreateBlock("Final Choice Readiness Inspector", root, new Vector3(0f, 1.25f, z), new Vector3(4f, 2.5f, 0.5f));
            gate.AddComponent<StoryRouteInteractable>().ConfigureFinalGate(route, "Inspect final choice readiness only");
        }

        private static Transform CreatePlayer(InputActionAsset actions)
        {
            var playerObject = new GameObject("First Person Player");
            playerObject.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -13f), Quaternion.identity);
            CharacterController character = playerObject.AddComponent<CharacterController>();
            character.height = 1.8f; character.radius = 0.32f; character.center = new Vector3(0f, 0.9f, 0f);
            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 85f; camera.nearClipPlane = 0.05f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f); cameraObject.AddComponent<AudioListener>();
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
            CanvasScaler scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var safeArea = new GameObject("Safe Area", typeof(RectTransform));
            safeArea.transform.SetParent(hud.transform, false);
            RectTransform safeRect = (RectTransform)safeArea.transform;
            safeRect.anchorMin = Vector2.zero; safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
            safeArea.AddComponent<SafeAreaFitter>();
            var crosshairObject = new GameObject("Crosshair", typeof(RectTransform)); crosshairObject.transform.SetParent(hud.transform, false);
            RectTransform crosshairRect = (RectTransform)crosshairObject.transform; crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f); crosshairRect.sizeDelta = new Vector2(4f, 4f);
            Image crosshair = crosshairObject.AddComponent<Image>(); crosshair.color = new Color(1f, 1f, 1f, 0.8f); crosshair.raycastTarget = false;
            var ui = new GameObject("Interaction UI", typeof(RectTransform)); ui.transform.SetParent(safeArea.transform, false);
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
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.65f;
            light.color = new Color(1f, 0.94f, 0.86f);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.54f);
            RenderSettings.ambientIntensity = 0.9f;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name; block.transform.SetParent(parent, true); block.transform.position = position; block.transform.localScale = scale; return block;
        }

        private static Light CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        private static void AddBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
