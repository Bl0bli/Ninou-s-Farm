using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject _uiWindow;

    private bool _uiToggle = false;

    public void ToogleUI()
    {
        _uiToggle = !_uiToggle;
        _uiWindow.SetActive(_uiToggle);
    }
}
