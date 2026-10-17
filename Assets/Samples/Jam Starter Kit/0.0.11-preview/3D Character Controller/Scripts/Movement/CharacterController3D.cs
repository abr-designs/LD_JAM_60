#if !JAM_INPUT_DELEGATOR
#define OLD_INPUT_SYSTEM
#endif

using System;
using System.Diagnostics;
using UnityEngine;
using Utilities;

namespace Samples.CharacterController3D.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterController3D : MonoBehaviour
    {
        public bool IsGrounded => m_3dBalancer.Grounded;
        
        [SerializeField]
        private CharacterMovement3DDataScriptableObject characterMovementData;

        //To Add
        //------------------------------------------------//

        private Vector3 groundVelocity = Vector3.zero;
        private float speedFactor = 1f;
        private float maxAccelerationForceFactor = 1f;
        
        //Private Fields
        //------------------------------------------------//

        private Vector2 m_movementInput;
        private Vector3 m_adjustMovementDirection;
        private Transform m_cameraTransform;

        private Vector3 m_goalVelocity;
        private Rigidbody m_rigidbody;

        private Character3DBalancer m_3dBalancer;

        //Jump Vars
        //------------------------------------------------//
        public float VerticalVelocity { get; private set; }
        private bool m_isJumpPressed;
        private bool m_lastFrameJumpPressed;
        
        private bool m_isJumping;
        private bool m_isJumpAvailable;
        private bool m_isFalling;
        private bool m_isFastFalling;
        private float m_fastFallTime;
        private float m_fastFallReleaseSpeed;
        private int m_numberOfJumpsUsed;
        private bool m_isPastApexThreshold;
        private float m_apexPoint;
        private float m_timePastApexThreshold;
        
        // jump buffer vars
        private float m_jumpBufferTimer;
        private bool m_jumpReleasedDuringBuffer;

        // coyote time vars
        private float m_coyoteTimer;

        //============================================================================================================//

#if JAM_INPUT_DELEGATOR
        private void OnEnable()
        {
            GameInput.GameInputDelegator.OnMovementChanged += OnMovementChanged;
            GameInput.GameInputDelegator.OnJumpPressed += OnJumpPressed;
        }
#endif

        private void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_cameraTransform = Camera.main.transform;

            m_3dBalancer = GetComponent<Character3DBalancer>();
        }

        private void Update()
        {
            ProcessInputs();
            CountTimers();
            JumpInputChecks();
            
            m_adjustMovementDirection = GetCameraBasedMove(m_movementInput).normalized;
            m_3dBalancer?.FaceDirection(m_adjustMovementDirection);
        }

        private void FixedUpdate()
        {
            Jump();

            ApplyMoveForce(m_adjustMovementDirection,
                m_3dBalancer.Grounded
                    ? characterMovementData.GroundAcceleration
                    : characterMovementData.AirAcceleration);
        }

#if JAM_INPUT_DELEGATOR
        private void OnDisable()
        {
            GameInput.GameInputDelegator.OnMovementChanged -= OnMovementChanged;
            GameInput.GameInputDelegator.OnJumpPressed -= OnJumpPressed;
        }
#endif

        //Locomotion Functions
        //============================================================================================================//

        #region Locomotion

        private Vector3 GetCameraBasedMove(Vector2 moveInput)
        {
            var projectedCameraForward = Vector3.ProjectOnPlane(m_cameraTransform.forward.normalized, Vector3.up);
            var cameraRight = m_cameraTransform.right.normalized;

            return (projectedCameraForward * moveInput.y) + (cameraRight * moveInput.x);
        }

        private void ApplyMoveForce(Vector3 inputGoal, float acceleration)
        {
            var unitVelocity = m_goalVelocity.normalized;
            var velocityDot = Vector3.Dot(inputGoal, unitVelocity);
            var accel = acceleration * characterMovementData.accelerationFactorFromDot.Evaluate(velocityDot);
            var goalVelocity = inputGoal * (characterMovementData.maxSpeed * speedFactor);

            m_goalVelocity = Vector3.MoveTowards(m_goalVelocity, 
                goalVelocity + groundVelocity,
                accel * Time.fixedDeltaTime);

            var neededAcceleration = (m_goalVelocity - m_rigidbody.linearVelocity) / Time.fixedDeltaTime;

            var maxAcceleration = characterMovementData.maxAccelerationForce * characterMovementData.maxAccelerationForceFactorFromDot.Evaluate(velocityDot) *
                                  maxAccelerationForceFactor;

            neededAcceleration = Vector3.ClampMagnitude(neededAcceleration, maxAcceleration);
            
            m_rigidbody.AddForce(Vector3.Scale(neededAcceleration * m_rigidbody.mass, characterMovementData.forceScale));
        }

        #endregion //Locomotion

        //Jumping
        //============================================================================================================//
        
        #region Jump

        // Process vertical velocity
        private void JumpInputChecks()
        {
            // Player pressed jump button this frame -- start the jump buffer
            bool jumpPressedThisFrame = !m_lastFrameJumpPressed && m_isJumpPressed;
            bool jumpReleasedThisFrame = m_lastFrameJumpPressed && !m_isJumpPressed;
            
            // Jumping starts the buffer timer -- hitting ground within this timer will trigger a jump
            if (jumpPressedThisFrame)
            {
                m_jumpBufferTimer = characterMovementData.JumpBufferTime;
                m_jumpReleasedDuringBuffer = false;
            }

            if (jumpReleasedThisFrame)
            {
                if (m_jumpBufferTimer > 0f)
                    m_jumpReleasedDuringBuffer = true;

                if (m_isJumping && VerticalVelocity > 0f)
                {
                    if (m_isPastApexThreshold)
                    {
                        m_isPastApexThreshold = false;
                        m_isFastFalling = true;
                        m_fastFallTime = characterMovementData.TimeForUpwardsCancel;
                        VerticalVelocity = 0f;
                    }
                    else
                    {
                        m_isFastFalling = true;
                        m_fastFallReleaseSpeed = VerticalVelocity;
                    }
                }
            }

            //Initiate Jump with Jump buffering & coyote time
            //------------------------------------------------//

            if (m_jumpBufferTimer > 0f && !m_isJumping && (m_3dBalancer.Grounded || m_coyoteTimer > 0f))
            {
                DoJump(1);

                if (m_jumpReleasedDuringBuffer)
                {
                    m_isFastFalling = true;
                    m_fastFallReleaseSpeed = VerticalVelocity;
                }
            }
            //Double Jump
            //------------------------------------------------//
            else if (m_jumpBufferTimer > 0f && m_isJumping &&
                     m_numberOfJumpsUsed < characterMovementData.NumberOfJumpsAllowed)
            {
                m_isFastFalling = false;
                DoJump(1);
            }
            //Air Jump after Coyote Time Lapsed
            //------------------------------------------------//
            else if (m_jumpBufferTimer > 0f && m_isFalling && m_numberOfJumpsUsed < characterMovementData.NumberOfJumpsAllowed - 1)
            {
                DoJump(2);
                m_isFastFalling = false;
            }

            //Landed
            //------------------------------------------------//
            if ((m_isJumping || m_isFalling) && m_3dBalancer.Grounded && VerticalVelocity <= 0f)
            {
                m_isJumping = false;
                m_isFalling = false;
                m_isFastFalling = false;
                m_fastFallTime = 0f;
                m_isPastApexThreshold = false;
                m_numberOfJumpsUsed = 0;

                VerticalVelocity = 0.0f/*Physics2D.gravity.y*/;
            }
        }

        // Set velocity and animations for jump
        private void DoJump(int numberOfJumpsUsed)
        {
            if (!m_isJumping)
            {
                m_isJumping = true;
            }

            m_jumpBufferTimer = 0f;
            VerticalVelocity = characterMovementData.InitialJumpVelocity;
            m_numberOfJumpsUsed += numberOfJumpsUsed;
        }

        private void Jump()
        {

            //Apply Gravity While Jumping
            //------------------------------------------------//
            if (m_isJumping)
            {
                //Check for Head Bump
                //------------------------------------------------//
                //if (m_bumpedHead)
                //    m_isFastFalling = true;
                //Gravity on Ascending
                //------------------------------------------------//
                if (VerticalVelocity >= 0f)
                {
                    //Apex Controls
                    //------------------------------------------------//
                    m_apexPoint = Mathf.InverseLerp(characterMovementData.InitialJumpVelocity, 0f, VerticalVelocity);

                    if (m_apexPoint > characterMovementData.ApexThreshold)
                    {
                        if (!m_isPastApexThreshold)
                        {
                            m_isPastApexThreshold = true;
                            m_timePastApexThreshold = 0f;
                        }

                        if (m_isPastApexThreshold)
                        {
                            m_timePastApexThreshold += Time.fixedDeltaTime;
                            if (m_timePastApexThreshold < characterMovementData.ApexHangTime)
                                VerticalVelocity = 0f;
                            else
                                VerticalVelocity = -0.01f;
                        }
                    }
                    //Gravity on Ascending but not past Apex Threshold
                    //------------------------------------------------//
                    else
                    {
                        VerticalVelocity += characterMovementData.Gravity * Time.fixedDeltaTime;
                        if (m_isPastApexThreshold)
                        {
                            m_isPastApexThreshold = false;
                        }
                    }
                }
                //Gravity on Descending
                //------------------------------------------------//
                else if (!m_isFastFalling)
                {
                    VerticalVelocity += characterMovementData.Gravity *
                                        characterMovementData.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (VerticalVelocity < 0f)
                {
                    if (!m_isFalling)
                        m_isFalling = true;
                }
            }


            //Jump Cut
            //------------------------------------------------//
            if (m_isFastFalling)
            {
                if (m_fastFallTime >= characterMovementData.TimeForUpwardsCancel)
                {
                    VerticalVelocity += characterMovementData.Gravity *
                                        characterMovementData.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (m_fastFallTime < characterMovementData.TimeForUpwardsCancel)
                {
                    VerticalVelocity = Mathf.Lerp(m_fastFallReleaseSpeed, 0f,
                        (m_fastFallTime / characterMovementData.TimeForUpwardsCancel));
                }

                m_fastFallTime += Time.fixedDeltaTime;
            }

            //Normal Gravity While Falling
            //------------------------------------------------//

            if (!m_3dBalancer.Grounded && !m_isJumping)
            {
                if (!m_isFalling)
                    m_isFalling = true;

                VerticalVelocity += characterMovementData.Gravity * Time.fixedDeltaTime;
            }

            //Clamp Fall Speed
            //------------------------------------------------//
            VerticalVelocity = Math.Clamp(VerticalVelocity, -characterMovementData.MaxFallSpeed,
                characterMovementData.MaxVerticalVelocity);
            m_rigidbody.linearVelocity = new Vector3(m_rigidbody.linearVelocity.x, VerticalVelocity, m_rigidbody.linearVelocity.z);
        }

        #endregion
        
        //Timers
        //============================================================================================================//

        #region Timers

        private void CountTimers()
        {
            m_jumpBufferTimer -= Time.deltaTime;
            if (!m_3dBalancer.Grounded)
            {
                m_coyoteTimer -= Time.deltaTime;
            }
            else
            {
                m_coyoteTimer = characterMovementData.JumpCoyoteTime;
            }
        }

        #endregion
        
        //Callbacks
        //============================================================================================================//

#if JAM_INPUT_DELEGATOR
        private void OnMovementChanged(Vector2 movementValue)
        {
            m_movementInput = movementValue;
        }
        
        private void OnJumpPressed(bool pressed)
        {
            m_isJumpPressed = pressed;
        }
#endif

        [Conditional("OLD_INPUT_SYSTEM")]
        private void ProcessInputs()
        {
            InputHelper.AxisInput(KeyCode.W, KeyCode.S, ref m_movementInput.y);
            InputHelper.AxisInput(KeyCode.D, KeyCode.A, ref m_movementInput.x);

            m_isJumpPressed = Input.GetKey(KeyCode.Space);
        }
        
        //============================================================================================================//
    }
}
