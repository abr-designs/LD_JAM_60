using GameInput;
using UnityEngine;

namespace ldjam60.Utilities
{
    [DefaultExecutionOrder(-100)]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")] [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float shoulderOffset = 0.5f;

        [Header("Camera")] [SerializeField] private float distance = 4f;
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;

        [Header("Input")]
        //[SerializeField] private InputActionReference lookAction;

        [Header("Smoothing")]
        [SerializeField]
        private float rotationSmoothTime = 0.05f;

        [SerializeField] private float positionSmoothTime = 0.08f;

        private float yaw;
        private float pitch;

        private float targetYaw;
        private float targetPitch;
        private float yawVelocity;
        private float pitchVelocity;

        private Vector3 positionVelocity;
        private bool enabledLookAction;

        private Vector2 lastMouseDelta;

        private void OnEnable()
        {
            /*if (lookAction == null || lookAction.action == null)
                return;

            if (!lookAction.action.enabled)
            {
                lookAction.action.Enable();
                enabledLookAction = true;
            }*/
            GameInputDelegator.OnMouseMoved += OnMouseMoved;
        }

        private void OnMouseMoved(Vector2 mouseDelta)
        {
            lastMouseDelta = mouseDelta;
        }

        private void OnDisable()
        {
            /*if (enabledLookAction && lookAction != null &&
                lookAction.action != null)
            {
                lookAction.action.Disable();
                enabledLookAction = false;
            }*/
            GameInputDelegator.OnMouseMoved -= OnMouseMoved;
        }

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;

            yaw = targetYaw = angles.y;
            pitch = targetPitch = angles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Read mouse delta.
            targetYaw += lastMouseDelta.x * mouseSensitivity;
            targetPitch -= lastMouseDelta.y * mouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // Smooth camera rotation.
            yaw = Mathf.SmoothDampAngle(
                yaw, targetYaw, ref yawVelocity, rotationSmoothTime);

            pitch = Mathf.SmoothDampAngle(
                pitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // Calculate target pivot in world space.
            Vector3 pivot = target.TransformPoint(localOffset);

            // Position the camera behind the pivot and over the shoulder.
            Vector3 desiredPosition =
                pivot
                - rotation * Vector3.forward * distance
                + rotation * Vector3.right * shoulderOffset;

            // Smooth camera position and rotation.
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            transform.rotation = rotation;
        }
    }
}