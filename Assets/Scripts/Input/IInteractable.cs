namespace DigitalLogicLab.Input
{
    /// <summary>
    /// Interface for interactive 3D and UI lab components.
    /// Enables desktop raycasting or future XR Ray Interactors to trigger interaction.
    /// </summary>
    public interface IInteractable
    {
        void Interact();
        string GetInteractPrompt();
        bool IsInteractable();
        void SetHover(bool isHovered);
    }
}
