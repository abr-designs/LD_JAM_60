using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Drone : MonoBehaviour
{
    private enum DroneState { Patrol, Suspicious, Chase, Investigate, Arrest }

    public static event Action<bool> OnPlayerSpotted;
    private static readonly HashSet<Drone> s_reportingDrones = new();

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Vision")]
    [SerializeField] private GameObject player;
    [SerializeField, Min(0f)] private float seek_distance = 10f;
    [SerializeField, Min(0f), Tooltip("Players this close are detected twice as quickly while visible.")]
    private float alert_distance = 4f;
    [SerializeField, Range(1f, 360f)] private float visionAngle = 100f;
    [SerializeField] private Vector3 eyeOffset = new(0f, 1f, 0f);
    [SerializeField, Min(0f)] private float playerTargetHeight = 1f;
    [SerializeField] private LayerMask visibilityMask = ~0;
    [SerializeField, Min(0.01f)] private float detectionBuildTime = 1.25f;
    [SerializeField, Min(1f), Tooltip("Detection speed multiplier while the player is inside Alert Distance.")]
    private float closeDetectionMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float loseSightGraceTime = 1f;
    [SerializeField, Min(0f)] private float investigateDuration = 5f;
    [SerializeField, Min(0.02f), Tooltip("How often a chasing drone samples the player's current position.")]
    private float destinationRefreshTime = 0.65f;
    [SerializeField, Min(0f), Tooltip("NavMesh turning speed used during pursuit. Patrol uses the agent's original angular speed.")]
    private float chaseAngularSpeed = 75f;

    [Header("Feedback")]
    [SerializeField] private Material material;
    [SerializeField, Min(0.02f)] private float flashTime = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool drawVisionGizmos = true;

    private readonly RaycastHit[] m_sightHits = new RaycastHit[16];
    private readonly Color m_patrolColor = new(191f / 255f, 109f / 255f, 22f / 255f);
    private NavMeshAgent m_agent;
    private DroneState m_state;
    private int m_patrolIndex;
    private float m_detectionTimer;
    private float m_lostSightTimer;
    private float m_investigateTimer;
    private float m_destinationTimer;
    private float m_flashTimer;
    private float m_arrestTimer;
    private float m_patrolAngularSpeed;
    private bool m_flashOn;
    private bool m_isReporting;
    private bool m_canSeePlayer;
    private bool m_hasLastKnownPosition;
    private Vector3 m_lastKnownPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_reportingDrones.Clear();

    private void OnEnable() => Mech.OnPlayerCaptured += OnPlayerCaptured;

    private void Start()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_patrolAngularSpeed = m_agent.angularSpeed;
        m_state = DroneState.Patrol;
        SetPatrolDestination();
    }

    private void Update()
    {
        if (player == null)
            return;

        var distance = Vector3.Distance(transform.position, player.transform.position);
        m_canSeePlayer = CanSeePlayer(distance, m_state == DroneState.Chase);

        switch (m_state)
        {
            case DroneState.Patrol: UpdatePatrol(); break;
            case DroneState.Suspicious: UpdateSuspicious(distance); break;
            case DroneState.Chase: UpdateChase(); break;
            case DroneState.Investigate: UpdateInvestigate(); break;
            case DroneState.Arrest: UpdateArrest(); break;
        }
    }

    private void OnDisable()
    {
        SetReporting(false);
        Mech.OnPlayerCaptured -= OnPlayerCaptured;
    }

    private void UpdatePatrol()
    {
        SetEmission(m_patrolColor);
        if (m_canSeePlayer)
        {
            m_detectionTimer = 0f;
            m_agent.isStopped = true;
            m_state = DroneState.Suspicious;
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;
        if (!m_agent.pathPending && m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            m_patrolIndex = (m_patrolIndex + 1) % patrolPoints.Length;
            SetPatrolDestination();
        }
    }

    private void UpdateSuspicious(float distance)
    {
        m_agent.isStopped = true;
        if (!m_canSeePlayer)
        {
            m_detectionTimer = Mathf.Max(0f, m_detectionTimer - Time.deltaTime * 2f);
            if (m_detectionTimer <= 0f)
                ReturnToPatrol();
            return;
        }

        m_lastKnownPosition = player.transform.position;
        m_hasLastKnownPosition = true;
        var multiplier = distance <= alert_distance ? closeDetectionMultiplier : 1f;
        m_detectionTimer += Time.deltaTime * multiplier;
        SetEmission(Color.Lerp(m_patrolColor, Color.red * 4f,
            Mathf.Clamp01(m_detectionTimer / detectionBuildTime)));
        if (m_detectionTimer < detectionBuildTime)
            return;

        m_lostSightTimer = 0f;
        m_destinationTimer = 0f;
        m_agent.isStopped = false;
        m_agent.angularSpeed = chaseAngularSpeed;
        m_state = DroneState.Chase;
        SetReporting(true);
    }

    private void UpdateChase()
    {
        UpdateAlertFlash();
        if (m_canSeePlayer)
        {
            m_lostSightTimer = 0f;
            m_lastKnownPosition = player.transform.position;
            m_hasLastKnownPosition = true;
            m_destinationTimer -= Time.deltaTime;
            if (m_destinationTimer <= 0f)
            {
                m_agent.SetDestination(m_lastKnownPosition);
                m_destinationTimer = destinationRefreshTime;
            }
            return;
        }

        m_lostSightTimer += Time.deltaTime;
        if (m_lostSightTimer < loseSightGraceTime)
            return;

        SetReporting(false);
        m_investigateTimer = 0f;
        m_agent.isStopped = false;
        m_agent.angularSpeed = m_patrolAngularSpeed;
        if (m_hasLastKnownPosition)
            m_agent.SetDestination(m_lastKnownPosition);
        m_state = DroneState.Investigate;
    }

    private void UpdateInvestigate()
    {
        SetEmission(m_patrolColor);
        if (m_canSeePlayer)
        {
            m_lastKnownPosition = player.transform.position;
            m_lostSightTimer = 0f;
            m_destinationTimer = 0f;
            m_agent.isStopped = false;
            m_agent.angularSpeed = chaseAngularSpeed;
            m_state = DroneState.Chase;
            SetReporting(true);
            return;
        }

        if (m_agent.pathPending || m_agent.remainingDistance > m_agent.stoppingDistance + 0.1f)
            return;
        m_agent.isStopped = true;
        m_investigateTimer += Time.deltaTime;
        if (m_investigateTimer >= investigateDuration)
            ReturnToPatrol();
    }

    private void UpdateArrest()
    {
        m_arrestTimer += Time.deltaTime;
        if (m_arrestTimer > 2f)
            Debug.Log("Player arrested!");
    }

    private bool CanSeePlayer(float distance, bool ignoreVisionCone)
    {
        if (distance > seek_distance)
            return false;

        var eyePosition = transform.TransformPoint(eyeOffset);
        var targetPosition = player.transform.position + Vector3.up * playerTargetHeight;
        var toPlayer = targetPosition - eyePosition;
        var sightDistance = toPlayer.magnitude;
        if (sightDistance <= Mathf.Epsilon)
            return true;

        if (!ignoreVisionCone)
        {
            var flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var flatDirection = Vector3.ProjectOnPlane(toPlayer, Vector3.up).normalized;
            if (flatForward.sqrMagnitude > 0f && flatDirection.sqrMagnitude > 0f
                && Vector3.Angle(flatForward, flatDirection) > visionAngle * 0.5f)
                return false;
        }

        var hitCount = Physics.RaycastNonAlloc(eyePosition, toPlayer / sightDistance, m_sightHits,
            sightDistance, visibilityMask, QueryTriggerInteraction.Ignore);
        var nearestDistance = float.PositiveInfinity;
        Transform nearestTransform = null;
        for (var i = 0; i < hitCount; i++)
        {
            var hitTransform = m_sightHits[i].transform;
            if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;
            if (m_sightHits[i].distance >= nearestDistance)
                continue;
            nearestDistance = m_sightHits[i].distance;
            nearestTransform = hitTransform;
        }

        return nearestTransform == null
               || nearestTransform == player.transform
               || nearestTransform.IsChildOf(player.transform);
    }

    private void ReturnToPatrol()
    {
        SetReporting(false);
        m_detectionTimer = 0f;
        m_lostSightTimer = 0f;
        m_investigateTimer = 0f;
        m_hasLastKnownPosition = false;
        m_agent.isStopped = false;
        m_agent.angularSpeed = m_patrolAngularSpeed;
        m_state = DroneState.Patrol;
        SetPatrolDestination();
    }

    private void SetPatrolDestination()
    {
        if (m_agent == null || patrolPoints == null || patrolPoints.Length == 0)
            return;
        m_patrolIndex = Mathf.Clamp(m_patrolIndex, 0, patrolPoints.Length - 1);
        if (patrolPoints[m_patrolIndex] != null)
            m_agent.SetDestination(patrolPoints[m_patrolIndex].position);
    }

    private void SetReporting(bool reporting)
    {
        if (m_isReporting == reporting)
            return;
        m_isReporting = reporting;
        var hadReports = s_reportingDrones.Count > 0;
        if (reporting) s_reportingDrones.Add(this);
        else s_reportingDrones.Remove(this);
        var hasReports = s_reportingDrones.Count > 0;
        if (hadReports != hasReports)
            OnPlayerSpotted?.Invoke(hasReports);
    }

    private void UpdateAlertFlash()
    {
        m_flashTimer += Time.deltaTime;
        if (m_flashTimer < flashTime)
            return;
        m_flashTimer = 0f;
        m_flashOn = !m_flashOn;
        SetEmission(m_flashOn ? Color.red * Mathf.Pow(2f, 6f) : Color.black);
    }

    private void SetEmission(Color color)
    {
        if (material != null)
            material.SetColor("_EmissionColor", color);
    }

    private void OnPlayerCaptured()
    {
        SetReporting(false);
        if (m_agent != null)
            m_agent.isStopped = true;
        m_state = DroneState.Arrest;
    }

    private void OnDrawGizmos()
    {
        if (!drawVisionGizmos)
            return;

        DrawStateDebug();

        var eyePosition = transform.TransformPoint(eyeOffset);
        var flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude <= 0f)
            flatForward = Vector3.forward;

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
        DrawCircle(transform.position, seek_distance, 48);
        DrawVisionCone(eyePosition, flatForward, seek_distance, visionAngle, 24);
        Gizmos.color = new Color(1f, 0.25f, 0f, 0.8f);
        DrawCircle(transform.position, alert_distance, 32);

        if (Application.isPlaying && player != null)
        {
            var target = player.transform.position + Vector3.up * playerTargetHeight;
            Gizmos.color = m_canSeePlayer ? Color.green : Color.red;
            Gizmos.DrawLine(eyePosition, target);
        }
        if (Application.isPlaying && m_hasLastKnownPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(m_lastKnownPosition, 0.5f);
            Gizmos.DrawLine(transform.position, m_lastKnownPosition);
        }
    }

    private void DrawStateDebug()
    {
        var markerPosition = transform.position + Vector3.up * 2.5f;
        var stateColor = m_state switch
        {
            DroneState.Patrol => Color.yellow,
            DroneState.Suspicious => new Color(1f, 0.45f, 0f),
            DroneState.Chase => Color.red,
            DroneState.Investigate => Color.cyan,
            DroneState.Arrest => Color.magenta,
            _ => Color.white
        };

        Gizmos.color = stateColor;
        Gizmos.DrawSphere(markerPosition, 0.2f);

#if UNITY_EDITOR
        var stateLabel = Application.isPlaying ? m_state.ToString().ToUpperInvariant() : "PATROL (EDIT MODE)";
        if (Application.isPlaying && m_state == DroneState.Suspicious)
            stateLabel += $"  {Mathf.RoundToInt(Mathf.Clamp01(m_detectionTimer / detectionBuildTime) * 100f)}%";
        else if (Application.isPlaying && m_state == DroneState.Investigate)
            stateLabel += $"  {m_investigateTimer:0.0}/{investigateDuration:0.0}s";

        var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
        {
            normal = { textColor = stateColor },
            fontSize = 13
        };
        UnityEditor.Handles.Label(markerPosition + Vector3.up * 0.3f, stateLabel, style);
#endif
    }

    private static void DrawCircle(Vector3 center, float radius, int segments)
    {
        var previous = center + Vector3.forward * radius;
        for (var i = 1; i <= segments; i++)
        {
            var angle = i * Mathf.PI * 2f / segments;
            var next = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private static void DrawVisionCone(Vector3 origin, Vector3 forward, float distance, float angle, int segments)
    {
        var halfAngle = angle * 0.5f;
        var previousDirection = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
        Gizmos.DrawLine(origin, origin + previousDirection * distance);
        for (var i = 1; i <= segments; i++)
        {
            var currentAngle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
            var direction = Quaternion.AngleAxis(currentAngle, Vector3.up) * forward;
            Gizmos.DrawLine(origin + previousDirection * distance, origin + direction * distance);
            previousDirection = direction;
        }
        Gizmos.DrawLine(origin, origin + previousDirection * distance);
    }
}
