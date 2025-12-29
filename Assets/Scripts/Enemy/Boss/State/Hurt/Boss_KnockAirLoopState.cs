using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_KnockAirLoopState : Boss_HurtStateBase
{
    public override void Enter()
    {
        base.Enter();
        boss.PlayAnimation("KnockAirLoop");
    }

    public override void Update()
    {
         //空中状态只需要y方向的速度
        UpdateMovement(false);
        if (IsGrounded())
            {
                boss.ChangeState(BossState.KnockDownLand);
            }
    }
}
