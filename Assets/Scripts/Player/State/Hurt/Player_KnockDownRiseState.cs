using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_KnockDownRiseState : Player_HurtStateBase
{
    private const string AnimKnockDownRise = "KnockDownEnd";
    public override void Enter()
    {
        base.Enter();
        player.PlayAnimation(AnimKnockDownRise);
    }

    public override void Update()
    {
        UpdateMovement(false);
        if (CheckAnimatorStateName(AnimKnockDownRise, out float endTime) && endTime >= 1f)
            {
                RestoreCapsule();
                player.ChangeState(PlayerState.Idle);
            }
    }
}
