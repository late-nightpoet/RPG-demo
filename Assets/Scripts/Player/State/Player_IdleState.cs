using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_IdleState : PlayerStateBase
{
   public override void Enter()
    {
        Debug.Log("Enter Idle State");
        player.Ctx.isStopped = true;
        if (player.Model.Animator != null) player.Model.Animator.applyRootMotion = false;
        //播放角色待机动画
        player.PlayAnimation("Idle");
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
        //从locomotion状态切换到idle状态时，先减速到停止再响应跳跃和移动输入
        if (player.Ctx.isStopped)
        {
            player.MovementHelper.DecelerateToStop();
            player.MovementHelper.Sync();
            if (player.Ctx.speed2D <= 0.01f)
                player.Ctx.isStopped = false;
        }
        if(Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Exiting Idle State due to G key press");
            player.ChangeState(PlayerState.StandAttack);
            return;
        }
         if (player.Ctx.isJumping)
        {
            Debug.Log("Exiting Idle State due to jump input");
            player.ChangeState(PlayerState.Jump);
            return;
        }
        if (player.Ctx.rollRequested)
        {
            player.Ctx.rollRequested = false;
            player.ChangeState(PlayerState.DodgeRoll);
            return;
        }
        if(!player.Ctx.isGrounded)
        {
            //player.MovementHelper.ApplyGravity();
            //player.MovementHelper.Move();
            //切换到falling状态
            player.ChangeState(PlayerState.Fall);
            return;
        }
        //检测格挡
        if(Input.GetKeyDown(KeyCode.B))
        {
            player.ChangeState(PlayerState.Defence);
            return;
        }
        //检测玩家移动输入
        if(player.Ctx.SmoothedMoveInput.magnitude > 0.1f)
        {
            Debug.Log("Exiting Idle State due to movement input");
            player.Ctx.isStopped = false;
            //切换到移动状态
            player.ChangeState(PlayerState.Locomotion);
            return;
        }
        player.MovementHelper.Sync();
    }
}
