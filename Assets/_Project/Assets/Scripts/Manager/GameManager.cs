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

    public void Init()
    {
        GridWorld.Instance.OnWorldInit += SpawnPlayer;
    }

    private void SpawnPlayer(Vector2 position)
    {
        _playerTransform.position = position;
    }
}
