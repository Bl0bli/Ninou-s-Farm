using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private Vector2 _currentInput;
    private Vector2 _lastDirection;

    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        _currentInput = context.ReadValue<Vector2>();

        if (_currentInput.sqrMagnitude > 0.01f)
        {
            _lastDirection = _currentInput;
            UpdateAnimator(_currentInput, true);
        }
        else
        {
            UpdateAnimator(_lastDirection, false);
        }
    }

    private void UpdateAnimator(Vector2 dir, bool isMoving)
    {
        if(_animator == null) return;
        _animator.SetFloat("Dir_x", dir.x);
        _animator.SetFloat("Dir_y", dir.y);
        _animator.SetBool("IsMoving", isMoving);
    }
}
