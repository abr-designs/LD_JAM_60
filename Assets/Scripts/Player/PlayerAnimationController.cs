using GameInput;
using ldjam60.Interactables;
using UnityEngine;

namespace ldjam60.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int CarryingAnimationHash = Animator.StringToHash("Carrying");
        private static readonly int GatherAnimationHash = Animator.StringToHash("Gather");
        private static readonly int SpeedAnimationHash = Animator.StringToHash("Speed");

        private bool m_hasInput;
        private float m_animationSpeed, m_speedVelocity;

        [SerializeField]
        private Animator animator;
        
        private void OnEnable()
        {
            InteractableController.OnCarryingInteractable += OnCarryingInteractable;
            GameInputDelegator.OnMovementChanged += OnMovementChanged;
        }

        private void LateUpdate()
        {
            m_animationSpeed = Mathf.SmoothDamp(m_animationSpeed,
                m_hasInput ? 1f : 0f,
                ref m_speedVelocity, 
                0.25f);
            
            
            animator.SetFloat(SpeedAnimationHash, m_animationSpeed);
        }


        private void OnDisable()
        {
            InteractableController.OnCarryingInteractable -= OnCarryingInteractable;
            GameInputDelegator.OnMovementChanged -= OnMovementChanged;
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
        
        private void OnMovementChanged(Vector2 input)
        {
            m_hasInput = input != Vector2.zero;
        }
    }
}