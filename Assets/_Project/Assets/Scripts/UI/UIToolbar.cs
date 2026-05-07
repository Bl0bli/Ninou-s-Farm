using UnityEngine;

public class UIToolbar : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private MenuSelector _menuSelector;
    
    [SerializeField] private Transform[] _uiSlotTransforms; 

    private void OnEnable()
    {
        if (_playerController != null)
            _playerController.OnSlotSelected += MoveSelectorVisually;
    }

    private void OnDisable()
    {
        if (_playerController != null)
            _playerController.OnSlotSelected -= MoveSelectorVisually;
    }

    private void MoveSelectorVisually(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _uiSlotTransforms.Length) return;

        Transform targetSlot = _uiSlotTransforms[slotIndex];

        _menuSelector.AnimateSelection(targetSlot);
    }
}
