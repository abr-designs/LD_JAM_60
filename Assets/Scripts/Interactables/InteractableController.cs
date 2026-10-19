using System;
using System.Collections.Generic;
using GameInput;
using Samples.CharacterController3D.Scripts;
using UnityEngine;
using Utilities;

namespace ldjam60.Interactables
{
    public class InteractableController : HiddenSingleton<InteractableController>
    {
        public static event Action<IInteractable> OnNewInteractableInRange;
        public static event Action<IInteractable> OnCarryingInteractable;

        public static IInteractable HoldingInteractable { get; private set; }
        public static bool PauseScan { get; set; }
        
        [SerializeField, Range(0f, 1f)]
        private float speedEffector = 0.75f;
        private float m_startingSpeed;
        

        [SerializeField, Min(0f)]
        private float interactableRadius;
        
        private static List<IInteractable> s_interactables;
        
        private Transform m_playerTransform;

        [SerializeField, Min(0f)]
        private float updateInterval;
        private float m_updateTimer;
        
        private InteractableBase m_closestInteractable;

        [Header("Carry Info")]
        [SerializeField]
        private Transform carryJointTransform;
        [SerializeField]
        private Vector3 localCarryPositionOffset;
        [SerializeField]
        private Vector3 localCarryRotationOffset;

        //Unity Functions
        //================================================================================================================//

        private void OnEnable()
        {
            GameInputDelegator.OnInteractPressChanged += OnInteractPressed;
        }

        private void Start()
        {
            m_startingSpeed = CharacterController3D.SpeedMult;
            
            m_playerTransform = transform;
            m_updateTimer = updateInterval;
        }

        private void Update()
        {
            //If something has control, we don't want to start interacting with new things by accident
            if (PauseScan)
                return;
            
            if(m_updateTimer > 0f)
            {
                m_updateTimer -= Time.deltaTime;
                return;
            }
            
            m_updateTimer =  updateInterval;

            var closest = FindClosestInteractableInRange();

            if (closest == m_closestInteractable)
                return;
            
            m_closestInteractable = (InteractableBase)closest;
            OnNewInteractableInRange?.Invoke(closest);
        }

        private void OnDisable()
        {
            GameInputDelegator.OnInteractPressChanged -= OnInteractPressed;
        }
        
        private void OnDestroy()
        {
            GameInputDelegator.OnInteractPressChanged -= OnInteractPressed;

            //Shutdown Cleanup
            OnNewInteractableInRange = null;
            OnCarryingInteractable = null;
            HoldingInteractable = null;
            PauseScan = false;
            s_interactables = null;
        }

        //InteractionController Functions
        //================================================================================================================//

        private IInteractable FindClosestInteractableInRange()
        {
            if(s_interactables == null || s_interactables.Count == 0)
                return null;
            
            var closestIndex = -1;
            var closestDistance = float.MaxValue;
            var closestPriority = int.MinValue;

            for (var i = 0; i < s_interactables.Count; i++)
            {
                var interactable = s_interactables[i];

                if (!interactable.enabled || !interactable.IsInteractable)
                    continue;

                //If the player is holding something, don't try to hold another?
                //TODO See if the player should swap what they're holding
                if(HoldingInteractable != null && interactable is ICanBeCarried)
                    continue;
                
                var interactableSqrDistance = (interactable.WorldPosition - m_playerTransform.position).sqrMagnitude;
                var combinedRadius = interactableRadius + interactable.InteractableRadius;

                if (interactableSqrDistance > combinedRadius * combinedRadius)
                    continue;

                if (interactable.InteractionPriority < closestPriority)
                    continue;

                if (interactable.InteractionPriority == closestPriority &&
                    interactableSqrDistance >= closestDistance)
                    continue;
                
                closestIndex = i;
                closestDistance = interactableSqrDistance;
                closestPriority = interactable.InteractionPriority;
            }

            return closestIndex == -1 ? null : s_interactables[closestIndex];
        }

        //Callbacks
        //================================================================================================================//

        private void OnInteractPressed(bool isPressed)
        {
            if (!isPressed)
                return;
            
            if (m_closestInteractable is { IsInteractable: true })
            {
                m_closestInteractable.Interact();
                return;
            }
            
            if (HoldingInteractable is { IsInteractable: true })
            {
                HoldingInteractable.Interact();
                return;
            }
            
        }

        //Static Functions
        //================================================================================================================//
        
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

            if (!s_interactables.Contains(interactable))
                return;
            
            s_interactables.Remove(interactable);
        }

        public static void CarryInteractable(IInteractable interactable)
        {
            if (interactable is not ICanBeCarried canBeCarried)
                return;
            
            HoldingInteractable = interactable;
            canBeCarried.Carry();

            var carryTransform = HoldingInteractable.gameObject.transform;
            carryTransform.SetParent(Instance.carryJointTransform, false);
            carryTransform.SetLocalPositionAndRotation(
                Instance.localCarryPositionOffset, 
                Quaternion.Euler(Instance.localCarryRotationOffset));
            
            OnCarryingInteractable?.Invoke(HoldingInteractable);
            CharacterController3D.SpeedMult = Instance.speedEffector;
        }

        public static void DropInteractable()
        {
            if (HoldingInteractable is not ICanBeCarried canBeCarried)
                return;

            HoldingInteractable.transform.SetParent(null);
            
            canBeCarried.Drop();
            HoldingInteractable = null;
            OnCarryingInteractable?.Invoke(null);
            CharacterController3D.SpeedMult = Instance.m_startingSpeed;
        }
        
        //Editor Functions
        //================================================================================================================//

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            
            Gizmos.DrawWireSphere(transform.position, interactableRadius);
            
            if (s_interactables == null || s_interactables.Count == 0)
                return;
            
            foreach (var interactable in s_interactables)
            {
                Gizmos.color = interactable == m_closestInteractable ? Color.green : Color.white;
                
                Gizmos.DrawWireSphere(interactable.WorldPosition, interactable.InteractableRadius);
            }
        }
    }
}
