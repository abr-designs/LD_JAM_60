using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Audio.SoundFX;
using UnityEngine;
using Utilities.Animations;
using Utilities.Enums;
using Utilities.Tweening;

namespace ldjam60.Interactables
{
    public class InteractablePort : InteractableBase
    {
        public override bool IsInteractable => CanInteract();

        public InteractableDrive Mounted { get; private set; }
        
        
        [Header("Audio")]
        [SerializeField]
        private AudioSource functionalAudioSource;
        [SerializeField, Min(0)]
        private float maxVolume;
        private float m_volumeVelocity;
        
        
        [Header("Animation")]
        [SerializeField]
        private Vector3 insertStartPosition;
        [SerializeField] 
        private Vector3 insertEndPosition;
        
        [SerializeField]
        private Vector3 insertStartRotation;
        [SerializeField] 
        private Vector3 insertEndRotation;

        
        
        
        [SerializeField]
        private float animationTime;
        [SerializeField]
        private AnimationCurve animationCurve;

        private TransformAnimator TransformAnimator;
        
        private List<InteractableDrive> m_completedInteractableDrives;
        
        
        private void Start()
        {
            m_completedInteractableDrives = new List<InteractableDrive>();
        }
        
        private void LateUpdate()
        {
            functionalAudioSource.volume = Mathf.SmoothDamp(
                functionalAudioSource.volume,
                Mounted != null ? maxVolume : 0f,
                ref m_volumeVelocity,
                0.3f);
        }

        //================================================================================================================//

        private bool CanInteract()
        {
            if(Mounted != null)
                return false;

            if (InteractableController.HoldingInteractable is not InteractableDrive interactableDrive)
                return false;

            if (m_completedInteractableDrives.Contains(interactableDrive))
                return false;

            return true;
        }

        public override void Interact()
        {
            if (Mounted != null)
                throw new ArgumentException();
            
            if (InteractableController.HoldingInteractable is not InteractableDrive interactableDrive)
                throw new ArgumentException();
            
            InteractableController.DropInteractable();

            TryMountDrive(interactableDrive);
        }

        private void TryMountDrive(InteractableDrive interactableDrive)
        {
            Mounted = interactableDrive;
            
            SFX.LOAD_DRIVE.PlaySound();
         
            m_completedInteractableDrives.Add(interactableDrive);

            interactableDrive.Carry();
            interactableDrive.enabled = false;
            
            //Reposition & Animate
            interactableDrive.transform.SetParent(transform, false);
            //interactableDrive.transform.localPosition = insertStartPosition;
            //interactableDrive.transform.up = transform.up;

            StartCoroutine(AnimationCoroutine());
            
            
            /*
            interactableDrive.transform.TweenTo(SPACE.LOCAL, insertEndPosition, animationTime, CURVE.EASE_IN_OUT,
                () =>
                {
                    
                });
                */

            IEnumerator AnimationCoroutine()
            {
                var halfTime = animationTime / 2f;
                var currentPosition = transform.localPosition;
                var currentRotation = transform.localRotation;
                var startRotation = Quaternion.Euler(insertStartRotation);
                var endRotation = Quaternion.Euler(insertEndRotation);

                for (var t = 0f; t <= halfTime; t += Time.deltaTime)
                {
                    transform.localPosition = Vector3.Lerp(currentPosition, insertStartPosition,
                        animationCurve.Evaluate(t / halfTime));
                    transform.localRotation = Quaternion.Slerp(currentRotation, startRotation,
                        animationCurve.Evaluate(t / halfTime));
                    yield return null;
                }


                for (var t = 0f; t <= halfTime; t += Time.deltaTime)
                {
                    transform.localPosition = Vector3.Lerp(insertStartPosition, insertStartPosition,
                        animationCurve.Evaluate(t / halfTime));
                    transform.localRotation =
                        Quaternion.Slerp(startRotation, endRotation, animationCurve.Evaluate(t / halfTime));

                    yield return null;
                }

                SFX.LOADING.PlaySound();
            }
        }

        //Unity Editor Functions
        //================================================================================================================//

        private void OnDrawGizmosSelected()
        {
            var localStartPosition = transform.TransformPoint(insertStartPosition);
            var localEndPosition = transform.TransformPoint(insertEndPosition);
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(localStartPosition, localEndPosition);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(localStartPosition, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(localEndPosition, 0.1f);
        }

        //================================================================================================================//

        
    }
}