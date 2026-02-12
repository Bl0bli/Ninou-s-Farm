using UnityEngine;

public enum TileState
{
    NONE,       
    HOED,
    PLANTED,  
    WATERED,    
    BLOCKED
}
public class WorldTile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    private TileState _tileState;

    public TileState TileState => _tileState;

    public void SetSprite(Sprite sprite)
    {
        _renderer.sprite = sprite;
    }
}
