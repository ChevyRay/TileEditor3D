using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TileEditorWindow : EditorWindow
{
    [MenuItem("Window/Tile Editor")]
    public static void CreateWindow()
    {
        var type = mouseOverWindow != null ? mouseOverWindow.GetType() : null;
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/TileEditor Icon.png");
        var win = GetWindow<TileEditorWindow>("Tile Editor", true, type);
        win.titleContent = new GUIContent("Tile Editor", icon);
        win.minSize = new Vector2(170f, 0f);
        win.Show();
    }

    public enum EditMode
    {
        None,
        Block,
        Paint
    }

    //Tilemap3D tilemap;
    EditMode editMode;
    Tile3D selectedTile;
    Vector2 mousePos;
    bool ctrlDown;
    bool dragging;
    bool deleting;
    Vector3Int dragStart;
    Vector3Int dragEnd;
    Plane dragPlane;
    bool dragFloor;

    Tile3D[] tiles;
    string[] tilePaths;
    string search = string.Empty;

    int rotation;

    bool overwriteBlock = false;
    bool paintInvisible = true;

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;

        Undo.undoRedoPerformed -= OnUndoRedo;
        Undo.undoRedoPerformed += OnUndoRedo;

        LoadTiles();
        if (selectedTile == null && tiles.Length > 0)
            selectedTile = tiles[0];
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    void OnSelectionChange()
    {
        if (tilemap == null)
        {
            editMode = EditMode.None;
            rotation = 0;
            rayHit = false;
            hitPoint = Vector3.zero;
            hoverPos = Vector3Int.zero;
            hoverNor = Vector3Int.up;
            isHovering = false;
            isFloor = false;
        }
        Repaint();
    }

    void OnUndoRedo()
    {
        if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        {
            editMode = EditMode.None;
            return;
        }
        BuildAndRetainSubdivisions();
    }

    void SetEditMode(EditMode mode)
    {
        if (editMode == mode)
            return;

        Tools.current = Tool.None;
        editMode = mode;

        Repaint();
    }

    bool canEdit
    {
        get
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isPaused ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
                return false;
            return true;
        }
    }

    void LoadTiles()
    {
        //Load all the tiles
        var guids = AssetDatabase.FindAssets("t:Tile3D");
        tilePaths = new string[guids.Length];
        tiles = new Tile3D[guids.Length];
        for (int i = 0; i < guids.Length; ++i)
        {
            tilePaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            tiles[i] = AssetDatabase.LoadAssetAtPath<Tile3D>(tilePaths[i]);
        }
    }

    void OnGUI()
    {
        if (!canEdit)
        {
            EditorGUILayout.HelpBox("Editing disabled.", MessageType.Warning);
            return;
        }

        if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        {
            EditorGUILayout.HelpBox("Select a Tilemap3D to edit.", MessageType.Info);
            return;
        }

        var noneIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/TileNone Icon.png");
        var blockIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/TileBlock Icon.png");
        var paintIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/TilePaint Icon.png");

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = editMode != EditMode.None;
        if (GUILayout.Button(new GUIContent(noneIcon)))
            SetEditMode(EditMode.None);
        GUI.enabled = editMode != EditMode.Block;
        if (GUILayout.Button(new GUIContent(blockIcon)))
            SetEditMode(EditMode.Block);
        GUI.enabled = editMode != EditMode.Paint;
        if (GUILayout.Button(new GUIContent(paintIcon)))
            SetEditMode(EditMode.Paint);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (editMode != EditMode.None)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
                EditorGUILayout.HelpBox("Hold ⌘ to delete.", MessageType.Info, true);
            else
                EditorGUILayout.HelpBox("Hold CTRL to delete.", MessageType.Info, true);
        }

        var drawGrid = EditorPrefs.GetBool("Tilemap3D_drawGrid", true);
        drawGrid = EditorGUILayout.Toggle("Draw Grid", drawGrid);
        EditorPrefs.SetBool("Tilemap3D_drawGrid", drawGrid);

        paintInvisible = EditorGUILayout.Toggle("Raycast Invisible", paintInvisible);

        if (editMode == EditMode.Block)
            overwriteBlock = EditorGUILayout.Toggle("Overwrite", overwriteBlock);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↻"))
            rotation = (rotation + 1) % 4;
        if (GUILayout.Button("↺"))
            rotation = (rotation + 5) % 4;    
        EditorGUILayout.EndHorizontal();

        LoadTiles();

        var viewWidth = EditorGUIUtility.currentViewWidth - 10f;

        EditorGUILayout.Space();

        var searchRect = EditorGUILayout.GetControlRect(GUILayout.Width(viewWidth - 12f));
        var cancelRect = searchRect;
        cancelRect.x = cancelRect.xMax;
        cancelRect.width = 10f;

        var searchStyle = GUI.skin.GetStyle("ToolbarSeachTextField");
        var searchCancel = GUI.skin.GetStyle("ToolbarSeachCancelButton");
        var searchEnd = GUI.skin.GetStyle("ToolbarSeachCancelButtonEmpty");

        search = EditorGUI.TextField(searchRect, search, searchStyle);
        if (GUI.Button(cancelRect, "", string.IsNullOrEmpty(search) ? searchEnd : searchCancel))
        {
            search = string.Empty;
            GUIUtility.keyboardControl = 0;
        }

        var tiles = new List<Tile3D>();
        if (!string.IsNullOrEmpty(search))
        {
            var split = search.Split(' ');
            for (int i = 0; i < split.Length; ++i)
                split[i] = split[i].ToLower();
            foreach (var tile in this.tiles)
            {
                var tileName = tile.name.ToLower();
                foreach (var part in split)
                {
                    if (tileName.Contains(part))
                    {
                        tiles.Add(tile);
                        break;
                    }
                }
            }
        }
        else
            tiles.AddRange(this.tiles);

        //Draw the tiles on a grid
        var slotSize = 32f;
        var rect = GUILayoutUtility.GetRect(viewWidth, slotSize);
        rect.x += 4f;
        var slot = new Rect(rect.x, rect.y, slotSize, slotSize);
        for (int i = 0; i < tiles.Count; ++i)
        {
            var tile = tiles[i];
            GUI.enabled = tile != selectedTile;

            GUI.matrix = Matrix4x4.identity;
            var btnRect = new Rect(slot.x + 1f, slot.y + 1f, slot.width - 2f, slot.height - 2f);
            if (GUI.Button(btnRect, string.Empty))
                selectedTile = tile;

            GUI.color = Color.white * (GUI.enabled ? 1f : 0.8f);
            var texRect = new Rect(btnRect.x + 2f, btnRect.y + 2f, btnRect.width - 4f, btnRect.height - 4f);

            if (rotation > 0)
                GUIUtility.RotateAroundPivot(rotation * 90f, texRect.center);

            GUI.DrawTexture(texRect, tile.texture);
            GUI.color = Color.white;

            slot.x += slotSize;
            if (slot.xMax > rect.xMax)
            {
                rect = GUILayoutUtility.GetRect(viewWidth, slotSize);
                rect.x += 4f;
                slot = new Rect(rect.x, rect.y, slotSize, slotSize);
            }
        }

        GUI.matrix = Matrix4x4.identity;

        if (tilemap != null)
            Hotkeys();
    }

    void Hotkeys()
    {
        if (ev.type == EventType.KeyDown)
        {
            if (ev.keyCode == KeyCode.Alpha1 || ev.keyCode == KeyCode.Escape)
            {
                SetEditMode(EditMode.None);
                ev.Use();
            }
            else if (ev.keyCode == KeyCode.Alpha2)
            {
                SetEditMode(EditMode.Block);
                ev.Use();
            }
            else if (ev.keyCode == KeyCode.Alpha3)
            {
                SetEditMode(EditMode.Paint);
                ev.Use();
            }
            else if (ev.keyCode == KeyCode.R)
            {
                rotation = (rotation + 1) % 4;
                ev.Use();
                Repaint();
            }
        }
    }

    Vector3 hitPoint;
    Vector3Int hoverPos;
    Vector3Int hoverNor = Vector3Int.up;
    bool isHovering;
    bool isFloor;
    bool rayHit;
    RaycastHit hit;

    void OnSceneGUI(SceneView view)
    {
        if (!canEdit)
            return;

        int control = GUIUtility.GetControlID(FocusType.Passive);

        Hotkeys();

        //DRaw the
        if (this.tilemap != null)
        {
            Handles.matrix = this.tilemap.transform.localToWorldMatrix;
            Handles.color = Color.red;
            Handles.DrawLine(Vector3.left, Vector3.right);
            Handles.color = new Color32(0x38, 0x81, 0xee, 0xff);
            Handles.DrawLine(Vector3.back, Vector3.forward);
        }

        if (editMode == EditMode.None)
            return;

        HandleUtility.Repaint();

        bool leftDown = false;
        bool leftUp = false;
        ctrlDown = ev.command || ev.control;

        //Handle Unity GUI events
        switch (ev.type)
        {
            case EventType.Layout:
                HandleUtility.AddDefaultControl(control);
                break;
            case EventType.MouseDown:
                if (ev.button == 0)
                {
                    leftDown = true;
                    mousePos = ev.mousePosition;
                    ev.Use();
                }
                break;
            case EventType.MouseUp:
                if (ev.button == 0)
                {
                    leftUp = true;
                    mousePos = ev.mousePosition;
                    ev.Use();
                }
                break;
            case EventType.MouseDrag:
            case EventType.MouseMove:
                mousePos = ev.mousePosition;
                break;
        }

        //Variables for basic information and mouse contact
        var tilemap = this.tilemap;
        var coll = tilemap.GetComponent<MeshCollider>();
        var mouseRay = HandleUtility.GUIPointToWorldRay(mousePos);
        /*hitPoint = Vector3.zero;
        hoverPos = Vector3Int.zero;
        hoverNor = Vector3Int.up;
        isHovering = false;
        isFloor = false;*/
        var t = tilemap.transform;

        RaycastHit hitTest;
        bool rayTest = coll.Raycast(mouseRay, out hitTest, 100000f);
        if (paintInvisible)
        {
            rayHit = rayTest;
            hit = hitTest;
        }
        else if (rayTest && ev.isMouse)
        {
            //WOOT HACK
            var coll2 = coll.gameObject.AddComponent<MeshCollider>();
            coll2.sharedMesh = coll.GetComponent<MeshFilter>().sharedMesh;
            var pos = coll.transform.localPosition;
            coll2.transform.localPosition += Vector3.one;
            coll2.transform.localPosition -= Vector3.one;
            rayHit = coll2.Raycast(mouseRay, out hitTest, 100000f);
            hit = hitTest;
            DestroyImmediate(coll2);
        }

        //Get the mouse contact point on the mesh (or the floor plane)
        if (rayHit)
        {
            isHovering = true;
            isFloor = false;
            hitPoint = t.InverseTransformPoint(hit.point);
            var nor = t.InverseTransformDirection(hit.normal);
            var pos = hitPoint - nor * 0.5f;
            hoverPos = Round(pos);
            hoverNor = Round(nor);
        }
        else
        {
            var plane = new Plane(t.up, t.position - t.up * 0.5f);
            float enter;
            if (plane.Raycast(mouseRay, out enter))
            {
                isHovering = true;
                isFloor = true;
                hitPoint = t.InverseTransformPoint(mouseRay.GetPoint(enter));
                var up = t.InverseTransformDirection(Vector3.up);
                hoverPos = Round(hitPoint - up * 0.5f);
                hoverNor = Round(up);
            }
        }

        //Blocking mode
        if (editMode == EditMode.Block)
        {
            if (selectedTile == null)
                return;

            if (dragging)
            {
                float enter;
                if (dragPlane.Raycast(mouseRay, out enter))
                {
                    var pos = mouseRay.GetPoint(enter);
                    if (deleting && !dragFloor)
                        pos -= dragPlane.normal * 0.5f;
                    else
                        pos += dragPlane.normal * 0.5f;
                    dragEnd = Round(t.InverseTransformPoint(pos));
                }

                var localDragNor = t.InverseTransformDirection(dragPlane.normal);
                var placeNor = Round(localDragNor);

                Handles.matrix = t.localToWorldMatrix;
                if (deleting)
                {
                    foreach (var p in DragPositions())
                    {
                        if (tilemap.GetTile(p) != null)
                        {
                            Handles.color = Color.red;
                            Handles.RectangleHandleCap(control, p + localDragNor * 0.5f, Quaternion.LookRotation(localDragNor), 0.45f, EventType.Repaint);
                        }
                        Handles.color = new Color(1f, 0f, 0f, 0.25f);
                        Handles.CubeHandleCap(control, p, Quaternion.identity, 1f, EventType.Repaint);
                    }
                }
                else
                {
                    foreach (var p in DragPositions())
                    {
                        if (tilemap.GetTile(p - placeNor) != null || (placeNor.y > 0 && p.y == 0))
                        {
                            Handles.color = Color.white;
                            Handles.RectangleHandleCap(control, p - localDragNor * 0.5f, Quaternion.LookRotation(localDragNor), 0.45f, EventType.Repaint);
                        }
                        Handles.color = new Color(1f, 1f, 1f, 0.1f);
                        Handles.CubeHandleCap(control, p, Quaternion.identity, 1f, EventType.Repaint);
                    }
                }

                if (leftUp)
                {
                    dragging = false;

                    Undo.RecordObject(tilemap, "placed blocks");
                    if (deleting)
                    {
                        foreach (var p in DragPositions())
                            tilemap.RemoveTile(p);
                    }
                    else
                    {
                        foreach (var p in DragPositions())
                        {
                            var tile = tilemap.GetTile(p);
                            if (tile == null)
                                tile = new Tilemap3D.Tile();
                            else if (!overwriteBlock)
                                continue;
                                
                            tile.pos = p;
                            tile.right = selectedTile; tile.rotRight = rotation;
                            tile.left = selectedTile; tile.rotLeft = rotation;
                            tile.top = selectedTile; tile.rotTop = rotation;
                            tile.bottom = selectedTile; tile.rotBottom = rotation;
                            tile.front = selectedTile; tile.rotFront = rotation;
                            tile.back = selectedTile; tile.rotBack = rotation;

                            if (!tilemap.tiles.Contains(tile))
                                tilemap.tiles.Add(tile);
                        }
                    }
                    tilemap.BuildLookup();
                    tilemap.BuildMesh();
                }
            }
            else if (isHovering)
            {
                Vector3 pos = hoverPos;
                Vector3 nor = hoverNor;

                Handles.matrix = t.localToWorldMatrix;
                if (ctrlDown)
                {
                    Handles.color = Color.red;
                    Handles.RectangleHandleCap(control, pos + nor * 0.5f, Quaternion.LookRotation(nor), 0.45f, EventType.Repaint);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
                    if (isFloor)
                        pos += nor;
                    Handles.CubeHandleCap(control, pos, Quaternion.identity, 1f, EventType.Repaint);
                }
                else
                {
                    Handles.color = Color.white;
                    Handles.RectangleHandleCap(control, pos + nor * 0.5f, Quaternion.LookRotation(nor), 0.45f, EventType.Repaint);
                    Handles.color = new Color(1f, 1f, 1f, 0.1f);
                    Handles.CubeHandleCap(control, pos + nor, Quaternion.identity, 1f, EventType.Repaint);
                }

                if (leftDown)
                {
                    dragging = true;
                    dragFloor = isFloor;
                    deleting = ctrlDown;
                    dragStart = hoverPos;
                    if (!deleting || isFloor)
                        dragStart += hoverNor;
                    dragEnd = dragStart;
                    dragPlane = new Plane(t.TransformDirection(hoverNor), t.TransformPoint(hitPoint));
                }
            }
        }
        else if (editMode == EditMode.Paint)
        {
            if (selectedTile == null)
                return;

            if (dragging)
            {
                float enter;
                if (dragPlane.Raycast(mouseRay, out enter))
                {
                    var pos = mouseRay.GetPoint(enter) - dragPlane.normal * 0.5f;
                    dragEnd = Round(t.InverseTransformPoint(pos));
                }

                var localDragNor = t.InverseTransformDirection(dragPlane.normal);
                Handles.matrix = t.localToWorldMatrix;
                foreach (var p in DragPositions())
                {
                    Handles.color = deleting ? Color.red : Color.white;
                    if (tilemap.GetTile(p) == null)
                        Handles.color = Handles.color * 0.75f;
                    Handles.RectangleHandleCap(control, p + localDragNor * 0.5f, Quaternion.LookRotation(localDragNor), 0.45f, EventType.Repaint);
                }
               
                if (leftUp)
                {
                    dragging = false;

                    var dragNor = Round(t.InverseTransformDirection(dragPlane.normal));

                    Undo.RecordObject(tilemap, "painted blocks");
                    if (deleting)
                    {
                        foreach (var p in DragPositions())
                            tilemap.PaintTile(p, dragNor, null, 0);
                    }
                    else
                    {
                        foreach (var p in DragPositions())
                            tilemap.PaintTile(p, dragNor, selectedTile, rotation);
                    }
                    tilemap.BuildLookup();
                    tilemap.BuildMesh();
                }
            }
            else if (isHovering)
            {
                Vector3 pos = hoverPos;
                Vector3 nor = hoverNor;

                Handles.matrix = t.localToWorldMatrix;
                if (ctrlDown)
                {
                    Handles.color = Color.red;
                    Handles.RectangleHandleCap(control, pos + nor * 0.5f, Quaternion.LookRotation(nor), 0.45f, EventType.Repaint);
                }
                else
                {
                    Handles.color = Color.white;
                    Handles.RectangleHandleCap(control, pos + nor * 0.5f, Quaternion.LookRotation(nor), 0.45f, EventType.Repaint);
                }

                if (leftDown)
                {
                    dragging = true;
                    deleting = ctrlDown;
                    dragStart = hoverPos;
                    dragEnd = dragStart;
                    dragPlane = new Plane(t.TransformDirection(hoverNor), t.TransformPoint(hitPoint));
                }
            }
        }
    }

    public IEnumerable<Vector3Int> DragPositions()
    {
        var p = new Vector3Int();
        var min = Vector3Int.Min(dragStart, dragEnd);
        var max = Vector3Int.Max(dragStart, dragEnd);
        for (p.x = min.x; p.x <= max.x; ++p.x)
            for (p.y = min.y; p.y <= max.y; ++p.y)
                for (p.z = min.z; p.z <= max.z; ++p.z)
                    yield return p;
    }

    Vector3Int Round(Vector3 p)
    {
        return new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
    }

    public Event ev
    {
        get { return Event.current; }
    }

    public Tilemap3D tilemap
    {
        get
        {
            if (Selection.activeGameObject == null)
                return null;
            return Selection.activeGameObject.GetComponent<Tilemap3D>();
        }
    }

    void BuildAndRetainSubdivisions()
    {
        int sub = tilemap.subdivisions;
        tilemap.BuildLookup();
        tilemap.BuildMesh();
        for (int i = 0; i < sub; ++i)
            tilemap.SubdivideMesh();
    }
}
