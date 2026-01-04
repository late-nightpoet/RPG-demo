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

    public override void Enter()
    {
        float dist = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        Debug.Log($"<color=yellow>进入攻击状态，此时距离 Player: {dist}</color>");
        CurrentAttackIndex = -1;
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
    }

    public override void Exit()
    {
        boss.Model.ClearRootMotionAction();
        RestoreUpperBodyLayers();
        boss.OnSkillOver();
    }

    public override void Update()
    {
        if (CheckAnimatorStateName(boss.standAttackConfigs[CurrentAttackIndex].AnimationName, out float aniamtionTime) && aniamtionTime>=1)
        {
            // 回到待机
            boss.ChangeState(BossState.Idle);
            return;
        }
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
