using UnityEngine;

namespace ldjam60.Interactables
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        public Vector3 WorldPosition => transform.TransformPoint(interactionCenterOffset);
        public abstract bool IsInteractable { get; }
        public virtual int InteractionPriority => interactionPriority;

        [field: SerializeField] 
        public float InteractableRadius { get; protected set; }

        [SerializeField]
        private Vector3 interactionCenterOffset;
        [SerializeField]
        private int interactionPriority;

        
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
