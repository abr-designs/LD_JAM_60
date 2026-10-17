using System;
using System.Collections;
using GameInput;
using ldjam60.Player;
using Samples.CharacterController3D.Scripts;
using UnityEngine;

namespace ldjam60.Interactables
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    
    public class InteractableDrive : InteractableBase, ICanBeCarried
    {
        //If the player isn't already holding something & if they are, it's not this
        public override bool IsInteractable => InteractableController.HoldingInteractable == null 
                                               || InteractableController.HoldingInteractable == this;

        private static PlayerAnimationController s_playerAnimationController;
        
        [SerializeField, Range(0f, 1f)]
        private float speedEffector;

        private float m_startingSpeed;
        
        private Rigidbody m_rigidbody;
        private Collider m_collider;

        private bool m_isBusy;
        private Coroutine m_pickupCoroutine;
        [SerializeField, Min(0f)]
        private float pickupAnimationTime;

        private void Start()
        {
            if(s_playerAnimationController == null)
                s_playerAnimationController = FindAnyObjectByType<PlayerAnimationController>();
            
            m_startingSpeed = CharacterController3D.SpeedMult;
            m_rigidbody =  GetComponent<Rigidbody>();
            m_collider =  GetComponent<Collider>();
        }

        //IInteractable Functions
        //================================================================================================================//

        public override void Interact()
        {
            if (m_isBusy)
                return;
            
            if(InteractableController.HoldingInteractable == null)
            {
                //TODO Start to carry the drive
                //InteractableController.CarryInteractable(this);
                m_pickupCoroutine = StartCoroutine(PickupCoroutine(pickupAnimationTime));
            }
            else if(InteractableController.HoldingInteractable == this)
            {
                //TODO Drop the drive
                InteractableController.DropInteractable();
            }

            return;
            
            //----------------------------------------------------------//
            //FIXME This should be on the player controller, but there's some Order of Operations considerations to be making
            IEnumerator PickupCoroutine(float animationTime)
            {
                m_isBusy = true;
                s_playerAnimationController.PlayPickupAnimation();
                GameInputDelegator.SetInputLock(true);
            
                //TODO Play Animation
                yield return new WaitForSeconds(animationTime);
            
                InteractableController.CarryInteractable(this);

                m_isBusy = false;
                m_pickupCoroutine = null;
                s_playerAnimationController.ResetPickupAnimation();
                GameInputDelegator.SetInputLock(false);
            }
            
            //----------------------------------------------------------//
        }

        //ICanBeCarried Functions
        //================================================================================================================//

        public void Carry()
        {
            m_rigidbody.isKinematic = true;
            m_collider.enabled = false;
            CharacterController3D.SpeedMult = speedEffector;
        }

        public void Drop()
        {
            m_rigidbody.isKinematic = false;
            m_collider.enabled = true;
            CharacterController3D.SpeedMult = m_startingSpeed;
        }
        
        //================================================================================================================//


    }
}