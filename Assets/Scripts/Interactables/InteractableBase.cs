using UnityEngine;

namespace ldjam60.Interactables
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        public bool IsInteractable { get; }
        public float InteractableRadius { get; }
        
        protected void OnEnable()
        {
            InteractableController.RegisterInteractable(this);
        }

        protected void OnDisable()
        {
            InteractableController.UnregisterInteractable(this);
        }


        public abstract void Interact();
    }
}