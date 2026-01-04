using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_KnockDownLandState : Player_HurtStateBase
{

    private const string AnimKnockDownLand = "KnockDownLand";

    private bool animFinished = false;

    private float HardTimeTimer;

    public override void Enter()
    {
        base.Enter();
        player.PlayAnimation(AnimKnockDownLand);
        HardTimeTimer = 0;
        animFinished = false;
        // 确保水平速度为0
        player.Ctx.velocity.x = 0;
        player.Ctx.velocity.z = 0;
    }

    public override void Update()
    {
        UpdateMovement(false);

        // 1. 检查动画是否播完
        if (!animFinished && CheckAnimatorStateName("KnockDownLand", out float progress) && progress >= 1f)
        {
            animFinished = true;
        }
        if (animFinished)
        {
            // 【逻辑修改】如果有硬直时间，需要等待
            if (hitData.HardTime > 0)
            {
                // 累加等待时间
                HardTimeTimer += Time.deltaTime;

                // 只有等待时间超过了设定的 HardTime，才允许起身
                if (HardTimeTimer >= hitData.HardTime)
                    {
                        player.ChangeState(PlayerState.KnockDownRise);
                    }
            }
            else
            {
                // 如果 HardTime 为 0，则维持原有逻辑，动画播完立即起身
                player.ChangeState(PlayerState.KnockDownRise);
            }
        }            
    }
}
