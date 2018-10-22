using UnityEngine;

//[CreateAssetMenu(menuName = "Tile3D", fileName = "NewTile", order = 101)]
public class Tile3D : ScriptableObject
{
    public Texture2D texture;

    [Tooltip("Add metadata to a tile (eg. sounds to play, terrain type, etc.)")]
    public ScriptableObject data;

    [HideInInspector]
    public Rect rect;
}
