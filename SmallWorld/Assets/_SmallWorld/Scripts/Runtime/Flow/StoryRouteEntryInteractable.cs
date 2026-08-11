using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class StoryRouteEntryInteractable : InteractableBase
    {
        private const string StorySceneName = "04_StoryRoute";
        [SerializeField] private DoorInteractable entryDoor;
        private bool transitionStarted;

        public void ConfigureEntry() => Configure("Enter the chapter route", GetComponentsInChildren<Renderer>(true));

        public void ConfigureDoorEntry(DoorInteractable door)
        {
            entryDoor = door;
            Configure("문 너머로 나가기");
        }

        private void OnEnable()
        {
            if (entryDoor == null) entryDoor = GetComponent<DoorInteractable>();
            if (entryDoor != null) entryDoor.InteractionCompleted += HandleDoorInteractionCompleted;
        }

        protected override void OnDisable()
        {
            if (entryDoor != null) entryDoor.InteractionCompleted -= HandleDoorInteractionCompleted;
            base.OnDisable();
        }

        protected override async void BeginInteraction(InteractionContext context)
        {
            if (entryDoor != null)
            {
                CompleteInteraction();
                return;
            }

            await EnterStoryRouteAsync(context);
            CompleteInteraction();
        }

        private void HandleDoorInteractionCompleted(InteractableBase interactable)
        {
            if (entryDoor != null && entryDoor.IsOpen) _ = EnterStoryRouteAsync(default);
        }

        private async System.Threading.Tasks.Task EnterStoryRouteAsync(InteractionContext context)
        {
            if (transitionStarted) return;
            if (SceneTransitionService.Instance == null)
            {
                context.ShowFeedback("The scene transition service is unavailable.");
                return;
            }

            transitionStarted = true;
            context.ShowFeedback("Opening the chapter route.");
            await SceneTransitionService.Instance.LoadPlayingSceneAsync(StorySceneName);
        }
    }
}

