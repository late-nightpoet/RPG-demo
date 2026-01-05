using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_HitStaggerState : Player_HurtStateBase
{
    //攻击来自后方，角色向前方倾倒的动画
    protected const string AnimHitStaggerFront = "HitStagger_Front";

    protected const string AnimHitStaggerBack = "HitStagger_Back";

   
    protected const string AnimHitStaggerLeft = "HitStagger_Left";

    //攻击来自角色的左方，角色向右倾倒，即攻击者在角色的左方

    protected const string AnimHitStaggerRight = "HitStagger_Right";

    private string currentAnimName = AnimHitStaggerFront;

     // 【新增配置】受击动画的“停顿点” (0.0 ~ 1.0)
    // 0.1 表示动画播放到 10% 的时候停顿。建议设为受击动作幅度最大的那一帧。
    private const float HitStaggerFreezePoint = 0.15f; 

    // 【新增标记】是否处于卡肉停顿中
    private bool isHitStopFrozen = false;

    private float currHardTime = 0;

    public override void Enter()
    {
        currHardTime = 0;
        // 【新增】重置停顿标记
        isHitStopFrozen = false;
        //  判断受击方向，播放对应动画

        // 2. 【关键步骤 A】告诉 Animator：我要用 Root Motion
        if (player.Model.Animator != null)
        {
            player.Model.Animator.applyRootMotion = true;
            player.Model.Animator.speed = 1f;
        }

        // 3. 【关键步骤 B】注册“传输装置”
        // 也就是告诉 Model：当 Animator 产生位移时，请把这个位移传给 CharacterController
        player.Model.SetRootMotionAction((deltaPos, deltaRot) =>
        {
            // 你可以选择是否保留 Y 轴位移（如果有跳跃动作则需要保留）
            // deltaPos.y = 0; 
            
            // 驱动角色移动
            player.CharacterController.Move(deltaPos);
            
            // 驱动角色旋转（如果动画里有旋转）
            player.transform.rotation *= deltaRot;
        });

        currentAnimName = GetDirectionalAnimation();
        player.PlayAnimation(currentAnimName, 0.25f);
    }

        // 【核心逻辑】处理先播放、再停顿、再恢复
    private void HandleHitStaggerLogic()
    {
        if (player.Model == null || player.Model.Animator == null) return;

        Animator animator = player.Model.Animator;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 【新增 1】如果处于过渡状态（CrossFade 正在发生），数据是不准的，直接跳过这一帧
        if (animator.IsInTransition(0)) return;

        // 阶段 1: 还没停顿，正在播放前摇
        if (!isHitStopFrozen)
        {
            // 只有当动画真正开始播放(IsName)，且播放进度超过了设定点(FreezePoint)时，才冻结
            if (stateInfo.IsName(currentAnimName) && stateInfo.normalizedTime >= HitStaggerFreezePoint && stateInfo.normalizedTime < 0.6f)
            {
                // 进入停顿阶段
                animator.speed = 0f; // 冻结动画
                isHitStopFrozen = true;
                currHardTime = 0f;   // 开始计时硬直
            }
        }
        // 阶段 2: 处于停顿中 (HardTime 计时)
        else
        {
            currHardTime += Time.deltaTime;

            // 硬直时间结束
            if (currHardTime >= hitData.HardTime)
            {
                // 阶段 3: 恢复播放
                animator.speed = 1f;

                // 检查动画是否已经彻底播完 (比如 > 0.99 或者 > 1)
                // 注意：由于之前被冻结在 0.15，现在恢复后需要等它播完剩下部分
                if (stateInfo.normalizedTime >= 1f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
    }


    public override void Update()
    {
        // 保持静止，只受重力影响
        UpdateMovement(false);
        
        // 处理卡肉逻辑
        HandleHitStaggerLogic();
    }

    private string GetDirectionalAnimation()
    {
        if (source == null) return AnimHitStaggerFront;

        // 获取指向攻击者的向量
        Vector3 dirToAttacker = (source.ModelTransform.position - player.transform.position).normalized;
        // 转换到 Boss 的局部坐标系
        Vector3 localDir = player.transform.InverseTransformDirection(dirToAttacker);

        // 判定逻辑：
        // localDir.z > 0 (前方), < 0 (后方)
        // localDir.x > 0 (右方), < 0 (左方)
        // 比较绝对值大小来决定主要是前后还是左右

        if (Mathf.Abs(localDir.z) > Mathf.Abs(localDir.x))
        {
            // 前后为主
            return localDir.z > 0 ? AnimHitStaggerBack : AnimHitStaggerFront;
        }
        else
        {
            // 左右为主
            return localDir.x > 0 ? AnimHitStaggerLeft : AnimHitStaggerRight;
        }
    }

    public override void Exit()
    {
        // 【关键步骤 C】打扫战场
        // 退出状态时，必须取消注册，否则切到 Idle 状态后，Idle 动画原本只有微小的抖动，
        // 也会被强行应用到 CharacterController 上，导致角色奇怪地滑步。
        player.Model.ClearRootMotionAction();
        
        if (player.Model.Animator != null)
        {
            player.Model.Animator.applyRootMotion = false;
            player.Model.Animator.speed = 1f;
        }
        
    }
}
