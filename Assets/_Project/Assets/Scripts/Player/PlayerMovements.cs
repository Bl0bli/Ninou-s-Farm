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
    private bool _isMoving = false, _isGrounded = true;

    /// <summary>
    /// Gère les entrées de mouvement du joueur via l'Input System.
    /// </summary>
    /// <param name="context">Le contexte de l'action contenant la valeur du mouvement.</param>
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
        _rb.MovePosition(_rb.position + _currentInput * _speedScaler * Time.fixedDeltaTime);
        /*if (_isMoving)
        {
            Vector2 nextPosition = _rb.position + _currentInput * _speedScaler * Time.fixedDeltaTime;
            TileData targetTile = GridWorld.Instance.GetTileAt(nextPosition);

            bool wasGroundedBeforePhysics = _isGrounded;

            if (targetTile.Type != TileType.WATER)
            {
                _rb.MovePosition(nextPosition);
                _isGrounded = true;
            }
            else
            {
                _isGrounded = false;
            }

            if(wasGroundedBeforePhysics != _isGrounded) UpdateAnimator();
        }*/
    }

    private void UpdateAnimator()
    {
        if(_animator == null) return;

        Vector2 dir = _isMoving ? _currentInput : _lastDirection;
        
        _animator.SetFloat("Dir_x", dir.x);
        _animator.SetFloat("Dir_y", dir.y);
        _animator.SetBool("IsMoving", _isMoving && _isGrounded);
    }
}
