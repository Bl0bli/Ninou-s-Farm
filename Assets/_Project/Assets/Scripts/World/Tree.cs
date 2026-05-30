using System.Collections;
using UnityEngine;

public class Tree : Dropable, IDamageable
{
    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private string _idleAnimName, _hitAnimName;
    [SerializeField] private string _cutAnimName = "Cut";
    [SerializeField] private Animator _animator;
    
    private int _currentHealth;
    private bool _isCut;
    private bool _hasDropped;

    private void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (_isCut)
            return;

        _currentHealth -= amount;
        Debug.Log($"[Tree] Took {amount} damage. Current health: {_currentHealth}");

        _animator.SetTrigger(_hitAnimName);

        if (_currentHealth <= 0)
        {
            _isCut = true;
            _animator.SetBool(_cutAnimName, true);
            StartCoroutine(WaitForCutAnimationThenDrop());
        }
    }

    private IEnumerator WaitForCutAnimationThenDrop()
    {
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(_cutAnimName);
        });
        
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(_cutAnimName)
                   && stateInfo.normalizedTime >= 1f
                   && !_animator.IsInTransition(0);
        });

        if (_hasDropped)
            yield break;

        _hasDropped = true;

        DropItem(true);
    }
}