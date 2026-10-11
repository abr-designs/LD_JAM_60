using UnityEngine.Assertions;

namespace Audio.Music
{
    public static class MUSICExtensions
    {
        public static void PlayMusic(this MUSIC music)
        {
            MusicController.PlayMusic(music);
        }
    }
}