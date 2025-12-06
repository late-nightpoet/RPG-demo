using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_FallState : PlayerStateBase
{
    public override void Enter()
    {
        Debug.Log("Enter Fall State");
        player.Ctx.isGrounded = false;
        player.Model.Animator.SetBool("IsGrounded", false);
        player.MovementHelper.ResetFallingDuration();
        player.Ctx.velocity.y = 0f;
        //播放角色下落动画
        player.PlayAnimation("Fall");
    }

    public override void Update()
    {
        player.MovementHelper.CalculateInput();
        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.Sync();
        //fall状态不响应跳跃输入
        if (player.Ctx.isGrounded)
        {
            player.Model.Animator.SetBool("IsGrounded", true);
           // player.Ctx.velocity.y = -2f; // 小负值贴地
            //切换到落地状态
            player.ChangeState(PlayerState.Land);
            return;
        }
        player.MovementHelper.UpdateFallingDuration();
        
    }
}
