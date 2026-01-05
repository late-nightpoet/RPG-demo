using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;

public class Player_DefenseState : PlayerStateBase
{
    private enum DefenseChildState
    {
        Enter,
        Hold,
        WaitCounterattack,
        Counterattack,
        Exit,
    }

    private DefenseChildState childState;

    private DefenseChildState ChildState
    {
        get => childState;
        set
        {
            childState = value;
            switch(childState)
            {
                case DefenseChildState.Enter:
                    player.PlayAnimation("EnterDefence");
                    break;
                case DefenseChildState.Hold:
                    player.PlayAnimation("DefenceLoop");
                    break;
                case DefenseChildState.WaitCounterattack:
                    waitCoroutineattackTimerCoroutine = MonoManager.Instance.StartCoroutine(WaitCounterAttackTimer());
                    break;
                case DefenseChildState.Counterattack:
                    player.StartAttack(player.counterAttackSkillConfig);
                    break;
                case DefenseChildState.Exit:
                    player.PlayAnimation("ExitDefence");
                    break;
            }
        }
    }

    public override void Enter()
    {
        //注册根运动
        player.Model.SetRootMotionAction(OnRootMotion);
        ChildState = DefenseChildState.Enter;
        CacheAndDisableUpperBodyLayers();
    }

    public override void Update()
    {
        switch(childState)
        {
            case DefenseChildState.Enter:
                if(CheckAnimatorStateName("EnterDefence", out float animationTime) && animationTime>= 1 )
                {
                    ChildState = DefenseChildState.Hold;
                    return;
                }
                break;
            case DefenseChildState.Hold:
                if(!Input.GetKey(KeyCode.B))//如果松开按键,则退出
                {
                    ChildState = DefenseChildState.Exit;
                }
                break;
            case DefenseChildState.WaitCounterattack:
                //如果按下攻击键，则检测到反击意图，进行反击
                if(Input.GetKey(KeyCode.G))
                {
                    MonoManager.Instance.StopCoroutine(waitCoroutineattackTimerCoroutine);
                    waitCoroutineattackTimerCoroutine = null;
                    ChildState = DefenseChildState.Counterattack;
                }
                else if(!Input.GetKey(KeyCode.B))//如果松开按键,则退出
                {
                    ChildState = DefenseChildState.Exit;
                }
                break;
            case DefenseChildState.Counterattack:
                //动画播放完毕检测
                if(CheckAnimatorStateName(player.counterAttackSkillConfig.AnimationName, out float attackAniamtionTime) && attackAniamtionTime >=1)
                {
                    //回到待机
                    player.ChangeState(PlayerState.Idle);
                }
                else if(player.CanSwitchSkill && Input.GetKey(KeyCode.G))
                {
                    player.ChangeState(PlayerState.Attack);
                }
                break;
            case DefenseChildState.Exit:
                if(CheckAnimatorStateName("ExitDefence", out float exitAniamtionTime) && exitAniamtionTime >= 1)
                {
                    //回到待机
                    player.ChangeState(PlayerState.Idle);
                    return;
                }
                break;
        }
    }

    //此函数用于外界调用，当player检测到在hold状态时player仍然被攻击就可以转为进入等待反击状态，相当于进入一个反击的窗口期
    //例如有的游戏在被攻到的前0.15-后0.15秒进行反击都算弹反成功不受伤害，WaitCounterattack状态就是弹反窗口
    public void Hurt()
    {
        if(childState == DefenseChildState.Hold)
        {
            ChildState = DefenseChildState.WaitCounterattack;
        }
    }

    private Coroutine waitCoroutineattackTimerCoroutine;

    private IEnumerator WaitCounterAttackTimer()
    {
        //在waitcounterattack等待waitCounterattackTime时长，如果超过这个时长没有反击就返回到hold状态
        yield return new WaitForSeconds(player.waitCounterattackTime);
        ChildState = DefenseChildState.Hold;
        waitCoroutineattackTimerCoroutine = null;
    }

    public override void Exit()
    {
        player.Model.ClearRootMotionAction();
        RestoreUpperBodyLayers();
    }

    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        deltaPosition.y = Physics.gravity.y * Time.deltaTime;
        player.CharacterController.Move(deltaPosition);
    }
}
