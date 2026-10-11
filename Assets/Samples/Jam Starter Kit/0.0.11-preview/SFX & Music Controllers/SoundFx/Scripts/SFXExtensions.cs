using UnityEngine;

namespace Audio.SoundFX
{
    public static class SFXExtensions
    {
        public static void PlaySoundAtLocation(this SFX sfx, Vector3 worldPosition, float pitch = 1f)
        {
            SFXManager.PlaySoundAtLocation(sfx, worldPosition, pitch);
        }
        
        public static void PlaySound(this SFX sfx, float volume = 1f, float pitch = 1f)
        {
            SFXManager.PlaySound(sfx, volume, pitch);
        }

    }
}