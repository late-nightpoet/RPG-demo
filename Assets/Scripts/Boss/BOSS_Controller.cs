using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BOSS_Controller : MonoBehaviour, IHurt, IStateMachineOwner
{
    [SerializeField] private Boss_Model boss_Model;
    public Boss_Model Model { get => boss_Model;}

    private StateMachine stateMachine;

    private void Start()
    {
        stateMachine = new StateMachine();
        stateMachine.Init(this);
        ChangeState(BossState.Idle);
    }

    public void ChangeState(BossState bossState)
    {
        switch (bossState)
        {
            case BossState.Idle:
                stateMachine.ChangeState<Boss_IdleState>();
                break;
        }
    }
    public void Hurt()
    {
        Debug.Log("BOSS受到攻击！");
    }

    public void PlayAnimation(string animationName, float fixedTransitionDuration = 0.25f)
    {
        boss_Model.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration);
    }
}
