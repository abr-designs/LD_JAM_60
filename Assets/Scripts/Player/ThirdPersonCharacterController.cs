
using System;
using GameInput;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;
    [SerializeField] private bool rotateTowardsMovement = true;

    [Header("Slope & Grounding")]
    [SerializeField, Range(0f, 90f)] private float slopeLimit = 45f;
    [SerializeField, Min(0f)] private float groundStickForce = 2f;
    [SerializeField, Min(0f)] private float gravity = 25f;
    [SerializeField, Min(0f)] private float groundCheckDistance = 0.2f;

    private CharacterController characterController;
    private Vector2 moveInput;
    private float verticalVelocity;

    public Vector2 MoveInput => moveInput;
    public bool IsGrounded => characterController.isGrounded;

    private void OnEnable()
    {
        GameInputDelegator.OnMovementChanged += SetMoveInput;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterController.slopeLimit = slopeLimit;
    }
    
    private void OnDisable()
    {
        GameInputDelegator.OnMovementChanged -= SetMoveInput;
    }

    /// <summary>
    /// Sets movement input from an external input system.
    /// X = left/right, Y = forward/backward.
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void Update()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        Vector3 moveDirection = GetCameraRelativeDirection();

        if (rotateTowardsMovement && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        bool grounded = characterController.isGrounded
                        || IsNearGround();

        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -groundStickForce;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector3 GetCameraRelativeDirection()
    {
        Vector3 forward;
        Vector3 right;

        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(
                cameraTransform.forward, Vector3.up
            ).normalized;

            right = Vector3.ProjectOnPlane(
                cameraTransform.right, Vector3.up
            ).normalized;
        }
        else
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }

        Vector3 direction =
            right * moveInput.x + forward * moveInput.y;

        return Vector3.ClampMagnitude(direction, 1f);
    }

    private bool IsNearGround()
    {
        Vector3 origin = transform.TransformPoint(
            characterController.center
        );

        float radius = characterController.radius * 0.9f;
        float distance =
            characterController.height * 0.5f
            - characterController.radius
            + groundCheckDistance;

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void OnValidate()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterController != null)
            characterController.slopeLimit = slopeLimit;
    }
}
