using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _speedScaler;
    [SerializeField] private float _speedIdle = 1.25f;
    private Vector2 _currentInput;
    private Vector2 _lastDirection;
    private bool _isMoving = false;

    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        _currentInput = context.ReadValue<Vector2>();

        if (_currentInput.sqrMagnitude > 0.01f)
        {
            _lastDirection = _currentInput;
            _isMoving = true;
            _animator.speed = _speedScaler / 2;
        }
        else
        {
            _isMoving = false;
            _animator.speed = _speedIdle;
        }
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (_isMoving)
        {
            _rb.MovePosition(_rb.position + _currentInput * _speedScaler * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimator()
    {
        if(_animator == null) return;

        Vector2 dir = _isMoving ? _currentInput : _lastDirection;
        
        _animator.SetFloat("Dir_x", dir.x);
        _animator.SetFloat("Dir_y", dir.y);
        _animator.SetBool("IsMoving", _isMoving);
    }
}
