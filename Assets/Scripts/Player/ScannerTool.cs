using System.Collections.Generic;
using ldjam60.Interactables;
using UnityEngine;

namespace ldjam60.Player
{
    public class ScannerTool : MonoBehaviour
    {
        [Header("Scanner Settings")]
        [Tooltip("How often to run the check in milliseconds.")]
        [SerializeField] private float checkIntervalMs = 200f;
        [Tooltip("Maximum range to detect enemies or drives.")]
        [SerializeField] private float maxDetectionRange = 10f;

        [Header("References")]
        [Tooltip("Assign your 3 light GameObjects here.")]
        [SerializeField] private List<GameObject> lightObjects = new List<GameObject>(3);
        [Tooltip("The Renderer containing the emissive material.")]

        [Header("Colors")]
        [ColorUsage(true,true)]
        [SerializeField] private Color enemyColor = Color.red;
        [ColorUsage(true,true)]
        [SerializeField] private Color driveColor = Color.green;
        [SerializeField] private Color offColor = Color.black;

        // Cached lists populated on Start
        private List<Drone> _cachedEnemies = new List<Drone>();
        private List<InteractableDrive> _cachedDrives = new List<InteractableDrive>();

        // Cached renderers and material instances for each of the 3 lights
        private List<Renderer> _lightRenderers = new List<Renderer>();
        private List<Material> _lightMaterialInstances = new List<Material>();

        private float _nextCheckTime;

        private void Start()
        {
            // Cache all enemy and drive objects present in the scene at startup
            _cachedEnemies.AddRange(FindObjectsByType<Drone>());
            _cachedDrives.AddRange(FindObjectsByType<InteractableDrive>());

            // Cache renderers and create unique material instances for each light object
            foreach (var lightObj in lightObjects)
            {
                if (lightObj != null)
                {
                    Renderer rend = lightObj.GetComponent<Renderer>();
                    _lightRenderers.Add(rend);

                    if (rend != null)
                    {
                        // Create an instance so we don't modify the shared material globally
                        Material mat = new Material(rend.sharedMaterial);
                        rend.material = mat;
                        _lightMaterialInstances.Add(mat);
                    }
                    else
                    {
                        _lightMaterialInstances.Add(null);
                    }
                }
                else
                {
                    _lightRenderers.Add(null);
                    _lightMaterialInstances.Add(null);
                }
            }

            // Ensure lights start off
            SetLightsState(offColor, 0);
        }

        private void Update()
        {
            // Convert current time to milliseconds for interval checking
            if (Time.time * 1000f >= _nextCheckTime)
            {
                PerformScan();
                _nextCheckTime = (Time.time * 1000f) + checkIntervalMs;
            }
        }

        private void PerformScan()
        {
            // 1. Check for Enemies first (priority)
            float closestEnemyDist = GetClosestCachedDistance(_cachedEnemies, out bool enemyFound);
            if (enemyFound && closestEnemyDist <= maxDetectionRange)
            {
                int lightsToActive = CalculateLightCount(closestEnemyDist, maxDetectionRange);
                SetLightsState(enemyColor, lightsToActive);
                return;
            }

            // 2. If no enemies in range, check for Drives
            float closestDriveDist = GetClosestCachedDistance(_cachedDrives, out bool driveFound);
            if (driveFound && closestDriveDist <= maxDetectionRange)
            {
                int lightsToActive = CalculateLightCount(closestDriveDist, maxDetectionRange);
                SetLightsState(driveColor, lightsToActive);
                return;
            }

            // 3. Nothing in range - lights off
            SetLightsState(offColor, 0);
        }

        private float GetClosestCachedDistance<T>(List<T> objects, out bool found) where T : MonoBehaviour
        {
            found = false;
            float closestDist = Mathf.Infinity;

            foreach (var obj in objects)
            {
                if (obj == null) continue;

                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    found = true;
                }
            }

            return closestDist;
        }

        private int CalculateLightCount(float distance, float maxRange)
        {
            // Split the range into 3 distance tiers
            float tierSize = maxRange / 3f;

            if (distance <= tierSize) return 3;     // Very close: All 3 lights
            if (distance <= tierSize * 2f) return 2;  // Medium distance: 2 lights
            return 1;                                 // Far edge of range: 1 light
        }

        private void SetLightsState(Color color, int activeCount)
        {
            for (int i = 0; i < lightObjects.Count; i++)
            {
                if (lightObjects[i] == null) continue;

                bool shouldBeActive = i < activeCount;

                // Optional: Toggle GameObject active state if you want them to turn completely on/off
                // lightObjects[i].SetActive(shouldBeActive);

                // Update individual emissive material if present
                if (i < _lightMaterialInstances.Count && _lightMaterialInstances[i] != null)
                {
                    if (shouldBeActive)
                    {
                        _lightMaterialInstances[i].SetColor("_EmissionColor", color);
                        _lightMaterialInstances[i].EnableKeyword("_EMISSION");
                        DynamicGI.SetEmissive(_lightRenderers[i], color);
                    }
                    else
                    {
                        _lightMaterialInstances[i].SetColor("_EmissionColor", offColor);
                        _lightMaterialInstances[i].DisableKeyword("_EMISSION");
                        DynamicGI.SetEmissive(_lightRenderers[i], Color.black);
                    }
                }
            }
        }
    }
}