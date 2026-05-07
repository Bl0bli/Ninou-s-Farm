using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;

    public static GameManager Instance;

    private void Awake()
    {
        if(Instance != null) Destroy(this);
        Instance = this;
    }

    /// <summary>
    /// Initialise le gestionnaire de jeu et s'abonne aux événements d'initialisation du monde.
    /// </summary>
    public void Init()
    {
        GridWorld.Instance.OnWorldInit += SpawnPlayer;
    }

    private void SpawnPlayer(Vector2 position)
    {
        _playerTransform.position = position;
    }
}
