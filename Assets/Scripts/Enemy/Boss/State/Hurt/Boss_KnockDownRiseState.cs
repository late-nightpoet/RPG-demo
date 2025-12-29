using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_KnockDownRiseState : Boss_HurtStateBase
{
    private const string AnimKnockDownRise = "KnockDownEnd";
    public override void Enter()
    {
        base.Enter();
        boss.PlayAnimation(AnimKnockDownRise);
    }

    public override void Update()
    {
        UpdateMovement(false);
        if (CheckAnimatorStateName(AnimKnockDownRise, out float endTime) && endTime >= 1f)
            {
                RestoreCapsule();
                boss.ChangeState(BossState.Idle);
            }
    }
}
