using UnityEngine;
using Utilities;

namespace ldjam60.Player
{
    [DefaultExecutionOrder(100)]
    public class CameraShake : HiddenSingleton<CameraShake>
    {
        [Header("Defaults")] 
        [SerializeField, Min(0f)]
        private float defaultIntensity = 0.15f;

        [SerializeField, Min(0f)] private float defaultDuration = 0.25f;

        private float intensity;
        private float duration;
        private float elapsed;
        private bool shaking;
        
        private Vector3 previousPositionOffset;
        private Quaternion previousRotationOffset = Quaternion.identity;

        /// <summary>
        /// Shake using the component's default intensity and duration.
        /// </summary>
        public static void Shake()
        {
            Instance?.StartShake(Instance.defaultIntensity, Instance.defaultDuration);
        }

        /// <summary>
        /// Shake using a custom intensity and duration.
        /// </summary>
        public static void Shake(float intensity, float duration)
        {
            Instance?.StartShake(intensity, duration);
        }

        private void StartShake(float amount, float time)
        {
            intensity = Mathf.Max(0f, amount);
            duration = Mathf.Max(0f, time);
            elapsed = 0f;
            shaking = duration > 0f && intensity > 0f;
        }

        private void LateUpdate()
        {
            // Remove only the shake applied last frame.
            transform.position -= previousPositionOffset;
            transform.rotation *= Quaternion.Inverse(previousRotationOffset);

            previousPositionOffset = Vector3.zero;
            previousRotationOffset = Quaternion.identity;

            if (!shaking)
                return;

            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float strength = intensity * (1f - progress);

            previousPositionOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * strength;

            Vector3 rotationOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * (strength * 10f);

            previousRotationOffset = Quaternion.Euler(rotationOffset);

            transform.position += previousPositionOffset;
            transform.rotation *= previousRotationOffset;

            if (elapsed >= duration)
                shaking = false;
        }
    }
}