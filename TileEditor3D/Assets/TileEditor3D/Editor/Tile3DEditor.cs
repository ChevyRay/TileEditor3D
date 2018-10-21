using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Tile3D))]
public class Tile3DEditor : Editor
{
    [MenuItem("Assets/Create/Tile3D", priority = 101)]
    static void CreateTile3D()
    {
        int maxID = 0;
        var guids = AssetDatabase.FindAssets("t:Tile3D");
        foreach (var guid in guids)
        {
            var t = AssetDatabase.LoadAssetAtPath<Tile3D>(AssetDatabase.GUIDToAssetPath(guid));
            if (t != null)
                maxID = Mathf.Max(maxID, t.id);
        }

        var tile = CreateInstance<Tile3D>();
        tile.id = maxID + 1;

        string path;
        string name = "Tile3D";
        if (Selection.activeObject == null)
            path = "Assets";
        else
        {
            path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            if (!string.IsNullOrEmpty(path) && Selection.activeObject is Texture2D)
            {
                name = Selection.activeObject.name;
                tile.texture = Selection.activeObject as Texture2D;
            }
        }
        if (string.IsNullOrEmpty(path))
            path = "Assets";
        else if (System.IO.File.Exists(path))
            path = System.IO.Path.GetDirectoryName(path);
        path = System.IO.Path.Combine(path, name + ".asset");
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = tile;
    }

    Color32 GetPixel(Color32[] pixels, int w, int h, int x, int y)
    {
        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);
        return pixels[y * w + x];
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var targ = (Tile3D)target;

        EditorGUILayout.LabelField("ID", targ.id.ToString());

        var r = targ.rect;
        EditorGUILayout.LabelField("Rect", r.x + ", " + r.y + ", " + r.width + ", " + r.height);

        if (GUILayout.Button("Pack Tiles"))
        {
            //Load all the tiles
            var guids = AssetDatabase.FindAssets("t:Tile3D");
            var tiles = new Tile3D[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                tiles[i] = AssetDatabase.LoadAssetAtPath<Tile3D>(path);
            }
            System.Array.Sort(tiles, (a, b) => a.id.CompareTo(b.id));

            //Get the tiles texture
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TileEditor3D/Assets/Tiles.png");
            var pixels = new Color32[texture.width * texture.height];

            int tileIndex = 0;
            int maxHeight = 0;
            for (int y = 1; y < texture.height && tileIndex < tiles.Length; y += maxHeight + 3)
            {
                maxHeight = 0;
                int prevWidth = 0;
                for (int x = 1; x < texture.width && tileIndex < tiles.Length; x += prevWidth + 3)
                {
                    var tile = tiles[tileIndex++];
                    var tex = tile.texture;
                    var tilePixels = tex.GetPixels32();

                    prevWidth = tile.texture.width;
                    maxHeight = Mathf.Max(maxHeight, tile.texture.height);
                    tile.rect = new Rect(x + 1, y + 1, tile.texture.width, tile.texture.height);

                    for (int yy = 0; yy < tex.height + 2; ++yy)
                    {
                        for (int xx = 0; xx < tex.width + 2; ++xx)
                        {
                            var col = GetPixel(tilePixels, tex.width, tex.height, xx - 1, yy - 1);
                            pixels[(y + yy) * texture.width + (x + xx)] = col;
                        }
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(texture);
            EditorWindow.mouseOverWindow.ShowNotification(new GUIContent("Tiles packed."));
        }
    }
}
