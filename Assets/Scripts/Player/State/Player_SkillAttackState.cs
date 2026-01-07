using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_SkillAttackState : PlayerStateBase
{
    private SkillConfig skillConfig;
    

    public override void Enter()
    {
        Debug.Log("enter Player_SkillAttackState");
           player.Model.SetRootMotionAction((deltaPos, deltaRot) =>
        {
            // 叠加重力，避免悬空
            deltaPos.y += player.Ctx.velocity.y * Time.deltaTime;

            player.CharacterController.Move(deltaPos);
            player.transform.rotation *= deltaRot;
        });
        CacheAndDisableUpperBodyLayers();
    }

    public void InitData(SkillConfig skillConfig)
    {
        this.skillConfig = skillConfig;
        StartSkill();
    }

    private void StartSkill()
    {
        player.StartAttack(skillConfig);
    }

    public override void Exit()
    {
        Debug.Log("exit skillattackstate");
        player.Model.ClearRootMotionAction();
        RestoreUpperBodyLayers();
        player.OnSkillOver();
        skillConfig = null;
    }

    public override void Update()
    {
        player.MovementHelper.CalculateInput();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.ApplyGravity();
        if (CheckAnimatorStateName(skillConfig.AnimationName, out float aniamtionTime) && aniamtionTime>=1)
        {
            // 回到待机
            player.ChangeState(PlayerState.Idle);
            return;
        }
        //如果按下G键就切换到普攻
        if(CheckStandAttack())
        {
            player.ChangeState(PlayerState.StandAttack);
            return;
        }

        //检测有没有再次按下技能键播放其他技能
        if(player.CheckAndEnterSkillState())
        {
            return;
        }
        // 仅在后摇窗口允许取消
        if (player.CanSwitchSkill)
        {
            if (player.Ctx.isJumping)
            {
                player.ChangeState(PlayerState.Jump);
                return;
            }
            if (player.Ctx.rollRequested && player.Ctx.isGrounded)
            {
                player.Ctx.rollRequested = false;
                player.ChangeState(PlayerState.DodgeRoll);
                return;
            }
        }



        if (player.CurrentSkillConfig.ReleaseData.CanRotate)
        {
            player.MovementHelper.RotateDuringAttack(true);
        }
       
    }

    public bool CheckStandAttack()
    {
        return Input.GetKeyDown(KeyCode.G) && player.CanSwitchSkill;
    }

   
}
