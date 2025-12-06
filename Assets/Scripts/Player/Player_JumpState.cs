using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_JumpState : PlayerStateBase
{
    public override void Enter()
    {
        //这两个赋值在movementhelp中已经做过了
        //player.Ctx.isJumping = true;
        //player.Ctx.isGrounded = false;
        player.Model.Animator.SetBool("isJumping", true);
        player.Ctx.velocity.y = player.Ctx.jumpForce;
        //playanimation没法进入子状态机，改为animator连线触发
        //player.PlayAnimation("Base Layer/Jump/Entry");
        
    }

    public override void Update()
    {

        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.CalculateMoveDirection();
        player.MovementHelper.FaceMoveDirection();
        player.MovementHelper.Sync();

        if (player.Ctx.velocity.y <= 0f)
        {
            player.ChangeState(PlayerState.Fall);
            return;
        }
    }

    public override void Exit()
    {
        player.Ctx.isJumping = false;
        player.Model.Animator.SetBool("isJumping", false);
        player.MovementHelper.Sync();
    }

}
