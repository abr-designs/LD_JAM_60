using Audio.Music;
using UnityEngine;
using UnityEngine.Audio;
using Utilities;

namespace Audio
{
    public class VolumeController : HiddenSingleton<VolumeController>, ISetVolume
    {
        [SerializeField]
        private AudioMixerGroup masterAudioMixer;
        [SerializeField]
        private SFXManager sfxVolume;
        [SerializeField]
        private MusicController musicVolume;

        public static void SetMasterVolume(float volume) => Instance?.SetVolume(volume);
        public static void SetMusicVolume(float volume) => Instance?.musicVolume?.SetVolume(volume);
        public static void SetSFXVolume(float volume) => Instance?.sfxVolume?.SetVolume(volume);
        
        //Based on: https://johnleonardfrench.com/the-right-way-to-make-a-volume-slider-in-unity-using-logarithmic-conversion/
        public void SetVolume(float volume) => masterAudioMixer.audioMixer.SetVolume(volume);
    }
}