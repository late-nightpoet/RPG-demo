using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_KnockAirLoopState : Player_HurtStateBase
{
    public override void Enter()
    {
        base.Enter();
        player.PlayAnimation("KnockAirLoop");
    }

    public override void Update()
    {
         //空中状态只需要y方向的速度
        UpdateMovement(false);
        if (IsGrounded())
            {
                player.ChangeState(PlayerState.KnockDownLand);
            }
    }
}
