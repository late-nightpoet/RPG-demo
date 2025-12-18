using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_StandAttackState : PlayerStateBase
{
    private const int ARM_LAYER_INDEX = 1;
    private const int HAND_LAYER_INDEX = 2;

    private float _cachedArmLayerWeight;
    private float _cachedHandLayerWeight;

    public override void Enter()
    {
        CacheAndDisableUpperBodyLayers();
       
        // 播放技能
        StandAttack();
    }

    private void StandAttack()
    {
        //todo 实现连续普攻
        player.StartAttack(player.standAttackConfig[0]);
    }

    public override void Exit()
    {
        RestoreUpperBodyLayers();
    }

    public override void Update()
    {
        player.MovementHelper.CalculateInput();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.ApplyGravity();
        if (CheckAnimatorStateName(player.testSkillConfig.AnimationName, out float aniamtionTime) && aniamtionTime>=1)
        {
            // 回到待机
            player.ChangeState(PlayerState.Idle);
        }
    }

    private void CacheAndDisableUpperBodyLayers()
    {
        var animator = player.Model.Animator;
        if (animator == null) return;

        _cachedArmLayerWeight = GetLayerWeightSafe(animator, ARM_LAYER_INDEX);
        _cachedHandLayerWeight = GetLayerWeightSafe(animator, HAND_LAYER_INDEX);

        SetLayerWeightSafe(animator, ARM_LAYER_INDEX, 0f);
        SetLayerWeightSafe(animator, HAND_LAYER_INDEX, 0f);
    }

    private void RestoreUpperBodyLayers()
    {
        var animator = player.Model.Animator;
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
