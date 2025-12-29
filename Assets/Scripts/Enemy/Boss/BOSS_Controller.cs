using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BOSS_Controller : MonoBehaviour, IHurt, IStateMachineOwner,ISkillOwner
{
    [SerializeField] private Boss_Model boss_Model;
    public Boss_Model Model { get => boss_Model;}

    [SerializeField]private CharacterController characterController;

    public CharacterController CharacterController { get => characterController;}

    public Transform ModelTransform => Model.transform;

    private StateMachine stateMachine;

    public List<string> enemeyTagList;

    public EnemyBlackBoard Ctx { get; private set; }
    public EnemyMovementHelper MovementHelper { get; private set; }
    #region 配置信息
    public float gravity = -9.8f;
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
        boss_Model.Init(null,this, enemeyTagList);
        stateMachine = new StateMachine();
        stateMachine.Init(this);
        ChangeState(BossState.Idle);
    }

    public void ChangeState(BossState bossState, bool reCurrstate = false)
    {
        switch (bossState)
        {
            case BossState.Idle:
                stateMachine.ChangeState<Boss_IdleState>(reCurrstate);
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
        }
    }

    public Skill_HitData hitData {get; private set;}
    public ISkillOwner hurtSource {get; private set;}
    public void Hurt(Skill_HitData hitData, ISkillOwner hurtSource)
    {
        //todo boss可能处于霸体或者不可被击倒阶段
        //连击时可以从受伤状态到受伤状态
        this.hitData = hitData;
        this.hurtSource = hurtSource;
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
    }

    public void PlayAnimation(string animationName, float fixedTransitionDuration = 0.25f)
    {
        boss_Model.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration);
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

    public void StartSkillHit(int weaponIndex)
    {
        throw new NotImplementedException();
    }

    public void StopSkillHit(int weaponIndex)
    {
        throw new NotImplementedException();
    }

    public void SkillCanSwitch()
    {
        throw new NotImplementedException();
    }

    public void OnHit(IHurt target, Vector3 hitPostion)
    {
        throw new NotImplementedException();
    }

    public void OnFootStep()
    {
        throw new NotImplementedException();
    }


}
