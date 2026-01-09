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

    // 攻击状态总计时
    private float currentAttackTime = 0;
    // 当前正在播放的技能配置
    private SkillConfig currentSkillConfig;

    public override void Enter()
    {
        float dist = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        Debug.Log($"<color=yellow>进入攻击状态，此时距离 Player: {dist}</color>");
        CurrentAttackIndex = -1;
        // 【关键步骤 1】重置标记
        hasAnimStarted = false;
        currentAttackTime = 0;
        currentSkillConfig = null;
           boss.Model.SetRootMotionAction((deltaPos, deltaRot) =>
        {
            // 使用BOSS_Controller中定义的重力
            deltaPos.y = boss.gravity * Time.deltaTime;
            boss.CharacterController.Move(deltaPos);
            boss.transform.rotation *= deltaRot;
        });
        CacheAndDisableUpperBodyLayers();
       
        // 进入状态后，立即开始一次普通攻击
        StandAttack();
    }

    private void StandAttack()
    {
        CurrentAttackIndex += 1; // 只在确认连击时递增
        
        // 修正LookAt，防止Boss倾斜
        Vector3 pos = boss.targetPlayer.transform.position;
        boss.transform.LookAt(new Vector3(pos.x, boss.transform.position.y, pos.z));

        // 记录当前攻击并开始
        currentSkillConfig = boss.standAttackConfigs[CurrentAttackIndex];
        boss.StartAttack(currentSkillConfig);

        // 重置动画开始标记
        hasAnimStarted = false;
    }

    private void StartSkill(int index)
    {
        Vector3 pos = boss.targetPlayer.transform.position;
        boss.transform.LookAt(new Vector3(pos.x, boss.transform.position.y, pos.z));
        currentSkillConfig = boss.skillInfoList[index].skillConfig;
        boss.StartSkill(index);

        // 重置动画开始标记
        hasAnimStarted = false;
    }

    public override void Exit()
    {
        boss.Model.ClearRootMotionAction();
        RestoreUpperBodyLayers();
        boss.OnSkillOver();
        currentSkillConfig = null;
    }

    public override void Update()
    {
        currentAttackTime += Time.deltaTime;

        // --- 动画状态检查 (保留了hasAnimStarted逻辑) ---
        if (currentSkillConfig != null)
        {
            bool isNameMatch = CheckAnimatorStateName(currentSkillConfig.AnimationName, out float animationTime);
            
            if (boss.Model.Animator.IsInTransition(0)) return;

            if (!hasAnimStarted)
            {
                if (isNameMatch && animationTime < 0.6f)
                {
                    hasAnimStarted = true;
                }
                else
                {
                    // 数据无效，等待下一帧
                    return;
                }
            }

            // 动画播放完毕的后备检查
            if (isNameMatch && animationTime >= 1f)
            {
                // 切换到Walk状态以重新决策
                boss.ChangeState(BossState.Walk);
                return;
            }
        }

        // --- 攻击决策AI (来自参考代码) ---
        if (boss.CanSwitchSkill)
        {
            float distance = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
            if (distance <= boss.standAttackRange && currentAttackTime < boss.attackTime)
            {
                // 最高优先级: 破防技能
                if (boss.targetPlayer.isDefence)
                {
                    for (int i = 0; i < boss.skillInfoList.Count; i++)
                    {
                        if (boss.skillInfoList[i].remainCdTime <= 0 && boss.skillInfoList[i].skillConfig.AttackData.Length > 0)
                        {
                            if (boss.skillInfoList[i].skillConfig.AttackData[0].HitData.Break)
                            {
                                Debug.Log($"[Boss AI] 决策：玩家正在防御，使用破防技能: {boss.skillInfoList[i].skillConfig.AnimationName}");
                                StartSkill(i);
                                return;
                            }
                        }
                    }
                }

                // 次高优先级: 其他冷却完毕的技能
                for (int i = 0; i < boss.skillInfoList.Count; i++)
                {
                    if (boss.skillInfoList[i].remainCdTime <= 0)
                    {
                        Debug.Log($"[Boss AI] 决策：使用冷却完毕的技能: {boss.skillInfoList[i].skillConfig.AnimationName}");
                        StartSkill(i);
                        return;
                    }
                }

                // 最低优先级: 普通攻击
                int nextAttackIndex = (CurrentAttackIndex + 1) % boss.standAttackConfigs.Length;
                Debug.Log($"[Boss AI] 决策：无可用技能，进行普通攻击: {boss.standAttackConfigs[nextAttackIndex].AnimationName}");
                StandAttack();
            }
            else
            {
                // 超出攻击范围或超时，返回Walk状态
                Debug.Log($"[Boss AI] 决策：超出范围 (距离: {distance:F2}) 或超时 (时间: {currentAttackTime:F2})，切换到Walk状态");
                boss.ChangeState(BossState.Walk);
            }
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
