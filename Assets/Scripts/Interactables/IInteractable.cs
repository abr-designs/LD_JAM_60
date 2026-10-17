using UnityEngine;

namespace ldjam60
{
    public interface IInteractable
    {
        GameObject gameObject { get; }
        
        bool IsInteractable { get; }
        float InteractableRadius { get; }
        
        void Interact();
    }
}