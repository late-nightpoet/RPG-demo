using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;

public class BOSS_Controller : CharacterBase
{
    public Player_Controller targetPlayer;
    public NavMeshAgent navMeshAgent;
    public EnemyBlackBoard Ctx { get; private set; }
    public EnemyMovementHelper MovementHelper { get; private set; }
    #region 配置信息
    public float gravity = -9.8f;

    public float walkRange = 5;

    public float walkSpeed;

    public float runSpeed;

    //standAttackRange要设置为小于1.5，因为standattack1在大于1.5的距离进行攻击时，虽然看上去特效命中了player,但实际上碰撞体并不会命中player导致player不进入hurt状态感官奇怪
    public float standAttackRange = 1.5f;
    #endregion

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        Ctx = new EnemyBlackBoard
        {
            controller = characterController
        };
        MovementHelper = new EnemyMovementHelper(Ctx);
    }

    private void Start()
    {
        Init();
        ChangeState(BossState.Idle);
    }

    public void ChangeState(BossState bossState, bool reCurrstate = false)
    {
        switch (bossState)
        {
            case BossState.Idle:
                stateMachine.ChangeState<Boss_IdleState>(reCurrstate);
                break;
            case BossState.Walk:
                stateMachine.ChangeState<Boss_Walktate>(reCurrstate);
                break;
            case BossState.Run:
                stateMachine.ChangeState<Boss_RunState>(reCurrstate);
                break;
            case BossState.HitStagger:
                stateMachine.ChangeState<Boss_HitStaggerState>(reCurrstate);
                break;
            case BossState.KnockUp:
                stateMachine.ChangeState<Boss_KnockUpState>(reCurrstate);
                break;
            case BossState.KnockAirLoop:
                stateMachine.ChangeState<Boss_KnockAirLoopState>(reCurrstate);
                break;
            case BossState.KnockDownLand:
                stateMachine.ChangeState<Boss_KnockDownLandState>(reCurrstate);
                break;
            case BossState.KnockDownRise:
                stateMachine.ChangeState<Boss_KnockDownRiseState>(reCurrstate);
                break;
            case BossState.Attack:
                stateMachine.ChangeState<Boss_AttackState>(reCurrstate);
                break;
        }
    }

    public override bool Hurt(Skill_HitData hitData, ISkillOwner hurtSource)
    {
        SetHurtData(hitData, hurtSource);
        //todo boss可能处于霸体或者不可被击倒阶段
        //连击时可以从受伤状态到受伤状态
        if (hitData.IsKnockUp)
        {
            // 击飞路线
            ChangeState(BossState.KnockUp, true);
        }
        else
        {
            // 原地受击路线
            ChangeState(BossState.HitStagger, true);
        }
        return true;
    }

    #region UnityEditor 针对有多个碰撞体的角色进行标记

#if UNITY_EDITOR 
    [ContextMenu("SetHurtCollider")]
    private void SetHurtCollider()
    {
        // 设置所有的碰撞体为HurtColldier
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            // 排除武器
            if (colliders[i].GetComponent<Weapon_Controller>() == null)
            {
                colliders[i].gameObject.layer = LayerMask.NameToLayer("HurtCollider");
            }
        }
        // 标记场景修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
    #endregion
}
