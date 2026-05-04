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

    /// <summary>
    /// Définit le sprite à afficher pour cette tuile.
    /// </summary>
    /// <param name="sprite">Le sprite à appliquer au SpriteRenderer.</param>
    public void SetSprite(Sprite sprite)
    {
        _renderer.sprite = sprite;
    }
}
