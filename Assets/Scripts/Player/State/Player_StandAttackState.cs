using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_StandAttackState : PlayerStateBase
{
    public override void Enter()
    {
       
        // 播放技能
        StandAttack();
    }

    private void StandAttack()
    {
        player.PlayAnimation(player.testSkillConfig.AnimationName);
    }

    public override void Update()
    {
        player.MovementHelper.CalculateInput();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.ApplyGravity();
        if (CheckAnimatorStateName(player.testSkillConfig.AnimationName, out float aniamtionTime) && aniamtionTime>=1)
        {
            // 回到待机
            player.ChangeState(PlayerState.Idle);
        }
    }

  
}
