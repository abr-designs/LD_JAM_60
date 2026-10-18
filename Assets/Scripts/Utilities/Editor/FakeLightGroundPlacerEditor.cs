#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FakeLightGroundPlacer))]
public class FakeLightGroundPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);

        FakeLightGroundPlacer placer =
            (FakeLightGroundPlacer)target;

        if (GUILayout.Button("Place Quad On Ground"))
        {
            Undo.RecordObject(placer, "Place Fake Light On Ground");

            if (placer.Quad != null)
            {
                Undo.RecordObject(
                    placer.Quad,
                    "Align Fake Light Quad"
                );
            }

            if (placer.PlaceOnGround())
            {
                if (placer.Quad != null)
                    EditorUtility.SetDirty(placer.Quad);

                EditorUtility.SetDirty(placer);
            }
        }
    }
}
#endif