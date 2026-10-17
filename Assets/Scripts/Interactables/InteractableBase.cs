using UnityEngine;

namespace ldjam60.Interactables
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        public Vector3 WorldPosition => transform.position;
        public abstract bool IsInteractable { get; }
        [field: SerializeField] 
        public float InteractableRadius { get; protected set; }

        
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