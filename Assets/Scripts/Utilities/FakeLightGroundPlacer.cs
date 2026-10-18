
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class FakeLightGroundPlacer : MonoBehaviour
{
    [Header("Quad")]
    [SerializeField] private Transform quad;
    [SerializeField] private Vector2 size = Vector2.one;

    [Header("Ground Sampling")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [Min(0.01f)]
    [SerializeField] private float rayStartHeight = 5f;
    [Min(0.01f)]
    [SerializeField] private float rayDistance = 20f;
    [Min(0f)]
    [SerializeField] private float surfaceOffset = 0.01f;

    [Header("Slope Alignment")]
    [Range(0f, 1f)]
    [SerializeField] private float normalInfluence = 0.25f;

    [Tooltip("Maximum allowed angle between the fitted plane and world up.")]
    [Range(0f, 89f)]
    [SerializeField] private float maxSlopeAngle = 80f;

    [Tooltip("Direction used to determine rotation around the surface normal.")]
    [SerializeField] private Vector3 preferredForward = Vector3.forward;

    [Header("Missing Corners")]
    [SerializeField] private bool allowPartialHits = false;

    public Transform Quad => quad;

    public bool PlaceOnGround()
    {
        if (quad == null)
        {
            Debug.LogWarning("Assign a quad before placing it.", this);
            return false;
        }

        // A Unity Quad lies in local XY; its surface normal is local +Z.
        Vector3 right = quad.right;
        Vector3 planeUp = quad.up;
        Vector3 center = quad.position;

        Vector3[] corners =
        {
            center - right * size.x * 0.5f
                   - planeUp * size.y * 0.5f,

            center - right * size.x * 0.5f
                   + planeUp * size.y * 0.5f,

            center + right * size.x * 0.5f
                   - planeUp * size.y * 0.5f,

            center + right * size.x * 0.5f
                   + planeUp * size.y * 0.5f
        };

        Vector3[] points = new Vector3[4];
        Vector3 normalSum = Vector3.zero;
        bool[] hitCorners = new bool[4];
        int hitCount = 0;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 origin =
                corners[i] + Vector3.up * rayStartHeight;

            if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                rayStartHeight + rayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            {
                points[i] = hit.point;
                normalSum += hit.normal;
                hitCorners[i] = true;
                hitCount++;
            }
        }

        if (hitCount < 3 || (!allowPartialHits && hitCount != 4))
        {
            Debug.LogWarning(
                $"Ground placement requires 4 corner hits " +
                $"(received {hitCount}).",
                this);

            return false;
        }

        // Average sampled positions.
        Vector3 averagePosition = Vector3.zero;

        for (int i = 0; i < points.Length; i++)
        {
            if (hitCorners[i])
                averagePosition += points[i];
        }

        averagePosition /= hitCount;

        Vector3 averageNormal = normalSum.normalized;

        // Estimate the plane normal using height differences across
        // both quad axes. This captures slope even when terrain
        // colliders report slightly different surface normals.
        Vector3 fittedNormal = Vector3.zero;

        if (hitCount == 4)
        {
            // Corner order:
            // 0 = -right, -up
            // 1 = -right, +up
            // 2 = +right, -up
            // 3 = +right, +up

            Vector3 across =
                (points[2] - points[0] +
                 points[3] - points[1]) * 0.5f;

            Vector3 along =
                (points[1] - points[0] +
                 points[3] - points[2]) * 0.5f;

            fittedNormal = Vector3.Cross(across, along).normalized;
        }
        else
        {
            // With three hits, estimate a plane from the available
            // points rather than relying solely on surface normals.
            Vector3[] validPoints = new Vector3[3];
            int index = 0;

            for (int i = 0; i < points.Length; i++)
            {
                if (hitCorners[i])
                    validPoints[index++] = points[i];
            }

            fittedNormal = Vector3.Cross(
                validPoints[1] - validPoints[0],
                validPoints[2] - validPoints[0]
            ).normalized;
        }

        if (fittedNormal.sqrMagnitude < 0.0001f)
            fittedNormal = averageNormal;

        // Keep the normal pointing away from the ground.
        if (Vector3.Dot(fittedNormal, averageNormal) < 0f)
            fittedNormal = -fittedNormal;

        // Blend the geometric plane with the collider normals.
        Vector3 targetNormal = Vector3.Slerp(
            fittedNormal,
            averageNormal,
            normalInfluence
        ).normalized;

        if (targetNormal.sqrMagnitude < 0.0001f)
            return false;

        // Limit extreme slopes if desired.
        float slopeAngle = Vector3.Angle(
            Vector3.up,
            targetNormal
        );

        if (slopeAngle > maxSlopeAngle)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(
                targetNormal,
                Vector3.up
            ).normalized;

            targetNormal = Quaternion.AngleAxis(
                maxSlopeAngle,
                Vector3.Cross(Vector3.up, slopeDirection).normalized
            ) * Vector3.up;
        }

        // Choose a stable direction along the surface.
        Vector3 tangentForward = Vector3.ProjectOnPlane(
            preferredForward,
            targetNormal
        );

        if (tangentForward.sqrMagnitude < 0.0001f)
        {
            tangentForward = Vector3.ProjectOnPlane(
                Vector3.right,
                targetNormal
            );
        }

        Quaternion rotation = Quaternion.LookRotation(
            targetNormal,
            tangentForward.normalized
        );

        quad.SetPositionAndRotation(
            averagePosition + targetNormal * surfaceOffset,
            rotation
        );

        Vector3 scale = quad.localScale;
        scale.x = size.x;
        scale.y = size.y;
        quad.localScale = scale;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (quad == null)
            return;

        Vector3 right = quad.right * size.x * 0.5f;
        Vector3 up = quad.up * size.y * 0.5f;
        Vector3 center = quad.position;

        Vector3[] corners =
        {
            center - right - up,
            center - right + up,
            center + right - up,
            center + right + up
        };

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[3]);
        Gizmos.DrawLine(corners[3], corners[2]);
        Gizmos.DrawLine(corners[2], corners[0]);

        Gizmos.color = Color.cyan;

        foreach (Vector3 corner in corners)
        {
            Vector3 origin =
                corner + Vector3.up * rayStartHeight;

            Gizmos.DrawLine(
                origin,
                origin - Vector3.up *
                (rayStartHeight + rayDistance)
            );
        }
    }
}
