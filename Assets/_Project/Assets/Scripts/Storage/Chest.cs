using System;
using UnityEngine;

public class Chest : Storage, IInteractable
{
    [SerializeField] private int _slotsAmount = 20;
    [SerializeField] private ItemData[] _startingItems;
    
    private static readonly int OpenHash = Animator.StringToHash("Open");

    private bool _isOpen = false;
    private Animator _animator;

    /// <summary>Un coffre vient d'être ouvert : l'interface du coffre s'y relie.</summary>
    public static event Action<Chest> OnChestOpened;

    /// <summary>Un coffre vient d'être fermé.</summary>
    public static event Action<Chest> OnChestClosed;

    public bool IsOpen => _isOpen;
    
    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        Initialize(Mathf.Max(_startingItems.Length, _slotsAmount));

        base.Start();

        foreach (ItemData item in _startingItems)
        {
            if (item != null) Add(item, item.Amount);
        }
    }
    
    public bool Interact()
    {
        if(_isOpen)
        {
            Close();
            return true;
        }
        else
        {
            Open();
            return true;
        }
    }

    private void Open()
    {
        _isOpen = true;
        if (_animator != null) _animator.SetBool(OpenHash, true);
        OnChestOpened?.Invoke(this);
    }

    private void Close()
    {
        _isOpen = false;
        if (_animator != null) _animator.SetBool(OpenHash, false);
        OnChestClosed?.Invoke(this);
    }

    /// <summary>
    /// Le joueur s'éloigne : un coffre ouvert se referme.
    /// </summary>
    public void EndInteraction()
    {
        if (_isOpen) Close();
    }

    // Un coffre détruit ou désactivé pendant qu'il est ouvert ne doit pas laisser son interface affichée.
    private void OnDisable()
    {
        if (_isOpen) Close();
    }

    // Test sans action d'input : clic droit sur le composant en Play Mode → Toggle.
    [ContextMenu("Toggle (test)")]
    private void ToggleFromInspector() => Interact();
}
