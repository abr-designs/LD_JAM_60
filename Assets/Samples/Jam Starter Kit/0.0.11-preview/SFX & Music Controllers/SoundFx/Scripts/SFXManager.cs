using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using Utilities;
using Random = UnityEngine.Random;

namespace Audio
{
    public class SFXManager : HiddenSingleton<SFXManager>, ISetVolume
    {
        //============================================================================================================//
        [Serializable]
        private class SfxData
        {
            [SerializeField] internal string name;
            public SFX type;
            [SerializeField]
            private AudioClip[] audioClips;

            [Range(0f, 1f)]
            public float volume;

            [SerializeField, Min(0)]
            public int maxPlaying;

            public AudioClip GetRandomAudioClip()
            {
                return audioClips.Length == 1 ? audioClips[0] : audioClips[Random.Range(0, audioClips.Length)];
            }
        }

        //============================================================================================================//

        [SerializeField]
        private AudioMixerGroup sfxAudioMixer;
        [SerializeField]
        private AudioSource sfxAudioSource;

        [SerializeField]
        private AudioSource sfxSourcePrefab;
        private List<AudioSource> _audioSources;

        [SerializeField] private SfxData[] sfxDatas;

        private Dictionary<SFX, SfxData> _sfxDataDictionary;
        private Dictionary<SFX, int> _sfxAntiSpam;
        
        private bool _isReady;

        //Unity Functions
        //============================================================================================================//

        // Start is called before the first frame update
        private void Start()
        {
            Assert.IsNotNull(sfxAudioMixer);

            
            InitVfxLibrary();

            _sfxAntiSpam = new Dictionary<SFX, int>();
        }

        //============================================================================================================//

        private void InitVfxLibrary()
        {
            var count = sfxDatas.Length;
            _sfxDataDictionary = new Dictionary<SFX, SfxData>(count);
            for (var i = 0; i < count; i++)
            {
                var vfxData = sfxDatas[i];
                _sfxDataDictionary.Add(vfxData.type, vfxData);
            }
            //------------------------------------------------//

            _audioSources = new List<AudioSource>();
            _isReady = true;
        }

        //Play 2D Sound
        //============================================================================================================//
        public static void PlaySound(SFX sfx, float volume = 1f, float pitch = 1f)
        {
            Assert.IsNotNull(Instance, $"Missing the {nameof(SFXManager)} in the Scene!!");
            if (!Mathf.Approximately(pitch, 1.0f))
                Instance._PlaySoundWithPitch(sfx, volume, pitch);
            else
                Instance._PlaySound(sfx, volume);
        }
        private void _PlaySound(SFX sfx, float volume)
        {
            if(!_isReady)
                return;
            
            var sfxData = GetSFXData(sfx);

            var hasAntiSpam = _sfxAntiSpam.TryGetValue(sfx, out var count);
            if (sfxData.maxPlaying > 0 && hasAntiSpam && count > sfxData.maxPlaying)
                return;
            
            if(hasAntiSpam == false)
                _sfxAntiSpam.Add(sfx, 0);
            
            
            var audioClip = sfxData.GetRandomAudioClip();

            Assert.IsNotNull(sfxData);
            Assert.IsNotNull(audioClip);

            sfxAudioSource.PlayOneShot(audioClip, volume);
            //FIXME This should just use a loop, and not a coroutine
            StartCoroutine(DequeueSFXCoroutine(sfx, audioClip.length));
        }
        private void _PlaySoundWithPitch(SFX sfx, float volume, float pitch)
        {
            if(!_isReady)
                return;
            
            var sfxData = GetSFXData(sfx);

            var hasAntiSpam = _sfxAntiSpam.TryGetValue(sfx, out var count);
            if (sfxData.maxPlaying > 0 && hasAntiSpam && count > sfxData.maxPlaying)
                return;
            
            if(hasAntiSpam == false)
                _sfxAntiSpam.Add(sfx, 0);
            
            //Get an AudioSource but apply a 2D spatial setting of 0.0f
            var audioSource = TryGetAudioSourceInstance(0f);
            audioSource.transform.position = Vector3.zero;


            var audioClip = sfxData.GetRandomAudioClip();
            audioSource.clip = audioClip;
            audioSource.volume = sfxData.volume;
            audioSource.pitch = pitch;
            audioSource.Play();

            StartCoroutine(WaitForSoundFinishCoroutine(audioSource, audioClip.length));
            //FIXME This should just use a loop, and not a coroutine
            StartCoroutine(DequeueSFXCoroutine(sfx, audioClip.length));
        }
        
        //Play 3D Sound At Location
        //============================================================================================================//
        public static void PlaySoundAtLocation(SFX vfx, Vector3 worldPosition, float pitch = 1f)
        {
            Assert.IsNotNull(Instance, $"Missing the {nameof(SFXManager)} in the Scene!!");
            Instance._PlaySoundAtLocation(vfx, worldPosition, pitch);
        }
        //This is meant to be called via the VFXExtensions class
        private void _PlaySoundAtLocation(SFX vfx, Vector3 worldPosition, float pitch)
        {
            if(!_isReady)
                return;
                        
            var sfxData = GetSFXData(vfx);

            var audioSource = TryGetAudioSourceInstance();
            audioSource.transform.position = worldPosition;


            var audioClip = sfxData.GetRandomAudioClip();
            audioSource.clip = audioClip;
            audioSource.volume = sfxData.volume;
            audioSource.pitch = pitch;
            audioSource.Play();

            StartCoroutine(WaitForSoundFinishCoroutine(audioSource, audioClip.length));
        }

        //============================================================================================================//

        private SfxData GetSFXData(SFX sfx)
        {
            if (_sfxDataDictionary.TryGetValue(sfx, out var vfxData) == false)
                return null;

            Assert.IsNotNull(vfxData);

            return vfxData;
        }

        private AudioSource TryGetAudioSourceInstance(float spatialBlend = 1f)
        {
            AudioSource audioSource = null;
            if (_audioSources.Count > 0)
                audioSource = _audioSources.FirstOrDefault(x => x.gameObject.activeSelf == false);

            if (audioSource != null)
            {
                audioSource.gameObject.SetActive(true);
                //Force to 3D
                audioSource.spatialBlend = spatialBlend;
                
                return audioSource;
            }

            audioSource = Instantiate(sfxSourcePrefab, transform);
            audioSource.name = $"[{_audioSources.Count}]_SFX_AudioSource";
            _audioSources.Add(audioSource);
            //Force to 3D
            audioSource.spatialBlend = spatialBlend;
            
            return audioSource;
        }

        private static IEnumerator WaitForSoundFinishCoroutine(AudioSource targetAudioSource, float time)
        {
            yield return new WaitForSeconds(time);

            targetAudioSource.gameObject.SetActive(false);
            targetAudioSource.Stop();
            //Reset Pitch in case it changed
            targetAudioSource.pitch = 1f;
            targetAudioSource.spatialBlend = 1f;
        }
        
        private IEnumerator DequeueSFXCoroutine(SFX sfx, float time)
        {
            _sfxAntiSpam[sfx]++;
            
            yield return new WaitForSeconds(time);

            _sfxAntiSpam[sfx]--;
        }

        //Set Volume
        //============================================================================================================//
        public void SetVolume(float volume) => Instance.sfxAudioMixer.audioMixer.SetVolume(volume);

        //Unity Editor Functions
        //============================================================================================================//

#if UNITY_EDITOR

        private void OnValidate()
        {
            for (int i = 0; i < sfxDatas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sfxDatas[i].name))
                    sfxDatas[i].name = sfxDatas[i].type.ToString();
            }

            UnityEditor.EditorUtility.SetDirty(this);

            var enumTypes = (SFX[])Enum.GetValues(typeof(SFX));

            foreach (var enumType in enumTypes)
            {
                Assert.IsTrue(sfxDatas.Count(x => x.type == enumType) <= 1,
                    $"<b><color=\"red\">ERROR</color></b>\nMore than 1 SFX found in the SFX manager for {enumType}. <color=\"red\"><b>CAN ONLY HAVE 1</b></color>");
            }

        }

#endif


        //============================================================================================================//



    }
}
