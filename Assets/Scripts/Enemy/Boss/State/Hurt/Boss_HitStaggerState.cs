using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_HitStaggerState : Boss_HurtStateBase
{
    protected const string AnimHitStaggerFront = "Hurt_Front";

    protected const string AnimHitStaggerBack = "Hurt_Back";

    protected const string AnimHitStaggerLeft = "Hurt_Left";

    protected const string AnimHitStaggerRight = "Hurt_Right";

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
        base.Enter();
        // 【新增】重置停顿标记
        isHitStopFrozen = false;

        boss.PlayAnimation(currentAnimName);
    }

        // 【核心逻辑】处理先播放、再停顿、再恢复
    private void HandleHitStaggerLogic()
    {
        if (boss.Model == null || boss.Model.Animator == null) return;

        Animator animator = boss.Model.Animator;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 阶段 1: 还没停顿，正在播放前摇
        if (!isHitStopFrozen)
        {
            // 只有当动画真正开始播放(IsName)，且播放进度超过了设定点(FreezePoint)时，才冻结
            if (stateInfo.IsName(currentAnimName) && stateInfo.normalizedTime >= HitStaggerFreezePoint)
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
                    boss.ChangeState(BossState.Idle);
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
}
