using UnityEngine;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;
    public TileData[,] Tiles = new TileData[SIZE, SIZE];
    public Vector2Int Position;
}
