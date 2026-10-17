using UnityEngine;

namespace ldjam60.Interactables
{
    public interface IInteractable
    {
        GameObject gameObject { get; }
        
        bool IsInteractable { get; }
        float InteractableRadius { get; }
        
        void Interact();
    }
}