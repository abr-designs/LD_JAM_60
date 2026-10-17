using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace ldjam60.Interactables
{
    public class InteractableController : HiddenSingleton<InteractableController>
    {
        [SerializeField, Min(0f)]
        private float interactableRadius;
        
        private static List<IInteractable> s_interactables;
        
        public static void RegisterInteractable(IInteractable interactable)
        {
            if(s_interactables == null)
                s_interactables =  new List<IInteractable>();
            
            if(s_interactables.Contains(interactable))
            {
                Debug.LogError($"Interactable already registered...", interactable.gameObject);
                return;
            }
            
            s_interactables.Add(interactable);
        }

        public static void UnregisterInteractable(IInteractable interactable)
        {
            if (s_interactables == null)
                return;
            
            
        }
    }
}