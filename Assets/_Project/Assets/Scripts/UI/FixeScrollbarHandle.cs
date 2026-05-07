using System;
using UnityEngine;
using UnityEngine.UI;

public class FixeScrollbarHandle : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Scrollbar _scrollbar;

    private bool _isUpdating = false;

    private void Start()
    {
        _scrollRect.onValueChanged.AddListener(UpdateScrollbar);
        
        _scrollbar.onValueChanged.AddListener(UpdateScrollRect);
    }

    private void UpdateScrollbar(Vector2 scrollPos)
    {
        if (_isUpdating) return; 
        
        _isUpdating = true;
        _scrollbar.value = scrollPos.y;
        _isUpdating = false;
    }

    private void UpdateScrollRect(float scrollVal)
    {
        if (_isUpdating) return;
        
        _isUpdating = true;
        Vector2 newPos = _scrollRect.normalizedPosition;
        newPos.y = scrollVal;
        _scrollRect.normalizedPosition = newPos;
        _isUpdating = false;
    }
}
