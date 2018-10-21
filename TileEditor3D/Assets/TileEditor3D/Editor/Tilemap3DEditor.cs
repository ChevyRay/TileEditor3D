using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Tilemap3D))]
public class Tilemap3DEditor : Editor
{
    [MenuItem("GameObject/3D Object/Tilemap3D")]
    static void CreateInstance()
    {
        var obj = new GameObject("Tilemap3D", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(Tilemap3D));
        //obj.isStatic = true;
        var rend = obj.GetComponent<MeshRenderer>();
        rend.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/TileEditor3D/Assets/Tiles.mat");
        Undo.RegisterCreatedObjectUndo(obj, "create Tilemap3D");
        Selection.activeObject = obj;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var tilemap = (Tilemap3D)target;
        tilemap.BuildLookup();

        int sideCount = 0;
        foreach (var tile in tilemap.tiles)
        {
            if (tile.right != null && tilemap.GetTile(tile.pos + new Vector3Int(1, 0, 0)) == null) ++sideCount;
            if (tile.left != null && tilemap.GetTile(tile.pos + new Vector3Int(-1, 0, 0)) == null) ++sideCount;
            if (tile.front != null && tilemap.GetTile(tile.pos + new Vector3Int(0, 0, 1)) == null) ++sideCount;
            if (tile.back != null && tilemap.GetTile(tile.pos + new Vector3Int(0, 0, -1)) == null) ++sideCount;
            if (tile.top != null && tilemap.GetTile(tile.pos + new Vector3Int(0, 1, 0)) == null) ++sideCount;
            if (tile.bottom != null && tilemap.GetTile(tile.pos + new Vector3Int(0, -1, 0)) == null) ++sideCount;
        }

        EditorGUILayout.LabelField("Tile Count", tilemap.tiles.Count.ToString());
        EditorGUILayout.LabelField("Triangle Count", (sideCount * 2).ToString());

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Flip X"))
            EditorApplication.delayCall += tilemap.FlipX;
        if (GUILayout.Button("Flip Z"))
            EditorApplication.delayCall += tilemap.FlipZ;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Rotate ↻"))
            EditorApplication.delayCall += tilemap.Clockwise;
        if (GUILayout.Button("Rotate ↺"))
            EditorApplication.delayCall += tilemap.CounterClockwise;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Base Center"))
            EditorApplication.delayCall += tilemap.BottomCenterOrigin;
        if (GUILayout.Button("Center"))
            EditorApplication.delayCall += tilemap.CenterOrigin;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear Tiles", EditorStyles.miniButton))
        {
            Undo.RecordObject(tilemap, "clear tiles");
            tilemap.tiles.Clear();
            tilemap.BuildLookup();
            tilemap.BuildMesh();
        }
        EditorGUILayout.EndHorizontal();
    }
}
