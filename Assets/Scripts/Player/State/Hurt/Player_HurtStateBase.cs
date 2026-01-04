using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_HurtStateBase : PlayerStateBase
{

    protected Skill_HitData hitData => player.hitData;
    protected ISkillOwner source => player.hurtSource;
     // 用于记录胶囊体原始数值以便恢复
    protected float defaultHeight =1.5f;
    protected Vector3 defaultCenter = new Vector3(0,0.75f,0);
    protected float defaultStepOffset;
    protected float defaultRadius = 0.25f; // 建议也记录半径

    // 【新增】定义空中/倒地时的胶囊体参数
    // 建议高度设为原高度的一半或更小，模拟蜷缩状态
    protected const float AerialHeight = 0.5f; 
    protected readonly Vector3 AerialCenter = new Vector3(0, 0.26f, 0);

    protected Vector3 horizontalVelocity = Vector3.zero; 



    public override void Enter()
    {
   
        horizontalVelocity = Vector3.zero;

        //禁止rootmotion
        player.Model.ClearRootMotionAction();

        //Animator.applyRootMotion 是一个持久的状态。除非显式修改它，否则它会一直保持上一次设置的值。
        if (player.Model != null && player.Model.Animator != null)
        player.Model.Animator.applyRootMotion = false;
    }

    protected void UpdateMovement(bool allowHorizontal)
    {
        if (player.Ctx == null || player.MovementHelper == null)
        {
            return;
        }

        if (!allowHorizontal)
        {
            player.Ctx.velocity.x = 0f;
            player.Ctx.velocity.z = 0f;
        }
        else
        {
            player.Ctx.velocity.x = horizontalVelocity.x;
            player.Ctx.velocity.z = horizontalVelocity.z;
        }

        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();
        player.MovementHelper.GroundedCheck();
    }

    /// <summary>
    /// 压缩胶囊体 (用于击飞)
    /// </summary>
    protected void ShrinkCapsule()
    {
        //【核心修改】如果是击飞，立即压缩胶囊体！
        //不压缩胶囊体的话会导致胶囊体在空中还是竖直状态导致模型还在播放动画但是胶囊体过早触碰地面，从而有角色在空中就进入knockdownland的bug
        if (player.CharacterController != null)
        {
            // 必须先修改 Step Offset！防止因为 Offset 过大导致高度修改被拒绝
            player.CharacterController.stepOffset = 0.1f; 
            
            // 建议：稍微减小半径，或者让高度稍微大于 2*Radius
            // 这里为了安全，我们把半径设为 0.2，高度 0.5，这样就是个瘦长的胶囊，肯定合法
            player.CharacterController.radius = 0.2f;
            player.CharacterController.height = AerialHeight;
            player.CharacterController.center = AerialCenter;
        }
    }

    // 恢复胶囊体 (用于起身/退出)
    protected void RestoreCapsule()
    {
        if (player.CharacterController != null)
        {
            float targetHeight = defaultHeight > 0.1f ? defaultHeight : 1.5f; 

            
            // 恢复顺序：先恢复高度和半径，最后恢复 Step Offset
            // (其实恢复顺序不那么严格，但是个好习惯)
            player.CharacterController.radius = defaultRadius > 0 ? defaultRadius : 0.25f;
            player.CharacterController.height = targetHeight;
            player.CharacterController.center = defaultCenter;
            
            // 最后恢复 Step Offset，因为现在高度已经变高了，0.3 是合法的
            player.CharacterController.stepOffset = defaultStepOffset > 0 ? defaultStepOffset : 0.3f;
        }
    }

    public override void Exit()
    {
        base.Exit();
        player.Model.ClearRootMotionAction();
        if (player.Ctx != null)
        {
            player.Ctx.velocity = Vector3.zero;
        }
    }

    protected bool IsGrounded()
    {
        return player.CharacterController != null && player.CharacterController.isGrounded;
    }
   
}
