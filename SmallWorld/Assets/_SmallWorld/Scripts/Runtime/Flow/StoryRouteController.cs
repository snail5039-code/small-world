using System;
using System.Threading.Tasks;
using SmallWorld.Core;
using SmallWorld.Player;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.Save.Story;
using SmallWorld.UI.Stage7;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Flow
{
    public enum StoryRouteStep { Dialogue, Puzzle, Memory }

    public interface IStoryRouteProgressSource
    {
        bool IsNodeUnlocked(string nodeId);
        bool IsFinalGateUnlocked { get; }
        void ReportNodeReached(string nodeId);
        void ReportStep(string nodeId, StoryRouteStep step);
    }

    public interface IStoryRouteChapterPositionSource
    {
        int LatestUnlockedNodeIndex { get; }
    }

    public interface IStoryRouteRealityReturnSource
    {
        bool PrepareRealityRoomReturn(out string feedback);
    }

    public readonly struct StoryRouteReturnResult
    {
        public StoryRouteReturnResult(bool accepted, string feedback)
        {
            Accepted = accepted;
            Feedback = feedback;
        }
        public bool Accepted { get; }
        public string Feedback { get; }
    }

    [Serializable]
    public sealed class StoryRouteNode
    {
        public string Id;
        public string DisplayName;
        public Transform Arrival;
        public Transform DialogueEntry;
        public Transform PuzzleEntry;
        public Transform MemoryEntry;
    }

    public sealed class StoryRouteController : MonoBehaviour
    {
        private enum RuntimeOverlay { None, Records, Paused }

        [SerializeField] private Transform player;
        [SerializeField] private StoryRouteNode[] nodes = Array.Empty<StoryRouteNode>();
        [SerializeField] private int fallbackUnlockedIndex;

        private IStoryRouteProgressSource progressSource;
        private RuntimeOverlay runtimeOverlay;
        private FirstPersonPlayerController playerController;
        private Stage10ManualSavePanel savePanel;
        private bool playerWasEnabled;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;
        private float timeScaleBeforePause = 1f;
        private bool inputStateCaptured;
        private string currentLocation = string.Empty;
        private string currentObjective = string.Empty;
        private string arrivalDialogue = string.Empty;
        private float arrivalNoticeUntil;
        private int activeNodeIndex;
        private bool realityRoomTransitionStarted;
        private Func<Task> realityRoomLoader;

        public int NodeCount => nodes?.Length ?? 0;
        public int FallbackUnlockedIndex => fallbackUnlockedIndex;
        public bool IsFinalGateUnlocked => progressSource?.IsFinalGateUnlocked ?? false;
        public bool IsRuntimeOverlayOpen => runtimeOverlay != RuntimeOverlay.None;
        public bool IsRuntimePaused => runtimeOverlay == RuntimeOverlay.Paused;
        public string CurrentLocation => currentLocation;
        public string CurrentObjective => currentObjective;
        public int ActiveNodeIndex => activeNodeIndex;
        public static string PauseTitle => "일시정지";
        public static string PauseMessage => "Esc를 누르면 이야기로 돌아갑니다.";
        public static string RecordsTitle => "기록";
        public static string EmptyRecordsMessage => "아직 수집한 기록이 없습니다.\n\nTab 또는 Esc를 누르면 닫힙니다.";

        public static Rect RuntimeOverlayRect(int screenWidth, int screenHeight, bool paused)
        {
            float margin = Mathf.Clamp(screenWidth * 0.02f, 16f, 32f);
            float width = Mathf.Clamp(screenWidth * (paused ? 0.28f : 0.36f), 300f, paused ? 460f : 580f);
            width = Mathf.Min(width, Mathf.Max(240f, screenWidth - margin * 2f));
            float height = paused ? 116f : Mathf.Clamp(screenHeight * 0.32f, 220f, 340f);
            height = Mathf.Min(height, Mathf.Max(100f, screenHeight - margin * 2f));
            return new Rect(screenWidth - width - margin, margin, width, height);
        }

        public void UpdateGuidance(string location, string objective, string dialogue)
        {
            currentLocation = location ?? string.Empty;
            currentObjective = objective ?? string.Empty;
            arrivalDialogue = dialogue ?? string.Empty;
            arrivalNoticeUntil = Time.unscaledTime + 8f;
        }

        public void UpdateObjective(string objective)
        {
            currentObjective = objective ?? string.Empty;
        }

        public void Configure(Transform playerTransform, StoryRouteNode[] routeNodes)
        {
            player = playerTransform;
            nodes = routeNodes ?? Array.Empty<StoryRouteNode>();
        }

        public void BindProgressSource(IStoryRouteProgressSource source) => progressSource = source;

        public int RestoreToNodeOrPrologue(int requestedIndex)
        {
            int safeIndex = nodes != null && requestedIndex >= 0 && requestedIndex < nodes.Length
                ? requestedIndex
                : 0;
            if (nodes == null || nodes.Length == 0 || nodes[safeIndex]?.Arrival == null || player == null)
                return -1;

            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            player.SetPositionAndRotation(nodes[safeIndex].Arrival.position, nodes[safeIndex].Arrival.rotation);
            if (character != null) character.enabled = true;
            fallbackUnlockedIndex = Mathf.Max(fallbackUnlockedIndex, safeIndex);
            activeNodeIndex = safeIndex;
            return safeIndex;
        }

        public void ReportStep(string nodeId, StoryRouteStep step) => progressSource?.ReportStep(nodeId, step);

        private void Awake()
        {
            ResolveRuntimeInputOwners();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleTabPressed();
                return;
            }

            if (Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                HandleRoomBrowse(-1, out _);
                return;
            }
            if (Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                HandleRoomBrowse(1, out _);
                return;
            }
            if (Keyboard.current.homeKey.wasPressedThisFrame)
            {
                _ = ReturnToRealityRoomAsync();
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame) HandleEscapePressed();
        }

        private void OnDestroy()
        {
            CloseRuntimeOverlay();
        }

        public bool HandleTabPressed()
        {
            if (IsSaveMenuOpen()) return false;
            if (runtimeOverlay == RuntimeOverlay.Paused) return true;
            SetRuntimeOverlay(runtimeOverlay == RuntimeOverlay.Records
                ? RuntimeOverlay.None
                : RuntimeOverlay.Records);
            return true;
        }

        public bool HandleEscapePressed()
        {
            if (IsSaveMenuOpen()) return false;
            SetRuntimeOverlay(runtimeOverlay == RuntimeOverlay.None
                ? RuntimeOverlay.Paused
                : RuntimeOverlay.None);
            return true;
        }

        private void SetRuntimeOverlay(RuntimeOverlay overlay)
        {
            if (runtimeOverlay == overlay) return;
            if (runtimeOverlay == RuntimeOverlay.None && overlay != RuntimeOverlay.None)
                CaptureGameplayInputState();

            if (runtimeOverlay == RuntimeOverlay.Paused && overlay != RuntimeOverlay.Paused)
                RestoreTimeScale();

            runtimeOverlay = overlay;
            if (runtimeOverlay == RuntimeOverlay.Paused)
            {
                timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (runtimeOverlay == RuntimeOverlay.None) RestoreGameplayInputState();
        }

        private void CloseRuntimeOverlay()
        {
            if (runtimeOverlay == RuntimeOverlay.Paused) RestoreTimeScale();
            runtimeOverlay = RuntimeOverlay.None;
            RestoreGameplayInputState();
        }

        private void CaptureGameplayInputState()
        {
            if (inputStateCaptured) return;
            ResolveRuntimeInputOwners();
            playerWasEnabled = playerController != null && playerController.enabled;
            previousCursorLockState = DialogueCursorMode.RequestedLockState;
            previousCursorVisible = DialogueCursorMode.RequestedVisible;
            inputStateCaptured = true;
            if (playerWasEnabled) playerController.enabled = false;
            DialogueCursorMode.RequestUi();
        }

        private void RestoreGameplayInputState()
        {
            if (!inputStateCaptured) return;
            inputStateCaptured = false;
            if (playerController != null) playerController.enabled = playerWasEnabled;
            DialogueCursorMode.Restore(previousCursorLockState, previousCursorVisible);
        }

        private void ResolveRuntimeInputOwners()
        {
            if (playerController == null && player != null)
                playerController = player.GetComponent<FirstPersonPlayerController>();
            if (playerController == null)
                playerController = FindFirstObjectByType<FirstPersonPlayerController>();
            if (savePanel == null)
                savePanel = FindFirstObjectByType<Stage10ManualSavePanel>(FindObjectsInactive.Include);
        }

        private bool IsSaveMenuOpen()
        {
            ResolveRuntimeInputOwners();
            return savePanel != null && savePanel.IsOpen;
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f) Time.timeScale = Mathf.Max(0.0001f, timeScaleBeforePause);
        }

        private void OnGUI()
        {
            DrawGuidance();
            if (runtimeOverlay == RuntimeOverlay.None) return;
            bool paused = runtimeOverlay == RuntimeOverlay.Paused;
            Rect panel = RuntimeOverlayRect(Screen.width, Screen.height, paused);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 60f), 14, 20),
                alignment = TextAnchor.UpperCenter
            };
            GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 68f), 13, 18),
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(8, 8, 4, 4)
            };
            GUI.Box(panel, paused ? PauseTitle : RecordsTitle, boxStyle);
            Rect message = new Rect(panel.x + 18f, panel.y + 36f, panel.width - 36f, panel.height - 48f);
            GUI.Label(message, paused ? PauseMessage : EmptyRecordsMessage, messageStyle);
        }

        private void DrawGuidance()
        {
            if (string.IsNullOrWhiteSpace(currentLocation)) return;
            float margin = Mathf.Clamp(Screen.width * 0.0125f, 12f, 24f);
            float width = Mathf.Min(Mathf.Clamp(Screen.width * 0.36f, 340f, 680f), Screen.width - margin * 2f);
            Rect panel = new Rect(margin, margin, width, Time.unscaledTime < arrivalNoticeUntil ? 112f : 76f);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 64f), 14, 19),
                alignment = TextAnchor.UpperCenter
            };
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 72f), 13, 17),
                wordWrap = true
            };
            labelStyle.normal.textColor = Color.white;
            GUI.Box(panel, currentLocation, boxStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 30f, panel.width - 36f, 38f),
                $"현재 목표: {currentObjective}", labelStyle);
            if (Time.unscaledTime < arrivalNoticeUntil)
                GUI.Label(new Rect(panel.x + 18f, panel.y + 68f, panel.width - 36f, 36f), arrivalDialogue, labelStyle);
        }

        public bool TryTravelTo(int index, out string feedback)
        {
            if (runtimeOverlay != RuntimeOverlay.None || IsSaveMenuOpen())
            {
                feedback = "열린 UI를 닫은 뒤 방을 이동하세요.";
                return false;
            }
            if (nodes == null || index < 0 || index >= nodes.Length || nodes[index]?.Arrival == null)
            {
                feedback = "이동할 이야기 방이 연결되지 않았습니다.";
                return false;
            }

            StoryRouteNode node = nodes[index];
            bool unlocked = progressSource != null
                ? progressSource.IsNodeUnlocked(node.Id)
                : index <= fallbackUnlockedIndex;
            if (!unlocked)
            {
                feedback = $"{node.DisplayName}은(는) 아직 잠겨 있습니다.";
                return false;
            }

            if (player == null)
            {
                feedback = "플레이어를 찾을 수 없어 방을 이동할 수 없습니다.";
                return false;
            }

            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            player.SetPositionAndRotation(node.Arrival.position, node.Arrival.rotation);
            if (character != null) character.enabled = true;
            int storyIndex = progressSource is IStoryRouteChapterPositionSource position
                ? position.LatestUnlockedNodeIndex
                : index;
            if (index == storyIndex) progressSource?.ReportNodeReached(node.Id);
            if (progressSource is StoryRouteProgressAdapter adapter)
                adapter.PresentVisitedRoom((StoryChapterId)index, index == storyIndex);
            fallbackUnlockedIndex = Mathf.Max(fallbackUnlockedIndex, Mathf.Min(index + 1, nodes.Length - 1));
            activeNodeIndex = index;
            feedback = $"{node.DisplayName}에 들어왔습니다.";
            return true;
        }

        public bool HandleRoomBrowse(int direction, out string feedback)
        {
            if (direction == 0)
            {
                feedback = "이전 방 또는 다음 방을 선택하세요.";
                return false;
            }
            if (runtimeOverlay != RuntimeOverlay.None || IsSaveMenuOpen())
            {
                feedback = "열린 UI를 닫은 뒤 방을 이동하세요.";
                return false;
            }

            int latest = progressSource is IStoryRouteChapterPositionSource position
                ? position.LatestUnlockedNodeIndex
                : Mathf.Clamp(fallbackUnlockedIndex, 0, Mathf.Max(0, NodeCount - 1));
            int target = Mathf.Clamp(activeNodeIndex + Math.Sign(direction), 0, latest);
            if (target == activeNodeIndex)
            {
                feedback = direction < 0 ? "더 이전에 해금된 방이 없습니다." : "이미 현재 진행 방에 있습니다.";
                return false;
            }
            return TryTravelTo(target, out feedback);
        }

        public void ConfigureRealityRoomLoader(Func<Task> loader) => realityRoomLoader = loader;

        public async Task<StoryRouteReturnResult> ReturnToRealityRoomAsync()
        {
            if (activeNodeIndex != 0)
                return ReturnRejected("프롤로그 방에서만 현실방으로 돌아갈 수 있습니다.");
            if (runtimeOverlay != RuntimeOverlay.None || IsSaveMenuOpen())
                return ReturnRejected("열린 UI를 닫은 뒤 현실방으로 돌아가세요.");
            if (realityRoomTransitionStarted)
                return ReturnRejected("이미 현실방으로 이동하고 있습니다.");
            if (!(progressSource is IStoryRouteRealityReturnSource returnSource))
                return ReturnRejected("현실방 복귀 저장 장치가 연결되지 않았습니다.");
            if (realityRoomLoader == null && SceneTransitionService.Instance == null)
                return ReturnRejected("장면 이동 서비스를 찾을 수 없습니다.");
            if (!returnSource.PrepareRealityRoomReturn(out string saveFeedback))
                return ReturnRejected(string.IsNullOrWhiteSpace(saveFeedback)
                    ? "현실방 복귀 상태를 저장하지 못했습니다."
                    : saveFeedback);

            realityRoomTransitionStarted = true;
            try
            {
                if (realityRoomLoader != null) await realityRoomLoader();
                else await SceneTransitionService.Instance.LoadSceneAsync(SceneId.RealityRoom);
                return new StoryRouteReturnResult(true, "현실방으로 돌아갑니다.");
            }
            catch (Exception exception)
            {
                realityRoomTransitionStarted = false;
                Debug.LogException(exception, this);
                return ReturnRejected("현실방 이동에 실패했습니다. 다시 시도하세요.");
            }
        }

        private StoryRouteReturnResult ReturnRejected(string feedback)
        {
            UpdateObjective(feedback);
            return new StoryRouteReturnResult(false, feedback);
        }
    }
}
