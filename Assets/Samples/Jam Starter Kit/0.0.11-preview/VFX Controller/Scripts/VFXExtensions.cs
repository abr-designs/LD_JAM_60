using UnityEngine;

namespace VisualFX
{
    public static class VFXExtension
    {
        public static GameObject PlayAtLocation(this VFX vfx, Vector3 worldPosition, float scale = 1f, bool keepAlive = false)
        {
            return VFXManager.PlayAtLocation(vfx, worldPosition, scale, keepAlive);
        }
    }
}