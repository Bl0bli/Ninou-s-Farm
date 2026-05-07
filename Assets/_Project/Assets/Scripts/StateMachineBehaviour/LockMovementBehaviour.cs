using System;
using UnityEngine;

public class LockMovementBehaviour : StateMachineBehaviour
{
    private PlayerMovements _playerMovements;
    
    public void Init(PlayerMovements player) => _playerMovements = player;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(_playerMovements != null ) _playerMovements.CanMove = false;   
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(_playerMovements != null ) _playerMovements.CanMove = true;
    }
}
