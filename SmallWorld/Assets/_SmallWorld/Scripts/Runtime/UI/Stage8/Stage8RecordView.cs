using System;
using System.Collections.Generic;
using System.Text;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Player;
using SmallWorld.UI.Stage7;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI.Stage8
{
    public enum RecordTab
    {
        Inventory,
        Memories,
        Records
    }

    public sealed class Stage8RecordView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Text tabTitle;
        [SerializeField] private Text recordList;
        [SerializeField] private Text recordDetails;
        [SerializeField] private Button inventoryTab;
        [SerializeField] private Button memoryTab;
        [SerializeField] private Button recordsTab;
        [SerializeField] private Button closeButton;
        [SerializeField] private FirstPersonPlayerController player;
        [SerializeField] private Stage6UIController stage6UI;
        [SerializeField] private Stage7DialogueView dialogueView;

        private readonly InventoryJournal journal = new InventoryJournal();
        private RecordTab currentTab;

        public IInventoryReader Reader => journal;
        public IRecordCollector Collector => journal;
        public bool IsOpen => IsVisible(panel);
        public RecordTab CurrentTab => currentTab;
        public event Action<InventoryRecord> NewRecordAdded;

        public void Configure(CanvasGroup root, Text title, Text list, Text details,
            Button inventory, Button memories, Button records, Button close,
            FirstPersonPlayerController playerController, Stage6UIController stage6Controller,
            Stage7DialogueView dialogue)
        {
            Unbind();
            panel = root;
            tabTitle = title;
            recordList = list;
            recordDetails = details;
            inventoryTab = inventory;
            memoryTab = memories;
            recordsTab = records;
            closeButton = close;
            player = playerController;
            stage6UI = stage6Controller;
            dialogueView = dialogue;
            Bind();
            SetVisible(panel, false);
            SelectTab(RecordTab.Inventory);
        }

        private void Awake()
        {
            Bind();
            SetVisible(panel, false);
        }

        private void OnDestroy() => Unbind();

        public bool AddRecord(InventoryRecord record) => journal.Add(record);

        public IReadOnlyList<StoredRecord> CaptureRecords() => journal.GetAll(null, RecordSort.AcquiredNewest);

        public void RestoreRecords(IEnumerable<InventoryRecord> records)
        {
            journal.Clear();
            if (records != null)
                foreach (InventoryRecord record in records)
                    if (record != null) journal.Add(record);
            Refresh();
        }

        public bool Open()
        {
            if (IsOpen || stage6UI == null || stage6UI.StateMachine.Current != UIState.Gameplay ||
                (dialogueView != null && dialogueView.IsDialogueActive)) return false;
            SetVisible(panel, true);
            if (player != null) player.enabled = false;
            DialogueCursorMode.RequestUi();
            Refresh();
            return true;
        }

        public bool Close()
        {
            if (!IsOpen) return false;
            SetVisible(panel, false);
            if (CanRestoreGameplay())
            {
                if (player != null) player.enabled = true;
                DialogueCursorMode.RequestGameplay();
            }
            else DialogueCursorMode.RequestUi();
            return true;
        }

        public bool Toggle() => IsOpen ? Close() : Open();

        public void SelectInventory() => SelectTab(RecordTab.Inventory);
        public void SelectMemories() => SelectTab(RecordTab.Memories);
        public void SelectRecords() => SelectTab(RecordTab.Records);

        public void SelectTab(RecordTab tab)
        {
            currentTab = tab;
            Refresh();
        }

        private void Refresh()
        {
            if (tabTitle != null) tabTitle.text = GetTabTitle(currentTab);
            IReadOnlyList<StoredRecord> entries = GetEntries(currentTab);
            var listBuilder = new StringBuilder();
            var detailBuilder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                InventoryRecord record = entries[i].Record;
                if (listBuilder.Length > 0) listBuilder.AppendLine();
                listBuilder.Append("• ").Append(record.Title);
                if (detailBuilder.Length > 0) detailBuilder.AppendLine().AppendLine();
                detailBuilder.Append('[').Append(GetKindLabel(record.Kind)).Append("] ")
                    .Append(record.Title).AppendLine();
                detailBuilder.Append(record.Description);
            }
            if (recordList != null) recordList.text = entries.Count == 0 ? "아직 기록이 없습니다." : listBuilder.ToString();
            if (recordDetails != null) recordDetails.text = detailBuilder.ToString();
        }

        private IReadOnlyList<StoredRecord> GetEntries(RecordTab tab)
        {
            if (tab == RecordTab.Inventory) return journal.GetAll(RecordKind.KeyItem);
            if (tab == RecordTab.Memories) return journal.GetAll(RecordKind.MemoryFragment);
            var combined = new List<StoredRecord>();
            Append(combined, journal.GetAll(RecordKind.Investigation));
            Append(combined, journal.GetAll(RecordKind.Photo));
            Append(combined, journal.GetAll(RecordKind.NameFragment));
            combined.Sort((left, right) => left.Record.SortOrder != right.Record.SortOrder
                ? left.Record.SortOrder.CompareTo(right.Record.SortOrder)
                : string.CompareOrdinal(left.Record.Id, right.Record.Id));
            return combined;
        }

        private void Bind()
        {
            Unbind();
            inventoryTab?.onClick.AddListener(SelectInventory);
            memoryTab?.onClick.AddListener(SelectMemories);
            recordsTab?.onClick.AddListener(SelectRecords);
            closeButton?.onClick.AddListener(CloseFromButton);
            journal.RecordAdded += OnRecordAdded;
            if (stage6UI != null) stage6UI.StateMachine.Changed += OnUIStateChanged;
            if (dialogueView != null) dialogueView.DialogueActivityChanged += OnDialogueActivityChanged;
        }

        private void Unbind()
        {
            inventoryTab?.onClick.RemoveListener(SelectInventory);
            memoryTab?.onClick.RemoveListener(SelectMemories);
            recordsTab?.onClick.RemoveListener(SelectRecords);
            closeButton?.onClick.RemoveListener(CloseFromButton);
            journal.RecordAdded -= OnRecordAdded;
            if (stage6UI != null) stage6UI.StateMachine.Changed -= OnUIStateChanged;
            if (dialogueView != null) dialogueView.DialogueActivityChanged -= OnDialogueActivityChanged;
        }

        private void OnRecordAdded(NewRecordEvent added)
        {
            Refresh();
            NewRecordAdded?.Invoke(added.Entry.Record);
        }

        private void OnUIStateChanged(UIState previous, UIState current)
        {
            if (current != UIState.Gameplay && IsOpen) Close();
        }

        private void OnDialogueActivityChanged(bool active)
        {
            if (active && IsOpen) Close();
        }

        private void CloseFromButton() => Close();
        private bool CanRestoreGameplay() => stage6UI != null && stage6UI.StateMachine.Current == UIState.Gameplay &&
            (dialogueView == null || !dialogueView.IsDialogueActive);

        private static void Append(List<StoredRecord> destination, IReadOnlyList<StoredRecord> source)
        {
            for (int i = 0; i < source.Count; i++) destination.Add(source[i]);
        }

        private static string GetTabTitle(RecordTab tab) => tab == RecordTab.Inventory ? "인벤토리" :
            tab == RecordTab.Memories ? "기억 조각" : "조사 · 사진 · 이름 기록";

        private static string GetKindLabel(RecordKind kind)
        {
            switch (kind)
            {
                case RecordKind.KeyItem: return "핵심 물건";
                case RecordKind.MemoryFragment: return "기억";
                case RecordKind.Investigation: return "조사";
                case RecordKind.Photo: return "사진";
                case RecordKind.NameFragment: return "이름";
                default: return kind.ToString();
            }
        }

        private static bool IsVisible(CanvasGroup group) => group != null && group.alpha > 0f &&
            group.interactable && group.blocksRaycasts;

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
