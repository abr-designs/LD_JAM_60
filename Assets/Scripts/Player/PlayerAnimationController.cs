using System;
using ldjam60.Interactables;
using UnityEngine;

namespace ldjam60.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int CarryingAnimationHash = Animator.StringToHash("Carrying");
        private static readonly int GatherAnimationHash = Animator.StringToHash("Gather");

        [SerializeField]
        private Animator animator;
        
        private void OnEnable()
        {
            InteractableController.OnCarryingInteractable += OnCarryingInteractable;
        }
        

        private void OnDisable()
        {
            InteractableController.OnCarryingInteractable -= OnCarryingInteractable;
        }

        //AnimationController Functions
        //================================================================================================================//

        public void PlayPickupAnimation()
        {
            animator.SetTrigger(GatherAnimationHash);
        }

        public void ResetPickupAnimation()
        {
            animator.ResetTrigger(GatherAnimationHash);
        }

        //Callbacks
        //================================================================================================================//

        private void OnCarryingInteractable(IInteractable interactable)
        {
            animator.SetBool(CarryingAnimationHash, interactable != null);
        }
    }
}