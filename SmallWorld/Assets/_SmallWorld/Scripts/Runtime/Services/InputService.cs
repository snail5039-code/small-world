using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Services
{
    [DefaultExecutionOrder(-900)]
    public sealed class InputService : MonoBehaviour
    {
        private readonly HashSet<InputActionMap> managedMaps = new HashSet<InputActionMap>();
        private bool gameplayInputRequested = true;
        private bool hasFocus = true;

        public static InputService Instance { get; private set; }

        public bool IsGameplayInputActive => gameplayInputRequested && hasFocus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject(nameof(InputService)).AddComponent<InputService>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            hasFocus = Application.isFocused;
            RefreshActionMaps();
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            RefreshActionMaps();
        }

        private void OnDisable()
        {
            SetMapsEnabled(false);
        }

        public void RegisterGameplayMap(InputActionMap actionMap)
        {
            if (actionMap != null && managedMaps.Add(actionMap))
            {
                SetMapEnabled(actionMap, IsGameplayInputActive);
            }
        }

        public void UnregisterGameplayMap(InputActionMap actionMap)
        {
            if (actionMap != null && managedMaps.Remove(actionMap))
            {
                actionMap.Disable();
            }
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            gameplayInputRequested = enabled;
            RefreshActionMaps();
        }

        private void RefreshActionMaps()
        {
            SetMapsEnabled(IsGameplayInputActive);
        }

        private void SetMapsEnabled(bool enabled)
        {
            managedMaps.RemoveWhere(map => map == null);

            foreach (var actionMap in managedMaps)
            {
                SetMapEnabled(actionMap, enabled);
            }
        }

        private static void SetMapEnabled(InputActionMap actionMap, bool enabled)
        {
            if (enabled)
            {
                actionMap.Enable();
            }
            else
            {
                actionMap.Disable();
            }
        }
    }
}

