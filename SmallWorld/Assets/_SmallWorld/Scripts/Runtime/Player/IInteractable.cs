namespace SmallWorld.Player
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void SetFocused(bool focused);
        bool TryInteract(InteractionContext context);
    }
}
