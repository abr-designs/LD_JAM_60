
using UnityEngine;
using UnityEditor;

public class TerrainSplatmapImporter : EditorWindow
{
    private Terrain terrain;
    private Texture2D splatmap;
    private bool flipX;
    private bool flipY;

    [MenuItem("Tools/Terrain/Import Splatmap")]
    private static void Open()
    {
        GetWindow<TerrainSplatmapImporter>(
            "Splatmap Importer"
        );
    }

    private void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField(
            "Terrain", terrain, typeof(Terrain), true
        );

        splatmap = (Texture2D)EditorGUILayout.ObjectField(
            "Splatmap PNG", splatmap, typeof(Texture2D), false
        );

        flipX = EditorGUILayout.Toggle("Flip X", flipX);
        flipY = EditorGUILayout.Toggle("Flip Y", flipY);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(
            terrain == null || splatmap == null))
        {
            if (GUILayout.Button("Apply Splatmap"))
                ApplySplatmap();
        }
    }

    private void ApplySplatmap()
    {
        TerrainData data = terrain.terrainData;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        int layers = data.alphamapLayers;

        if (layers < 1 || layers > 4)
        {
            Debug.LogError(
                "This importer requires 1–4 Terrain Layers."
            );
            return;
        }

        if (!splatmap.isReadable)
        {
            Debug.LogError(
                "Enable Read/Write on the splatmap texture."
            );
            return;
        }

        float[,,] weights = new float[height, width, layers];

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            if (flipY) v = 1f - v;

            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                if (flipX) u = 1f - u;

                Color pixel = splatmap.GetPixelBilinear(u, v);

                float total = 0f;

                for (int i = 0; i < layers; i++)
                {
                    float value = Mathf.Max(0f, pixel[i]);
                    weights[y, x, i] = value;
                    total += value;
                }

                if (total > 0.00001f)
                {
                    for (int i = 0; i < layers; i++)
                        weights[y, x, i] /= total;
                }
                else
                {
                    weights[y, x, 0] = 1f;
                }
            }
        }

        Undo.RegisterCompleteObjectUndo(
            data, "Import Terrain Splatmap"
        );

        data.SetAlphamaps(0, 0, weights);

        EditorUtility.SetDirty(data);

        Debug.Log(
            $"Applied splatmap to '{terrain.name}' " +
            $"({width}x{height}, {layers} layers)."
        );
    }
}
