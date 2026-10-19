using System;
using Audio;
using Audio.SoundFX;
using ldjam60.Interactables;
using ldjam60.Player;
using UnityEngine;
using Random = UnityEngine.Random;

public class Mech : MonoBehaviour, IAnimateMech
{
    private static readonly int SpeedAnimatorHash = Animator.StringToHash("Speed");
    public static event Action OnPlayerCaptured;

    [SerializeField]
    private Animator animator;

    [SerializeField, Min(0f)]
    private float moveSpeed;

    [SerializeField]
    private float playerDetectionRange;
    
    /*[SerializeField]
    public float TimeUntilDeath = 10.0f;
    public float total_timer;*/

    [Header("Camera Shake")]
    [SerializeField]
    private float stepShakeIntensity;
    [SerializeField]
    private float stepShakeDuration;
    [SerializeField]
    private float alertShakeIntensity, alertShakeDuration;
    

    [SerializeField]
    private Vector3 MechEndPosition;

    [Header("Terminal Loss Condition")]
    [SerializeField, Tooltip("Optional explicit terminal reference. The scene terminal is found automatically when unset.")]
    private Transform terminalTarget;
    [SerializeField, Min(0f)]
    private float terminalLoseDistance = 20f;

    private bool m_alertedToPlayer;
    private bool m_playerCaptured;

    private float m_currentMotion;
    private float m_currentMotionTarget;
    private float m_currentMotionVelocity;

    //Unity Functions
    //================================================================================================================//

    private void OnEnable()
    {
        Drone.OnPlayerSpotted += OnPlayerSpotted;
    }

    private void Start()
    {
        if (terminalTarget == null)
        {
            var terminal = FindAnyObjectByType<InteractableTerminal>();
            if (terminal != null)
                terminalTarget = terminal.transform;
            else
                Debug.LogError("Titan could not find a terminal for its loss condition.", this);
        }

        var dir = (transform.position - MechEndPosition).normalized;
        transform.rotation = Quaternion.LookRotation(Vector3.down, dir);
        //transform.up = dir;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Update()
    {
        m_currentMotion = Mathf.SmoothDamp(m_currentMotion, m_currentMotionTarget,  ref m_currentMotionVelocity, 0.05f);
        animator.SetFloat(SpeedAnimatorHash, m_currentMotion);
        
        if (!m_alertedToPlayer)
            return;
        
        var groundedEndPosition = MechEndPosition;
        groundedEndPosition.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, groundedEndPosition, moveSpeed * Time.deltaTime);

        CheckForTerminalCapture();

        /*mech.total_timer += Time.deltaTime;
        float t = mech.total_timer / mech.TimeUntilDeath;
        mech.transform.position = UnityEngine.Vector3.Lerp(mech.MechStartPosition, mech.MechEndPosition, t);
        if (mech.total_timer >= mech.TimeUntilDeath)
        {
            state = DroneState.Arrest;
        }*/

    }

    private void OnDisable()
    {
        Drone.OnPlayerSpotted -= OnPlayerSpotted;
    }

    //Animator Event Listener
    //================================================================================================================//

    public void Step()
    {
        //TODO Camera Shake
        SFX.MECH_STEP.PlaySound(pitch: Random.Range(0.8f, 1.2f));
        CameraShake.Shake(stepShakeIntensity, stepShakeDuration);
    }

    //Callbacks
    //================================================================================================================//
    
    private void OnPlayerSpotted(bool visible)
    {
        if (m_alertedToPlayer == visible)
            return;
        
        //TODO Play alert sound
        SFX.MECH_ALERT.PlaySound(pitch: Random.Range(0.9f, 1.1f));
        CameraShake.Shake(alertShakeIntensity, alertShakeDuration);
        
        m_alertedToPlayer = visible;
        
        m_currentMotionTarget = visible ? 1f : 0f;
    }

    private void CheckForTerminalCapture()
    {
        if (m_playerCaptured || terminalTarget == null)
            return;

        var offset = transform.position - terminalTarget.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > terminalLoseDistance * terminalLoseDistance)
            return;

        m_playerCaptured = true;
        m_alertedToPlayer = false;
        m_currentMotionTarget = 0f;
        OnPlayerCaptured?.Invoke();
    }

    //Unity Editor Functions
    //================================================================================================================//

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
    }


}

public interface IAnimateMech
{
    void Step();
}
