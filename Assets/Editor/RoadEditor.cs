
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Road))]
public class RoadEditor : Editor
{
    private Road road;
    private int selectedPoint = -1;
    private bool previousToolsHidden;

    private void OnEnable()
    {
        road = (Road)target;

        previousToolsHidden = Tools.hidden;
        Tools.hidden = true;

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Tools.hidden = previousToolsHidden;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        if (road != null)
            RefreshRoad();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(road, "Add Road Point");

            Vector3 position = road.points.Count > 0
                ? road.points[^1] + Vector3.right * 5f
                : Vector3.zero;

            road.points.Add(position);
            selectedPoint = road.points.Count - 1;

            RefreshRoad();
        }

        bool validSelection =
            selectedPoint >= 0 &&
            selectedPoint < road.points.Count;

        using (new EditorGUI.DisabledScope(!validSelection))
        {
            if (GUILayout.Button("Delete Selected Point"))
            {
                DeleteSelectedPoint();
            }

            if (GUILayout.Button("Snap Selected Point To Terrain"))
            {
                Undo.RecordObject(road, "Snap Road Point");

                road.SnapPoint(selectedPoint);
                RefreshRoad();
            }
        }

        if (GUILayout.Button("Snap All Points To Terrain"))
        {
            Undo.RecordObject(road, "Snap Road To Terrain");

            road.SnapAllPoints();
            RefreshRoad();
        }

        EditorGUILayout.HelpBox(
            "Click a cyan point to select it.\n" +
            "Drag the selected point freely in 3D.\n" +
            "Click a green + to insert a point.\n" +
            "Use Snap buttons to project onto terrain.\n" +
            "Press Delete to remove the selected point.",
            MessageType.Info
        );
    }

    private void OnSceneGUI()
    {
        if (road == null)
            return;

        Transform tr = road.transform;

        var samples = road.GetSamples();

        // Draw the road centerline.
        Handles.color = Color.yellow;

        if (samples.Count >= 2)
            Handles.DrawAAPolyLine(3f, samples.ToArray());

        // Insert point buttons at segment midpoints.
        for (int i = 0; i < road.points.Count - 1; i++)
        {
            Vector3 midpoint = tr.TransformPoint(
                road.Evaluate(i, 0.5f)
            );

            float size =
                HandleUtility.GetHandleSize(midpoint) * 0.09f;

            Handles.color = Color.green;

            if (Handles.Button(
                midpoint,
                Quaternion.identity,
                size,
                size * 1.5f,
                Handles.CircleHandleCap))
            {
                Undo.RecordObject(road, "Insert Road Point");

                road.points.Insert(
                    i + 1,
                    tr.InverseTransformPoint(midpoint)
                );

                selectedPoint = i + 1;

                RefreshRoad();
                return;
            }

            Handles.Label(
                midpoint + Vector3.up * size,
                "+"
            );
        }

        // Draw selectable control points.
        for (int i = 0; i < road.points.Count; i++)
        {
            Vector3 world = tr.TransformPoint(road.points[i]);

            float size =
                HandleUtility.GetHandleSize(world) * 0.12f;

            Handles.color = i == selectedPoint
                ? Color.yellow
                : Color.cyan;

            if (Handles.Button(
                world,
                Quaternion.identity,
                size,
                size * 1.5f,
                Handles.SphereHandleCap))
            {
                selectedPoint = i;
                Repaint();
                SceneView.RepaintAll();
            }

            Handles.Label(
                world + Vector3.up * size,
                $"Point {i}"
            );
        }

        // Only selected point receives movement handles.
        if (selectedPoint >= 0 &&
            selectedPoint < road.points.Count)
        {
            Vector3 world = tr.TransformPoint(
                road.points[selectedPoint]
            );

            EditorGUI.BeginChangeCheck();

            Vector3 moved = Handles.PositionHandle(
                world,
                Quaternion.identity
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Road Point");

                // No snapping during movement.
                road.points[selectedPoint] =
                    tr.InverseTransformPoint(moved);

                RefreshRoad();
            }
        }

        Event e = Event.current;

        if (e.type == EventType.KeyDown &&
            (e.keyCode == KeyCode.Delete ||
             e.keyCode == KeyCode.Backspace) &&
            selectedPoint >= 0)
        {
            DeleteSelectedPoint();
            e.Use();
        }
    }

    private void DeleteSelectedPoint()
    {
        if (selectedPoint < 0 ||
            selectedPoint >= road.points.Count)
            return;

        Undo.RecordObject(road, "Delete Road Point");

        road.points.RemoveAt(selectedPoint);

        selectedPoint = Mathf.Min(
            selectedPoint,
            road.points.Count - 1
        );

        RefreshRoad();
    }

    private void RefreshRoad()
    {
        road.Refresh();
        EditorUtility.SetDirty(road);

        SceneView.RepaintAll();
        Repaint();
        EditorUtility.SetDirty(road.GetComponent<MeshFilter>());
    }
}
