using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Save.Stage10.Integration
{
    public sealed class Stage10ManualSavePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Button[] saveButtons = Array.Empty<Button>();
        [SerializeField] private Button[] loadButtons = Array.Empty<Button>();
        [SerializeField] private Button closeButton;
        private RealityRoomSaveCoordinator coordinator;
        public void Configure(CanvasGroup root, Button[] saves, Button[] loads, Button close) { panel = root; saveButtons = saves ?? Array.Empty<Button>(); loadButtons = loads ?? Array.Empty<Button>(); closeButton = close; Bind(); Close(); }
        public void Configure(RealityRoomSaveCoordinator value) => coordinator = value;
        private void Awake() { Bind(); Close(); }
        private void OnDestroy() => Unbind();
        public void Open() { SetVisible(true); RefreshLoads(); }
        public void Close() => SetVisible(false);
        public void Save0() => Save(0); public void Save1() => Save(1); public void Save2() => Save(2);
        public void Load0() => Load(0); public void Load1() => Load(1); public void Load2() => Load(2);
        private void Save(int slot) { coordinator?.SaveManual(slot); RefreshLoads(); }
        private void Load(int slot) { if (coordinator != null && coordinator.LoadManual(slot)) Close(); }
        private void RefreshLoads() { for (int i = 0; i < loadButtons.Length && i < 3; i++) if (loadButtons[i] != null) loadButtons[i].interactable = Stage10SaveRuntime.Service.LoadManual(i).IsSuccess; }
        private void Bind()
        {
            Unbind();
            if (saveButtons.Length > 0) saveButtons[0]?.onClick.AddListener(Save0); if (saveButtons.Length > 1) saveButtons[1]?.onClick.AddListener(Save1); if (saveButtons.Length > 2) saveButtons[2]?.onClick.AddListener(Save2);
            if (loadButtons.Length > 0) loadButtons[0]?.onClick.AddListener(Load0); if (loadButtons.Length > 1) loadButtons[1]?.onClick.AddListener(Load1); if (loadButtons.Length > 2) loadButtons[2]?.onClick.AddListener(Load2);
            closeButton?.onClick.AddListener(Close);
        }
        private void Unbind()
        {
            if (saveButtons.Length > 0) saveButtons[0]?.onClick.RemoveListener(Save0); if (saveButtons.Length > 1) saveButtons[1]?.onClick.RemoveListener(Save1); if (saveButtons.Length > 2) saveButtons[2]?.onClick.RemoveListener(Save2);
            if (loadButtons.Length > 0) loadButtons[0]?.onClick.RemoveListener(Load0); if (loadButtons.Length > 1) loadButtons[1]?.onClick.RemoveListener(Load1); if (loadButtons.Length > 2) loadButtons[2]?.onClick.RemoveListener(Load2);
            closeButton?.onClick.RemoveListener(Close);
        }
        private void SetVisible(bool visible) { if (panel == null) return; panel.alpha = visible ? 1f : 0f; panel.interactable = visible; panel.blocksRaycasts = visible; }
    }
}
