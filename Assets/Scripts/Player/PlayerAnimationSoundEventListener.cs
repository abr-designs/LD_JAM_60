using System;
using Audio;
using Audio.SoundFX;
using GameInput;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerAnimationSoundEventListener : MonoBehaviour
{
    [SerializeField]
    private AudioSource chunkWalkAudioSource;
    [SerializeField, Min(0)]
    private float maxVolume;
    private float m_volumeVelocity;

    private bool m_isMoving;
    

    //Unity Functions
    //================================================================================================================//

    private void OnEnable()
    {
        GameInputDelegator.OnMovementChanged += MovementChanged;
    }

    private void Start()
    {
        chunkWalkAudioSource.loop = true;
        chunkWalkAudioSource.volume = 0f;
        chunkWalkAudioSource.Play();
    }

    private void LateUpdate()
    {
        chunkWalkAudioSource.volume = Mathf.SmoothDamp(
            chunkWalkAudioSource.volume,
            m_isMoving ? maxVolume : 0f,
            ref m_volumeVelocity,
            0.3f);
    }

    private void OnDisable()
    {
        GameInputDelegator.OnMovementChanged -= MovementChanged;
    }

    //Animation Events
    //================================================================================================================//

    private void StepSound()
    {
        if (!m_isMoving)
            return;
        
        SFX.STEP.PlaySound(pitch:Random.Range(0.8f, 1.2f));
    }

    //Callbacks
    //================================================================================================================//
    
    private void MovementChanged(Vector2 input)
    {
        m_isMoving = input != Vector2.zero;
    }
}
