using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using Utilities;

namespace Audio.Music
{
    public class MusicController : HiddenSingleton<MusicController>, ISetVolume
    {
        //------------------------------------------------//
        
        [Serializable]
        private class MusicData
        {
            [SerializeField] internal string name;
            public MUSIC type;
            public AudioClip audioClip;
            [Range(0f, 1f)]
            public float targetVolume;
        }

        //------------------------------------------------//

        [SerializeField]
        private AudioMixerGroup musicAudioMixer;
        [SerializeField]
        private MUSIC startMusic;

        [SerializeField, Min(0)]
        private float crossFadeTime;

        [SerializeField]
        private MusicData[] musicDatas;

        private MUSIC _currentMusic;
        private AudioSource _currentMusicSource;
        
        [SerializeField]
        private AudioSource musicSourcePrefab;
        
        private Dictionary<MUSIC, MusicData> _musicDataDictionary;
        
        private bool _isReady;
        
        //Unity Functions
        //============================================================================================================//

        // Start is called before the first frame update
        private void Start()
        {
            Assert.IsNotNull(musicAudioMixer);
            
            InitMusicLibrary();

            if (startMusic == MUSIC.NONE)
                return;
            
            PlayMusic(startMusic);
        }

        //============================================================================================================//

        private void InitMusicLibrary()
        {
            var count = musicDatas.Length;
            _musicDataDictionary = new Dictionary<MUSIC, MusicData>(count);
            for (var i = 0; i < count; i++)
            {
                var vfxData = musicDatas[i];
                _musicDataDictionary.Add(vfxData.type, vfxData);
            }
            
            _isReady = true;
        }

        //============================================================================================================//
        public static void PlayMusic(MUSIC music)
        {
            Assert.IsNotNull(Instance, $"Missing the {nameof(MusicController)} in the Scene!!");
            Instance._PlayMusic(music);
        }
        private void _PlayMusic(MUSIC music)
        {
            if (!_isReady)
                return;
            
            //Don't need to fade into music that is already playing
            if (music == _currentMusic)
                return;

            var newMusic = GetMusicData(music);
            var newMusicSource = CreateMusicSource(newMusic);
            
            if (crossFadeTime == 0f)
            {
                if(_currentMusicSource)
                    Destroy(_currentMusicSource.gameObject);
                _currentMusic = music;
                _currentMusicSource = newMusicSource;
                return;
            }

            StartCoroutine(FadeInCoroutine(newMusicSource, newMusic.targetVolume, crossFadeTime));

            if(_currentMusicSource)
                StartCoroutine(FadeOutCoroutine(_currentMusicSource, crossFadeTime));
            
            _currentMusic = music;
            _currentMusicSource = newMusicSource;
        }

        private AudioSource CreateMusicSource(MusicData musicData)
        {
            var newMusicSource = Instantiate(musicSourcePrefab, transform);
            newMusicSource.name = $"{musicData.type}_Music_AudioSource";
            newMusicSource.clip = musicData.audioClip;
            newMusicSource.volume = 0f;
            newMusicSource.Play();

            return newMusicSource;
        }
        
        private MusicData GetMusicData(MUSIC music)
        {
            if (_musicDataDictionary.TryGetValue(music, out var musicData) == false)
                return null;

            Assert.IsNotNull(musicData);

            return musicData;
        }
        //============================================================================================================//

        private static IEnumerator FadeInCoroutine(AudioSource targetAudioSource, float targetVolume, float time)
        {
            for (float t = 0f; t < time; t +=Time.deltaTime)
            {
                targetAudioSource.volume = Mathf.Lerp(0f, targetVolume, t / time);
                yield return null;
            }

            targetAudioSource.volume = targetVolume;
        }
        
        private static IEnumerator FadeOutCoroutine(AudioSource targetAudioSource, float time)
        {
            var startingVolume = targetAudioSource.volume;
            for (float t = 0; t < time; t+=Time.deltaTime)
            {
                targetAudioSource.volume = Mathf.Lerp(startingVolume, 0f, t / time);
                yield return null;
            }
            
            Destroy(targetAudioSource.gameObject);
        }

        //ISetVolume
        //============================================================================================================//
        
        //Based on: https://johnleonardfrench.com/the-right-way-to-make-a-volume-slider-in-unity-using-logarithmic-conversion/
        public void SetVolume(float volume) => Instance.musicAudioMixer.audioMixer.SetVolume(volume);
        
        //Unity Editor Functions
        //============================================================================================================//
        
#if UNITY_EDITOR

        private void OnValidate()
        {
            for (int i = 0; i < musicDatas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(musicDatas[i].name))
                    musicDatas[i].name = musicDatas[i].type.ToString();
            }

            UnityEditor.EditorUtility.SetDirty(this);

            var enumTypes = (MUSIC[])Enum.GetValues(typeof(MUSIC));

            foreach (var enumType in enumTypes)
            {
                Assert.IsTrue(musicDatas.Count(x => x.type == enumType) <= 1,
                    $"<b><color=\"red\">ERROR</color></b>\nMore than 1 MUSIC found in the MUSIC manager for {enumType}. <color=\"red\"><b>CAN ONLY HAVE 1</b></color>");
            }

        }

#endif
        //============================================================================================================//
    }
}
