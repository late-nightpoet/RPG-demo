using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_AttackState : BossStateBase
{
    //当前处于第几次攻击
    private int currentAttackIndex;

    private int CurrentAttackIndex
    {
        get => currentAttackIndex;
        set
        {
            if(value >= boss.standAttackConfigs.Length) currentAttackIndex = 0;
            else currentAttackIndex = value;
        }
    }
    private const int ARM_LAYER_INDEX = 1;
    private const int HAND_LAYER_INDEX = 2;

    private float _cachedArmLayerWeight;
    private float _cachedHandLayerWeight;

    // 【新增变量】用于标记新动画是否真正开始了
    private bool hasAnimStarted = false;

    public override void Enter()
    {
        float dist = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        Debug.Log($"<color=yellow>进入攻击状态，此时距离 Player: {dist}</color>");
        CurrentAttackIndex = -1;
        // 【关键步骤 1】重置标记
        hasAnimStarted = false;
           boss.Model.SetRootMotionAction((deltaPos, deltaRot) =>
        {
            // 叠加重力，避免悬空
            deltaPos.y += boss.Ctx.velocity.y * Time.deltaTime;

            boss.CharacterController.Move(deltaPos);
            boss.transform.rotation *= deltaRot;
        });
        CacheAndDisableUpperBodyLayers();
       
        // 播放技能
        StandAttack();
    }

    private void StandAttack()
    {
        //todo 实现连续普攻
        Debug.Log("CurrentAttackIndex is " + CurrentAttackIndex);
        CurrentAttackIndex += 1; // 只在确认连击时递增
        boss.transform.LookAt(boss.targetPlayer.transform);
        boss.StartAttack(boss.standAttackConfigs[CurrentAttackIndex]);

        // 注意：StandAttack 内部会增加 Index 并播放新动画
            // 调用完后，hasAnimStarted 应该再次被设为 false 吗？
            // 实际上，因为这是同一个 State 内部切换动作，Enter 不会被重新调用。
            // 所以我们需要在这里手动重置标记！
        hasAnimStarted = false;
    }

    public override void Exit()
    {
        boss.Model.ClearRootMotionAction();
        RestoreUpperBodyLayers();
        boss.OnSkillOver();
    }

    public override void Update()
    {
        // 1. 获取动画状态
        // 注意：这里我们先不把 time 拿来做判断，先检查是否处于过渡
        bool isNameMatch = CheckAnimatorStateName(boss.standAttackConfigs[CurrentAttackIndex].AnimationName, out float animationTime);
        
        // 【关键步骤 2】过渡期保护
        // 如果 Animator 正在混合（Transition），数据是不准的，直接 return 等待混合结束
        if (boss.Model.Animator.IsInTransition(0)) return;

        // 【关键步骤 3】启动检测（幽灵数据过滤器）
        if (!hasAnimStarted)
        {
            // 只有当名字匹配，且进度“归零”（小于 0.1）时，才认为新动画开始了
            // 否则，如果读到 1.0，说明还是旧数据，本帧忽略
            if (isNameMatch && animationTime < 0.1f)
            {
                hasAnimStarted = true;
            }
            else
            {
                // 数据无效，等待下一帧
                return;
            }
        }

        // --- 代码走到这里，说明 animationTime 是可信的新数据 ---
        // 【关键步骤 4】正常的退出逻辑
        if (isNameMatch && animationTime >= 1f)
        {
            // 回到待机
            boss.ChangeState(BossState.Idle);
            return;
        }

        // 连招逻辑
        float distance = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        if(distance <= boss.standAttackRange && boss.CanSwitchSkill)
        {
            StandAttack();

            
            return;
        }

    }


    private void CacheAndDisableUpperBodyLayers()
    {
        var animator = boss.Model.Animator;
        if (animator == null) return;

        _cachedArmLayerWeight = GetLayerWeightSafe(animator, ARM_LAYER_INDEX);
        _cachedHandLayerWeight = GetLayerWeightSafe(animator, HAND_LAYER_INDEX);

        SetLayerWeightSafe(animator, ARM_LAYER_INDEX, 0f);
        SetLayerWeightSafe(animator, HAND_LAYER_INDEX, 0f);
    }

    private void RestoreUpperBodyLayers()
    {
        var animator = boss.Model.Animator;
        if (animator == null) return;

        SetLayerWeightSafe(animator, ARM_LAYER_INDEX, _cachedArmLayerWeight);
        SetLayerWeightSafe(animator, HAND_LAYER_INDEX, _cachedHandLayerWeight);
    }

    private static float GetLayerWeightSafe(Animator animator, int index)
    {
        return animator.layerCount > index ? animator.GetLayerWeight(index) : 0f;
    }

    private static void SetLayerWeightSafe(Animator animator, int index, float weight)
    {
        if (animator.layerCount > index)
        {
            animator.SetLayerWeight(index, weight);
        }
    }
}
