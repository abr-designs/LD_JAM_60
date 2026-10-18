using UnityEngine;

namespace ldjam60.Interactables
{
    public interface IInteractable
    {
        bool enabled { get; }
        GameObject gameObject { get; }
        Transform transform { get; }
        Vector3 WorldPosition { get; }
        
        bool IsInteractable { get; }
        float InteractableRadius { get; }
        
        void Interact();
    }
}