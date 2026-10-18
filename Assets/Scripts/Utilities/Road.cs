
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Road : MonoBehaviour
{
    [Header("Control Points")]
    public List<Vector3> points = new()
    {
        new(-5, 0, 0),
        new(0, 0, 3),
        new(5, 0, 0)
    };

    [Header("Shape")]
    [Min(0.01f)] public float width = 2f;
    [Min(1)] public int subdivisions = 8;
    public bool smooth = true;
    public bool generateMesh = true;

    [Header("Terrain Snapping")]
    public LayerMask terrainLayers = ~0;
    [Min(0.01f)] public float rayHeight = 1000f;
    public float heightOffset = 0.05f;

    private Mesh generatedMesh;

    private void OnEnable() => Refresh();
    private void OnValidate() => Refresh();

    private void OnDisable()
    {
        if (TryGetComponent(out MeshFilter filter) &&
            filter.sharedMesh == generatedMesh)
        {
            filter.sharedMesh = null;
        }

        if (generatedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);

            generatedMesh = null;
        }
    }

    // Explicit terrain snapping only.
    // Does not modify any control points automatically.
    public Vector3 ProjectToTerrain(Vector3 world)
    {
        Vector3 origin = new(
            world.x,
            world.y + rayHeight,
            world.z
        );

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            rayHeight * 2f,
            terrainLayers,
            QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * heightOffset;
        }

        return world;
    }

    public void SnapPoint(int index)
    {
        if (index < 0 || index >= points.Count)
            return;

        Vector3 world = transform.TransformPoint(points[index]);

        points[index] = transform.InverseTransformPoint(
            ProjectToTerrain(world)
        );

        Refresh();
    }

    public void SnapAllPoints()
    {
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 world = transform.TransformPoint(points[i]);

            points[i] = transform.InverseTransformPoint(
                ProjectToTerrain(world)
            );
        }

        Refresh();
    }

    // Evaluate a segment in local space.
    public Vector3 Evaluate(int segment, float t)
    {
        Vector3 b = points[segment];
        Vector3 c = points[segment + 1];

        if (!smooth)
            return Vector3.Lerp(b, c, t);

        Vector3 a = points[Mathf.Max(0, segment - 1)];
        Vector3 d = points[Mathf.Min(
            points.Count - 1, segment + 2
        )];

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * b +
            (-a + c) * t +
            (2f * a - 5f * b + 4f * c - d) * t2 +
            (-a + 3f * b - 3f * c + d) * t3
        );
    }

    // Get world-space samples along the road.
    public List<Vector3> GetSamples()
    {
        List<Vector3> samples = new();

        if (points.Count == 0)
            return samples;

        samples.Add(transform.TransformPoint(points[0]));

        int steps = smooth
            ? Mathf.Max(1, subdivisions)
            : 1;

        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = 1; j <= steps; j++)
            {
                float t = (float)j / steps;

                samples.Add(transform.TransformPoint(
                    Evaluate(i, t)
                ));
            }
        }

        // No automatic terrain projection.
        return samples;
    }

    public void Refresh()
    {
        if (!TryGetComponent(out LineRenderer line) ||
            !TryGetComponent(out MeshFilter filter) ||
            !TryGetComponent(out MeshRenderer renderer))
            return;

        List<Vector3> samples = GetSamples();

        line.useWorldSpace = false;
        line.loop = false;
        line.widthMultiplier = width;
        line.positionCount = samples.Count;

        for (int i = 0; i < samples.Count; i++)
        {
            line.SetPosition(
                i,
                transform.InverseTransformPoint(samples[i])
            );
        }

        line.enabled = !generateMesh;
        renderer.enabled = generateMesh;

        if (!generateMesh || samples.Count < 2)
        {
            filter.sharedMesh = null;
            return;
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = "Generated Road Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        generatedMesh.Clear();

        generatedMesh.indexFormat =
            samples.Count * 2 > 65535
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;

        Vector3[] vertices = new Vector3[samples.Count * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(samples.Count - 1) * 6];

        float distance = 0f;

        for (int i = 0; i < samples.Count; i++)
        {
            Vector3 tangent;

            if (i == 0)
                tangent = samples[1] - samples[0];
            else if (i == samples.Count - 1)
                tangent = samples[i] - samples[i - 1];
            else
                tangent = samples[i + 1] - samples[i - 1];

            // Horizontal road width.
            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.forward;

            tangent.Normalize();

            Vector3 right = Vector3.Cross(
                Vector3.up,
                tangent
            ).normalized;

            Vector3 leftEdge =
                samples[i] - right * width * 0.5f;

            Vector3 rightEdge =
                samples[i] + right * width * 0.5f;

            // Vertices use the actual curve positions,
            // including elevations above/below terrain.
            vertices[i * 2] =
                transform.InverseTransformPoint(leftEdge);

            vertices[i * 2 + 1] =
                transform.InverseTransformPoint(rightEdge);

            if (i > 0)
            {
                distance += Vector3.Distance(
                    samples[i - 1],
                    samples[i]
                );
            }

            uvs[i * 2] = new Vector2(0, distance);
            uvs[i * 2 + 1] = new Vector2(1, distance);

            if (i < samples.Count - 1)
            {
                int v = i * 2;
                int t = i * 6;

                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;

                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }
        }

        generatedMesh.Clear();

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        // Force MeshFilter to recognize the updated geometry.
        filter.sharedMesh = null;
        filter.sharedMesh = generatedMesh;
    }
}
