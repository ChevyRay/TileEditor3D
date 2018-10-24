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

    void OnEnable()
    {
        CheckPrefabState();
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

        EditorGUILayout.BeginHorizontal();
        var rendMesh = tilemap.GetComponent<MeshFilter>().sharedMesh;
        int vertCount = rendMesh != null ? rendMesh.vertexCount : 0;
        GUILayout.Label("Tiles:", EditorStyles.miniLabel);
        GUILayout.Label(tilemap.tiles.Count.ToString(), EditorStyles.miniLabel);
        GUILayout.Label("Vertices:", EditorStyles.miniLabel);
        GUILayout.Label(vertCount.ToString(), EditorStyles.miniLabel);
        GUILayout.Label("Triangles:", EditorStyles.miniLabel);
        GUILayout.Label((vertCount * 2).ToString(), EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        if (PrefabUtility.GetPrefabType(tilemap.gameObject) == PrefabType.Prefab)
            return;

        var newBox = EditorGUILayout.ObjectField("BoxCollider Prefab", tilemap.boxPrefab, typeof(BoxCollider), false) as BoxCollider;
        if (tilemap.boxPrefab != newBox)
        {
            Undo.RecordObject(tilemap, "set box collider prefab");
            tilemap.boxPrefab = newBox;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("X+", EditorStyles.miniButtonLeft))
            tilemap.ShiftTiles(new Vector3Int(1, 0, 0));
        if (GUILayout.Button("X-", EditorStyles.miniButtonMid))
            tilemap.ShiftTiles(new Vector3Int(-1, 0, 0));
        if (GUILayout.Button("Y+", EditorStyles.miniButtonMid))
            tilemap.ShiftTiles(new Vector3Int(0, 1, 0));
        if (GUILayout.Button("Y+", EditorStyles.miniButtonMid))
            tilemap.ShiftTiles(new Vector3Int(0, -1, 0));
        if (GUILayout.Button("Z+", EditorStyles.miniButtonMid))
            tilemap.ShiftTiles(new Vector3Int(0, 0, 1));
        if (GUILayout.Button("Z+", EditorStyles.miniButtonRight))
            tilemap.ShiftTiles(new Vector3Int(0, 0, -1));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Flip X", EditorStyles.miniButtonLeft))
            EditorApplication.delayCall += tilemap.FlipX;
        if (GUILayout.Button("Flip Z", EditorStyles.miniButtonMid))
            EditorApplication.delayCall += tilemap.FlipZ;
        if (GUILayout.Button("Rotate ↻", EditorStyles.miniButtonMid))
            EditorApplication.delayCall += tilemap.Clockwise;
        if (GUILayout.Button("Rotate ↺", EditorStyles.miniButtonRight))
            EditorApplication.delayCall += tilemap.CounterClockwise;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Base Center", EditorStyles.miniButtonLeft))
            EditorApplication.delayCall += tilemap.BottomCenterOrigin;
        if (GUILayout.Button("Center", EditorStyles.miniButtonMid))
            EditorApplication.delayCall += tilemap.CenterOrigin;

        if (GUILayout.Button("Create Clone", EditorStyles.miniButtonMid))
        {
            var dupe = Instantiate(tilemap);
            dupe.gameObject.name = tilemap.gameObject.name;
            dupe.GetComponent<MeshFilter>().mesh = null;
            dupe.GetComponent<MeshCollider>().sharedMesh = null;
            dupe.BuildLookup();
            dupe.BuildMesh();
            Undo.RegisterCreatedObjectUndo(dupe.gameObject, "create duplicate");
        }
        if (GUILayout.Button("Clear Tiles", EditorStyles.miniButtonRight))
        {
            Undo.RecordObject(tilemap, "clear tiles");
            tilemap.tiles.Clear();
            tilemap.BuildLookup();
            tilemap.BuildMesh();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = tilemap.boxPrefab != null;
        if (GUILayout.Button("Create Boxes", EditorStyles.miniButtonLeft))
            tilemap.CreateBoxColliders();
        GUI.enabled = true;
        if (GUILayout.Button("Clear Boxes", EditorStyles.miniButtonRight))
            tilemap.ClearBoxColliders();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Rebuild Mesh", EditorStyles.miniButtonLeft))
        {
            tilemap.BuildLookup();
            tilemap.BuildMesh();
        }
        var sm = tilemap.GetComponent<MeshFilter>().sharedMesh;
        GUI.enabled = sm != null && sm.vertexCount > 0;
        if (GUILayout.Button("Subdivide Mesh", EditorStyles.miniButtonRight))
        {
            tilemap.BuildLookup();
            tilemap.SubdivideMesh();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    static void BuildPrefab(GameObject prefab, GameObject inst)
    {

    }
    void CheckPrefabState()
    {
        var prefab = (target as Tilemap3D).gameObject;
        var type = PrefabUtility.GetPrefabType(prefab);
        if (type == PrefabType.Prefab)
        {
            foreach (var inst in FindObjectsOfType<GameObject>())
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(inst) == prefab)
                {
                    var rendMesh = inst.GetComponent<MeshFilter>().sharedMesh;
                    var collMesh = inst.GetComponent<MeshCollider>().sharedMesh;

                    if (rendMesh != null && !AssetDatabase.IsSubAsset(rendMesh))
                    {
                        AssetDatabase.AddObjectToAsset(rendMesh, prefab);
                        prefab.GetComponent<MeshFilter>().mesh = rendMesh;
                    }

                    if (collMesh != null && !AssetDatabase.IsSubAsset(collMesh))
                    {
                        AssetDatabase.AddObjectToAsset(collMesh, prefab);
                        prefab.GetComponent<MeshCollider>().sharedMesh = collMesh;
                    }

                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                    return;
                }
            }
        }
    }
}
