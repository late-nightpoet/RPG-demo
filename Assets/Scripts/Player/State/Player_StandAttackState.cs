using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_StandAttackState : PlayerStateBase
{
    //当前处于第几次攻击
    private int currentAttackIndex;

    private int CurrentAttackIndex
    {
        get => currentAttackIndex;
        set
        {
            if(value >= player.standAttackConfigs.Length) currentAttackIndex = 0;
            else currentAttackIndex = value;
        }
    }
    private const int ARM_LAYER_INDEX = 1;
    private const int HAND_LAYER_INDEX = 2;

    private float _cachedArmLayerWeight;
    private float _cachedHandLayerWeight;

    public override void Enter()
    {
        CurrentAttackIndex = 0;
           player.Model.SetRootMotionAction((deltaPos, deltaRot) =>
        {
            // 叠加重力，避免悬空
            deltaPos.y += player.Ctx.velocity.y * Time.deltaTime;

            player.CharacterController.Move(deltaPos);
            player.transform.rotation *= deltaRot;
        });
        CacheAndDisableUpperBodyLayers();
       
        // 播放技能
        StandAttack();
    }

    private void StandAttack()
    {
        //todo 实现连续普攻
        Debug.Log("CurrentAttackIndex is " + CurrentAttackIndex);
        player.StartAttack(player.standAttackConfigs[CurrentAttackIndex]);
    }

    public override void Exit()
    {
        player.Model.ClearRootMotionAction();
        RestoreUpperBodyLayers();
        player.OnSkillOver();
    }

    public override void Update()
    {
        player.MovementHelper.CalculateInput();
        player.MovementHelper.GroundedCheck();
        player.MovementHelper.ApplyGravity();
         if(CheckStandAttack())
        {
            Debug.Log("CurrentAttackIndex += 1;");
            CurrentAttackIndex += 1; // 只在确认连击时递增
            StandAttack();
        }
        if (CheckAnimatorStateName(player.standAttackConfigs[CurrentAttackIndex].AnimationName, out float aniamtionTime) && aniamtionTime>=1)
        {
            // 回到待机
            player.ChangeState(PlayerState.Idle);
            return;
        }
       
    }

    public bool CheckStandAttack()
    {
        return Input.GetKeyDown(KeyCode.G) && player.CanSwitchSkill;
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
