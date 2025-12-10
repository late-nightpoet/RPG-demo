using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_LandState : PlayerStateBase
{
    private static readonly int LandSoftHash   = Animator.StringToHash("Base Layer.Landing.Land_Soft");
    private static readonly int LandHardHash   = Animator.StringToHash("Base Layer.Landing.Land_Hard");
    private static readonly int LandMediumHash = Animator.StringToHash("Base Layer.Landing.Land_Medium");
    private static readonly int LandDefaultHash = Animator.StringToHash("Base Layer.Landing.Land_Default");

    private static readonly int LandWalkHash = Animator.StringToHash("Base Layer.Landing.Land_Walk");

    private static readonly int LandRunHash = Animator.StringToHash("Base Layer.Landing.Land_Run");

    private static readonly int LandSprintHash = Animator.StringToHash("Base Layer.Landing.Land_Sprint");

    private static readonly int[] LandingHashes =
    {
        LandSoftHash,
        LandHardHash,
        LandMediumHash,
        LandDefaultHash,
        LandWalkHash,
        LandRunHash,
        LandSprintHash
    };

    float landAnimationProgress = 0f;
    public override void Enter()
    {
        Debug.Log("Enter Land State");
        //进入子状态机，不用playanimation
        //player.PlayAnimation("Base Layer.Landing.Land_Hard");
        player.MovementHelper.Sync();
       
    }

    public override void Update()
    {
       
        if (!player.Ctx.isLandingDone)
        {
             //这个函数不能放在enter里，因为animator状态机的切换需要一帧时间
            landAnimationProgress = getCurrrentAnimatorStateProgress();
            bool landingDoneParam = player.Model.Animator.GetBool("IsLandingDone");

            if (landAnimationProgress >= 0.95f || landingDoneParam)
            {
                player.Ctx.isLandingDone = true;
                player.Model.Animator.SetBool("IsLandingDone", true); // 使用一次后复位
            }
            else
            {
                return; // 动画还没播完，或参数未触发
            }
        }
        player.MovementHelper.CalculateInput();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();
        player.MovementHelper.CalculateMoveDirection();
        player.MovementHelper.FaceMoveDirection();
        player.MovementHelper.Sync();
        if (landAnimationProgress < 0.95f)
        {
            return; // 等待落地动画播放完毕
        } else {
            player.Ctx.isLandingDone = true;
        }
        //根据输入决定下一个状态
        if (!player.Ctx.isGrounded)
        {
            player.ChangeState(PlayerState.Fall);
            return;
        }
        else if (player.Ctx.SmoothedMoveInput.sqrMagnitude > 0.01f)
        {
            player.ChangeState(PlayerState.Locomotion);
            return;
        }
        else if (player.Ctx.isLandingDone && player.Ctx.SmoothedMoveInput.sqrMagnitude <= 0.01f)
        {
            Debug.Log("Exiting Land State to Idle State");
            player.ChangeState(PlayerState.Idle);
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("Exit Land State");
        player.Ctx.isLandingDone = false;
        player.Model.Animator.SetBool("IsLandingDone", false);
        player.MovementHelper.ResetFallingDuration();
        player.Ctx.velocity.x = 0f;
        player.Ctx.velocity.z = 0f;
        player.Ctx.velocity.y = 0f;
        player.Ctx.speed2D = 0f;
        player.MovementHelper.Sync();
    }

    private float getCurrrentAnimatorStateProgress()
    {
        if (player.Model.Animator.IsInTransition(0)) return 0f;
        var info = player.Model.Animator.GetCurrentAnimatorStateInfo(0);
        var hash = info.fullPathHash;

        for (int i = 0; i < LandingHashes.Length; i++)
        {
            if (hash == LandingHashes[i])
            return info.normalizedTime;
        }
        return 0f;
    }
}
