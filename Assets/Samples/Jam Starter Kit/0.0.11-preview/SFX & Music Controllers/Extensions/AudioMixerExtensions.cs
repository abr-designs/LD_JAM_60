namespace Audio
{
    public static class AudioMixerExtensions
    {
        public static void SetVolume(this UnityEngine.Audio.AudioMixer instance, float volume)
        {
            var v = UnityEngine.Mathf.Log10(volume) * 20;
            instance.SetFloat(ISetVolume.VOLUME_ID, v);
        }
    }
}