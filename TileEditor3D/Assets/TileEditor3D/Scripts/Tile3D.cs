using UnityEngine;

//[CreateAssetMenu(menuName = "Tile3D", fileName = "NewTile", order = 101)]
public class Tile3D : ScriptableObject
{
    public Texture2D texture;

    [HideInInspector]
    public int id;

    [HideInInspector]
    public Rect rect;
}
