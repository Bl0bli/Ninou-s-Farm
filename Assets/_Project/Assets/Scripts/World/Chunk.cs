using UnityEngine;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;
    public Tile[,] Tiles = new Tile[SIZE, SIZE];
    public Vector2Int Position;
}
