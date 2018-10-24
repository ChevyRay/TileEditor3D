using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Tile3D))]
public class Tile3DEditor : Editor
{
    [MenuItem("Assets/Create/Tile3D", priority = 101)]
    static void CreateTile3D()
    {
        var tile = CreateInstance<Tile3D>();

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

    Color32 GetNormal(Color32[] pixels, int w, int h, int x, int y)
    {
        //x = Mathf.Clamp(x, 0, w - 1);
        //y = Mathf.Clamp(y, 0, h - 1);
        x = (x + w) % w;
        y = (y + h) % h;
        return pixels[y * w + x];
    }

    bool ForEachTileRect(Tile3D[] tiles, int maxW, int maxH, System.Action<Tile3D, int, int> func)
    {
        int x = 0;
        int y = 0;
        int maxHeight = 0;
        foreach (var tile in tiles)
        {
            if (x + tile.texture.width + 2 > maxW)
            {
                x = 0;
                y += maxHeight;
                maxHeight = 0;
            }
            if (y + tile.texture.height + 2 > maxH)
                return false;
            if (func != null)
                func(tile, x, y);
            x += tile.texture.width + 2;
            maxHeight = Mathf.Max(maxHeight, tile.texture.height + 2);
        }
        return true;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var targ = (Tile3D)target;

        //EditorGUILayout.LabelField("ID", targ.id.ToString());

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
            //System.Array.Sort(tiles, (a, b) => a.id.CompareTo(b.id));

            //Get the required texture size
            int texWidth = 16;
            int texHeight = 16;
            while (!ForEachTileRect(tiles, texWidth, texHeight, null))
            {
                texWidth *= 2;
                if (ForEachTileRect(tiles, texWidth, texHeight, null))
                    break;
                texHeight *= 2;
            }

            //Get the tiles texture
            var pixels = new Color32[texWidth * texHeight];

            //Pack the tiles into the atlas and paint the tileset texture
            ForEachTileRect(tiles, texWidth, texHeight, (t, x, y) =>
            {
                var tex = t.texture;
                var tilePixels = tex.GetPixels32();
                for (int yy = 0; yy < tex.height + 2; ++yy)
                {
                    for (int xx = 0; xx < tex.width + 2; ++xx)
                    {
                        var col = GetPixel(tilePixels, tex.width, tex.height, xx - 1, yy - 1);
                        pixels[(y + yy) * texWidth + x + xx] = col;
                    }
                }
                t.rect = new Rect(x + 1, y + 1, tex.width, tex.height);
            });

            //Set the texture pixels
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TileEditor3D/Assets/Tiles.png");
            texture.Resize(texWidth, texHeight);
            texture.SetPixels32(pixels);
            texture.Apply();

            //Overwrite the texture PNG
            var texBytes = texture.EncodeToPNG();
            var texPath = AssetDatabase.GetAssetPath(texture);
            System.IO.File.WriteAllBytes(texPath, texBytes);

            //Update the assets
            AssetDatabase.ImportAsset(texPath);
            AssetDatabase.SaveAssets();

            //Update UVs of all live tilemaps
            foreach (var tilemap in FindObjectsOfType<Tilemap3D>())
                tilemap.UpdateUVs();
        }
    }
}
