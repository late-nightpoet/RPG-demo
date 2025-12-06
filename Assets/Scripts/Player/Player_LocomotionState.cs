using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_LocomotionState : PlayerStateBase
{
    public override void Enter()
    {
        Debug.Log("Enter Locomotion State");
        if (player.Model.Animator != null) player.Model.Animator.applyRootMotion = false;
        player.PlayAnimation("Locomotion");
    }

    public override void Exit()
    {
        // player.Ctx.velocity.x = 0f;
        // player.Ctx.velocity.z = 0f;
        // player.Ctx.speed2D = 0f;
        player.Ctx.isStopped = true;
        player.MovementHelper.Sync();
    }

    public override void Update()
    {
        
        player.MovementHelper.CalculateInput();
        player.MovementHelper.CalculateMoveDirection();
        player.MovementHelper.FaceMoveDirection();
        player.MovementHelper.CalculateGait();
        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();
        player.MovementHelper.Sync();
        if (player.Ctx.isJumping)
        {
            player.ChangeState(PlayerState.Jump);
            return;
        }
        if (!player.Ctx.isGrounded)
        {
            player.ChangeState(PlayerState.Fall);
            return;
        }
        if(player.Ctx.MoveInput.magnitude < 0.1f)
        {
            Debug.Log("Exiting Locomotion State due to no movement input");
            player.ChangeState(PlayerState.Idle);
            return;
        }
    }
}
