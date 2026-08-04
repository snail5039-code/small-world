using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        private RectTransform target;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void Awake()
        {
            target = (RectTransform)transform;
            ConfigureScaler();
            Apply();
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != lastSafeArea || size != lastScreen) Apply();
        }

        public void Apply()
        {
            if (target == null) target = (RectTransform)transform;
            Vector4 anchors = CalculateAnchors(Screen.safeArea, Screen.width, Screen.height);
            target.anchorMin = new Vector2(anchors.x, anchors.y);
            target.anchorMax = new Vector2(anchors.z, anchors.w);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            lastSafeArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }

        public static Vector4 CalculateAnchors(Rect safeArea, int screenWidth, int screenHeight)
        {
            Vector4 fullRect = new Vector4(0f, 0f, 1f, 1f);
            if (screenWidth <= 0 || screenHeight <= 0 ||
                !IsFinite(safeArea.xMin) || !IsFinite(safeArea.yMin) ||
                !IsFinite(safeArea.xMax) || !IsFinite(safeArea.yMax) ||
                safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return fullRect;
            }

            float minX = Mathf.Clamp01(safeArea.xMin / screenWidth);
            float minY = Mathf.Clamp01(safeArea.yMin / screenHeight);
            float maxX = Mathf.Clamp01(safeArea.xMax / screenWidth);
            float maxY = Mathf.Clamp01(safeArea.yMax / screenHeight);
            if (maxX <= minX || maxY <= minY) return fullRect;
            return new Vector4(minX, minY, maxX, maxY);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void ConfigureScaler()
        {
            if (scaler == null) scaler = GetComponentInParent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
