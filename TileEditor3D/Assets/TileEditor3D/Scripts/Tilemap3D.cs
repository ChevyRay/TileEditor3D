using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Tilemap3D : MonoBehaviour, ISerializationCallbackReceiver
{
    static List<Vector3> rendVerts = new List<Vector3>();
    static List<Vector2> rendUVs = new List<Vector2>();
    static List<int> rendTris = new List<int>();
    static List<Vector3> collVerts = new List<Vector3>();
    static List<int> collTris = new List<int>();
    static List<Color32> colors = new List<Color32>();

    static Vector2[] rotUVs = new Vector2[4];

    [System.Serializable]
    public class Tile
    {
        public Vector3Int pos;
        public Tile3D top;
        public Tile3D bottom;
        public Tile3D right;
        public Tile3D left;
        public Tile3D front;
        public Tile3D back;
        public int rotFlags;

        int GetFlag(int shift)
        {
            return (rotFlags >> shift) & 0xf;
        }
        void SetFlag(int shift, int value)
        {
            value = ((value % 4) + 4) % 4;
            rotFlags &= ~(0xf << shift);
            rotFlags |= value << shift;
        }

        public int rotTop
        {
            get { return GetFlag(0); }
            set { SetFlag(0, value); }
        }
        public int rotBottom
        {
            get { return GetFlag(4); }
            set { SetFlag(4, value); }
        }
        public int rotRight
        {
            get { return GetFlag(8); }
            set { SetFlag(8, value); }
        }
        public int rotLeft
        {
            get { return GetFlag(12); }
            set { SetFlag(12, value); }
        }
        public int rotFront
        {
            get { return GetFlag(16); }
            set { SetFlag(16, value); }
        }
        public int rotBack
        {
            get { return GetFlag(20); }
            set { SetFlag(20, value); }
        }
    }

    [HideInInspector]
    public Texture2D texture;

    [HideInInspector]
    public List<Tile> tiles = new List<Tile>();

    Dictionary<Vector3Int, Tile> tileLookup = new Dictionary<Vector3Int, Tile>();

    void AddVerts(Tile tile, Tile3D t3, float w, float h, int rot, Vector3 nor,
                 float x0, float y0, float z0,
                 float x1, float y1, float z1,
                 float x2, float y2, float z2,
                 float x3, float y3, float z3,
                 int i0, int i1, int i2,
                 int i3, int i4, int i5)
    {
        var p = tile.pos;
        if (t3 != null)
        {
            int i = rendVerts.Count;
            rendVerts.Add(new Vector3(p.x + x0, p.y + y0, p.z + z0));
            rendVerts.Add(new Vector3(p.x + x1, p.y + y1, p.z + z1));
            rendVerts.Add(new Vector3(p.x + x2, p.y + y2, p.z + z2));
            rendVerts.Add(new Vector3(p.x + x3, p.y + y3, p.z + z3));
            var r = t3.rect;

            rotUVs[0] = new Vector2(r.xMin / w, r.yMin / h);
            rotUVs[1] = new Vector2(r.xMax / w, r.yMin / h);
            rotUVs[2] = new Vector2(r.xMax / w, r.yMax / h);
            rotUVs[3] = new Vector2(r.xMin / w, r.yMax / h);
            for (int j = 0; j < 4; ++j)
                rendUVs.Add(rotUVs[(j + rot) % 4]);

            rendTris.Add(i + i0); rendTris.Add(i + i1); rendTris.Add(i + i2);
            rendTris.Add(i + i3); rendTris.Add(i + i4); rendTris.Add(i + i5);
        }
        {
            int i = collVerts.Count;
            collVerts.Add(new Vector3(p.x + x0, p.y + y0, p.z + z0));
            collVerts.Add(new Vector3(p.x + x1, p.y + y1, p.z + z1));
            collVerts.Add(new Vector3(p.x + x2, p.y + y2, p.z + z2));
            collVerts.Add(new Vector3(p.x + x3, p.y + y3, p.z + z3));
            collTris.Add(i + i0); collTris.Add(i + i1); collTris.Add(i + i2);
            collTris.Add(i + i3); collTris.Add(i + i4); collTris.Add(i + i5);
        }
    }

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        BuildLookup();
    }

    public Tile GetTile(Vector3Int pos)
    {
        Tile tile;
        tileLookup.TryGetValue(pos, out tile);
        return tile;
    }
    public Tile GetTile(Vector3 pos)
    {
        return GetTile(new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z)));
    }

    public void BuildLookup()
    {
        tileLookup.Clear();
        foreach (var tile in tiles)
            tileLookup[tile.pos] = tile;
    }

    public Tile3D GetSurface(Vector3 point, Vector3 normal)
    {
        var tile = GetTile(point - normal * 0.5f);
        if (tile != null)
        {
            if (normal.x > 0.5f)
                return tile.right;
            else if (normal.x < -0.5f)
                return tile.left;
            else if (normal.z > 0.5f)
                return tile.front;
            else if (normal.z < -0.5f)
                return tile.back;
            else if (normal.y > 0.5f)
                return tile.top;
            else
                return tile.bottom;
        }
        return null;
    }

    public Tile3D GetContactSurface(ContactPoint contact)
    {
        return GetSurface(contact.point, contact.normal);
    }

    public bool RaycastTile(Ray ray, float maxDistance, out RaycastHit hit, out Tile tile)
    {
        var coll = GetComponent<MeshCollider>();
        if (coll.Raycast(ray, out hit, maxDistance))
        {
            tile = GetTile(hit.point - hit.normal * 0.5f);
            return true;
        }
        tile = null;
        return false;
    }
    public bool RaycastTile(Ray ray, float maxDistance, out RaycastHit hit, out Tile3D tile)
    {
        var coll = GetComponent<MeshCollider>();
        if (coll.Raycast(ray, out hit, maxDistance))
        {
            tile = GetSurface(hit.point, hit.normal);
            return true;
        }
        tile = null;
        return false;
    }

#if UNITY_EDITOR
    public void BuildMesh()
    {
        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/TileEditor3D/Assets/Tiles.mat");
        texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TileEditor3D/Assets/Tiles.png");
        var rend = GetComponent<MeshRenderer>();
        rend.material = mat;

        var tex = mat.mainTexture = texture;

        var filter = GetComponent<MeshFilter>();
        if (filter.sharedMesh == null)
        {
            filter.sharedMesh = new Mesh();
            filter.sharedMesh.name = "Tilemap3D Mesh";
            //filter.sharedMesh.hideFlags = HideFlags.HideAndDontSave;
        }

        var coll = GetComponent<MeshCollider>();
        if (coll.sharedMesh == null)
        {
            coll.sharedMesh = new Mesh();
            coll.sharedMesh.name = "Tilemap 3D Collider";
            //coll.sharedMesh.hideFlags = HideFlags.HideAndDontSave;
        }

        rendVerts.Clear();
        rendUVs.Clear();
        rendTris.Clear();
        collVerts.Clear();
        collTris.Clear();

        foreach (var tile in tiles)
        {
            var p = tile.pos;

            //Top
            if (!tileLookup.ContainsKey(p + Vector3Int.up))
            {
                AddVerts(tile, tile.top, tex.width, tex.height, tile.rotTop, Vector3.up,
                        -0.5f, 0.5f, -0.5f,
                        0.5f, 0.5f, -0.5f,
                        0.5f, 0.5f, 0.5f,
                        -0.5f, 0.5f, 0.5f,
                        2, 1, 0, 3, 2, 0);
            }

            //Bottom
            if (!tileLookup.ContainsKey(p + Vector3Int.down))
            {
                AddVerts(tile, tile.bottom, tex.width, tex.height, tile.rotBottom, Vector3.down,
                         -0.5f, -0.5f, 0.5f,
                         0.5f, -0.5f, 0.5f,
                         0.5f, -0.5f, -0.5f,
                         -0.5f, -0.5f, -0.5f,
                         2, 1, 0, 3, 2, 0);
            }

            //Front
            if (!tileLookup.ContainsKey(p + new Vector3Int(0, 0, 1)))
            {
                AddVerts(tile, tile.front, tex.width, tex.height, tile.rotFront, Vector3.forward,
                         0.5f, -0.5f, 0.5f,
                         -0.5f, -0.5f, 0.5f,
                         -0.5f, 0.5f, 0.5f,
                         0.5f, 0.5f, 0.5f,
                         2, 1, 0, 3, 2, 0);
            }

            //Back
            if (!tileLookup.ContainsKey(p + new Vector3Int(0, 0, -1)))
            {
                AddVerts(tile, tile.back, tex.width, tex.height, tile.rotBack, Vector3.back,
                         -0.5f, -0.5f, -0.5f,
                         0.5f,-0.5f, -0.5f,
                         0.5f, 0.5f, -0.5f,
                         -0.5f, 0.5f, -0.5f,
                         2, 1, 0, 3, 2, 0);
            }

            //Right
            if (!tileLookup.ContainsKey(p + Vector3Int.right))
            {
                AddVerts(tile, tile.right, tex.width, tex.height, tile.rotRight, Vector3.right,
                         0.5f, -0.5f, -0.5f,
                         0.5f, -0.5f, 0.5f,
                         0.5f, 0.5f, 0.5f,
                         0.5f, 0.5f, -0.5f,
                         2, 1, 0, 3, 2, 0);
            }

            //Left
            if (!tileLookup.ContainsKey(p + Vector3Int.left))
            {
                AddVerts(tile, tile.left, tex.width, tex.height, tile.rotLeft, Vector3.left,
                         -0.5f, -0.5f, 0.5f,
                         -0.5f, -0.5f, -0.5f,
                         -0.5f, 0.5f, -0.5f,
                         -0.5f, 0.5f, 0.5f,
                         2, 1, 0, 3, 2, 0);
            }
        }

        Color32 white = Color.white;
        for (int i = 0; i < rendVerts.Count; ++i)
            colors.Add(white);
        colors.Clear();

        filter.sharedMesh.Clear();
        filter.sharedMesh.SetVertices(rendVerts);
        filter.sharedMesh.SetUVs(0, rendUVs);
        filter.sharedMesh.SetTriangles(rendTris, 0);
        filter.sharedMesh.SetColors(colors);
        filter.sharedMesh.UploadMeshData(false);
        filter.sharedMesh.RecalculateNormals();
        filter.sharedMesh.RecalculateBounds();
        filter.sharedMesh.RecalculateTangents();
        filter.mesh = filter.sharedMesh;

        coll.sharedMesh.Clear();
        coll.sharedMesh.SetVertices(collVerts);
        coll.sharedMesh.SetTriangles(collTris, 0);
        coll.sharedMesh.UploadMeshData(false);
        coll.sharedMesh.RecalculateNormals();
        coll.sharedMesh.RecalculateBounds();
        coll.sharedMesh.RecalculateTangents();
        coll.sharedMesh = coll.sharedMesh;

        var pos = transform.localPosition;
        transform.localPosition += Vector3.one;
        transform.localPosition = pos;
    }

    void DrawSide(ref Matrix4x4 m, Vector3 pos, Vector3 nor)
    {
        var rot = Quaternion.LookRotation(nor);
        Gizmos.matrix = m * Matrix4x4.Translate(pos) * Matrix4x4.Rotate(rot);
        const float z = 0.503f;
        Gizmos.DrawLine(new Vector3(-0.5f, -0.5f, z), new Vector3(0.5f, -0.5f, z));
        Gizmos.DrawLine(new Vector3(-0.5f, 0.5f, z), new Vector3(0.5f, 0.5f, z));
        Gizmos.DrawLine(new Vector3(-0.5f, -0.5f, z), new Vector3(-0.5f, 0.5f, z));
        Gizmos.DrawLine(new Vector3(0.5f, -0.5f, z), new Vector3(0.5f, 0.5f, z));
    }

    void OnDrawGizmosSelected()
    {
        if (!UnityEditor.EditorPrefs.GetBool("Tilemap3D_drawGrid"))
            return;

        if (tileLookup == null)
            BuildLookup();

        var m = transform.localToWorldMatrix;
        var cam = UnityEditor.SceneView.lastActiveSceneView.camera;
        var camDir = cam.transform.forward;

        Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
        foreach (var tile in tiles)
        {
            //if (tile.front == null)
            if (Vector3.Dot(camDir, Vector3.forward) < 0f && !tileLookup.ContainsKey(tile.pos + new Vector3Int(0, 0, 1)))
                DrawSide(ref m, tile.pos, Vector3.forward);
            //if (tile.back == null)
            if (Vector3.Dot(camDir, Vector3.back) < 0f && !tileLookup.ContainsKey(tile.pos + new Vector3Int(0, 0, -1)))
                DrawSide(ref m, tile.pos, Vector3.back);
            //if (tile.left == null)
            if (Vector3.Dot(camDir, Vector3.left) < 0f && !tileLookup.ContainsKey(tile.pos + new Vector3Int(-1, 0, 0)))
                DrawSide(ref m, tile.pos, Vector3.left);
            //if (tile.right == null)
            if (Vector3.Dot(camDir, Vector3.right) < 0f && !tileLookup.ContainsKey(tile.pos + new Vector3Int(1, 0, 0)))
                DrawSide(ref m, tile.pos, Vector3.right);
            //if (tile.top == null)
            if (Vector3.Dot(camDir, Vector3.up) < 0f && !tileLookup.ContainsKey(tile.pos + new Vector3Int(0, 1, 0)))
                DrawSide(ref m, tile.pos, Vector3.up);
            //if (tile.bottom == null)
            if (Vector3.Dot(camDir, Vector3.down) < 0f && !tileLookup.ContainsKey(tile.pos + new Vector3Int(0, -1, 0)))
                DrawSide(ref m, tile.pos, Vector3.down);
        }
    }

    public void RemoveTile(Vector3Int pos)
    {
        var tile = GetTile(pos);
        if (tile != null)
            tiles.Remove(tile);
    }

    public void PaintTile(Vector3Int pos, Vector3Int nor, Tile3D surface, int rot)
    {
        var tile = GetTile(pos);
        if (tile == null)
            return;
        if (nor.x > 0)
        {
            tile.right = surface;
            tile.rotRight = rot;
        }
        else if (nor.x < 0)
        {
            tile.left = surface;
            tile.rotLeft = rot;
        }
        else if (nor.y > 0)
        {
            tile.top = surface;
            tile.rotTop = rot;
        }
        else if (nor.y < 0)
        {
            tile.bottom = surface;
            tile.rotBottom = rot;
        }
        else if (nor.z > 0)
        {
            tile.front = surface;
            tile.rotFront = rot;
        }
        else if (nor.z < 0)
        {
            tile.back = surface;
            tile.rotBack = rot;
        }
    }

    void GetTileBounds(out Vector3Int min, out Vector3Int max)
    {
        min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        foreach (var tile in tiles)
        {
            min = Vector3Int.Min(min, tile.pos);
            max = Vector3Int.Max(max, tile.pos);
        }
    }

    void ShiftLeft()
    {
        UnityEditor.Undo.RecordObject(this, "shift left");
        foreach (var tile in tiles)
            tile.pos += Vector3Int.left;
        BuildLookup();
        BuildMesh();
    }

    void ShiftRight()
    {
        UnityEditor.Undo.RecordObject(this, "shift right");
        foreach (var tile in tiles)
            tile.pos += Vector3Int.right;
        BuildLookup();
        BuildMesh();
    }

    void ShiftForward()
    {
        UnityEditor.Undo.RecordObject(this, "shift forward");
        foreach (var tile in tiles)
            tile.pos += new Vector3Int(0, 0, 1);
        BuildLookup();
        BuildMesh();
    }

    void ShiftBack()
    {
        UnityEditor.Undo.RecordObject(this, "shift back");
        foreach (var tile in tiles)
            tile.pos += new Vector3Int(0, 0, -1);
        BuildLookup();
        BuildMesh();
    }

    void ShiftUp()
    {
        UnityEditor.Undo.RecordObject(this, "shift up");
        foreach (var tile in tiles)
            tile.pos += Vector3Int.up;
        BuildLookup();
        BuildMesh();
    }

    void ShiftDown()
    {
        UnityEditor.Undo.RecordObject(this, "shift down");
        foreach (var tile in tiles)
            tile.pos += Vector3Int.down;
        BuildLookup();
        BuildMesh();
    }

    public void FlipX()
    {
        UnityEditor.Undo.RecordObject(this, "flip x");
        foreach (var tile in tiles)
            tile.pos = new Vector3Int(-tile.pos.x, tile.pos.y, tile.pos.z);
        BuildLookup();
        BuildMesh();
    }

    public void FlipZ()
    {
        UnityEditor.Undo.RecordObject(this, "flip z");
        foreach (var tile in tiles)
            tile.pos = new Vector3Int(tile.pos.x, tile.pos.y, -tile.pos.z);
        BuildLookup();
        BuildMesh();
    }

    public void Clockwise()
    {
        UnityEditor.Undo.RecordObject(this, "clockwise");
        var rot = Quaternion.Euler(0f, 90f, 0f);
        foreach (var tile in tiles)
        {
            var p = rot * tile.pos;
            tile.pos = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
            var temp = tile.right;
            tile.right = tile.front;
            tile.front = tile.left;
            tile.left = tile.back;
            tile.back = temp;
        }
        BuildLookup();
        BuildMesh();
    }

    public void CounterClockwise()
    {
        UnityEditor.Undo.RecordObject(this, "counter-clockwise");
        var rot = Quaternion.Euler(0f, -90f, 0f);
        foreach (var tile in tiles)
        {
            var p = rot * tile.pos;
            tile.pos = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
            var temp = tile.right;
            tile.right = tile.back;
            tile.back = tile.left;
            tile.left = tile.front;
            tile.front = temp;
        }
        BuildLookup();
        BuildMesh();
    }

    public void BottomCenterOrigin()
    {
        UnityEditor.Undo.RecordObject(this, "bottom center");

        Vector3Int min, max;
        GetTileBounds(out min, out max);

        var center = min;
        center.x += (max.x - min.x) / 2;
        center.z += (max.z - min.z) / 2;

        foreach (var tile in tiles)
            tile.pos -= center;

        BuildLookup();
        BuildMesh();
    }

    public void CenterOrigin()
    {
        UnityEditor.Undo.RecordObject(this, "center");

        Vector3Int min, max;
        GetTileBounds(out min, out max);

        var center = min;
        center.x += (max.x - min.x) / 2;
        center.y += (max.y - min.y) / 2;
        center.z += (max.z - min.z) / 2;

        foreach (var tile in tiles)
            tile.pos -= center;

        BuildLookup();
        BuildMesh();
    }

#endif
}
