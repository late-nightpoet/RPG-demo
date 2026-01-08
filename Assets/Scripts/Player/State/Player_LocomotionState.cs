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
        player.MovementHelper.GroundedCheck();
        //检测到技能的输入
        if(player.CheckAndEnterSkillState())
        {
            return;
        }
        //检测攻击
        if(Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Exiting Idle State due to G key press");
            player.ChangeState(PlayerState.StandAttack);
            return;
        }
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
        //todo 解决player被攻击一次之后按F键不闪躲的bug
        if (player.Ctx.rollRequested && player.Ctx.isGrounded)
        {
            player.Ctx.rollRequested = false;
            player.ChangeState(PlayerState.DodgeRoll);
            return;
        }
        //检测格挡
        if(Input.GetKeyDown(KeyCode.B))
        {
            player.ChangeState(PlayerState.Defence);
            return;
        }
        bool hasMoveInput = player.Ctx.SmoothedMoveInput.magnitude > 0.1f;

        if (hasMoveInput)
        {
            player.MovementHelper.CalculateMoveDirection();
            player.Ctx.isStopped = false;
        }
        else
        {
            
            player.MovementHelper.DecelerateToStop();
            if (player.Ctx.speed2D <= 0.05f)
            {
                player.Ctx.isStopped = true;
                player.ChangeState(PlayerState.Idle);
                return;
            }
        }

        player.MovementHelper.FaceMoveDirection();
        player.MovementHelper.CalculateGait();
        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();
        player.MovementHelper.Sync();
    }
}
