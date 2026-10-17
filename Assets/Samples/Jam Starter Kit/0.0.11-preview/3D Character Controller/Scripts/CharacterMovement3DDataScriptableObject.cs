using System.Diagnostics;
using UnityEngine;

namespace Samples.CharacterController3D.Scripts
{
    [CreateAssetMenu(fileName = "3D Character Movement Data", menuName = "Character Controller/3D Movement Data")]
    public class CharacterMovement3DDataScriptableObject : ScriptableObject
    {
        public float Gravity { get; private set; }
        public float InitialJumpVelocity { get; private set; }
        
        //Locomotion
        //------------------------------------------------//
        [Header("Locomotion")]
        public float maxSpeed;
        [Min(0f)] public float GroundAcceleration = 100f;
        [Min(0f)] public float AirAcceleration = 50f;
        public AnimationCurve accelerationFactorFromDot;
        public float maxAccelerationForce;
        public AnimationCurve maxAccelerationForceFactorFromDot;
        public Vector3 forceScale;
        public float gravityScaleDrop;
        
        //Grounded/Collision Checks
        //------------------------------------------------//
        [Header("Grounded/Collision Checks")] 
        public LayerMask GroundLayer;
        //public float GroundDetectionRayLength = 0.02f;
        
        //Jump
        //------------------------------------------------//
        [Header("Jump"), Range(1.0f, 1.1f)] 
        public float JumpHeightCompensationFactor = 1.054f;
        public float MaxVerticalVelocity = 50f;
        public float JumpHeight = 6.5f;
        public float TimeTillJumpApex = 0.35f;
        [Range(0.1f, 5f)]
        public float GravityOnReleaseMultiplier = 2f;
        public float MaxFallSpeed = 26f;
        [Range(0, 10)]
        public int NumberOfJumpsAllowed = 2;
        
        //Jump Cut
        //------------------------------------------------//
        [Header("Jump Cut"), Range(0f, 1f)] 
        public float TimeForUpwardsCancel = 0.027f;
        
        //Jump Apex
        //------------------------------------------------//
        [Header("Jump Apex"), Range(0f, 1f)]
        public float ApexThreshold;
        public float ApexHangTime = 0.075f;
        
        //Jump Buffer
        //------------------------------------------------//
        [Header("Jump Buffer"), Range(0f, 1f)] 
        public float JumpBufferTime = 0.125f;

        //Jump Coyote Time
        //------------------------------------------------//
        [Header("Jump Coyote Time"), Range(0f, 1f)]
        public float JumpCoyoteTime = 0.1f;

        //Character Balancing
        //------------------------------------------------//
        [Header("Character Balancing")]
        [Min(0f)]
        public float rideHeight;
        [Min(0f)]
        public float springStrength;
        [Min(0f)]
        public float springDamper;
        [Min(0f)]
        public float uprightStrength;
        [Min(0f)]
        public float uprightDamper;

        private void OnEnable()
        {
            CalculateValues();
        }

        private void CalculateValues()
        {
            var adjustedJumpHeight = JumpHeight * JumpHeightCompensationFactor;
            Gravity = -(2f * adjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
            InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillJumpApex;
        }
         
        [Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            CalculateValues();
        }
    }
}