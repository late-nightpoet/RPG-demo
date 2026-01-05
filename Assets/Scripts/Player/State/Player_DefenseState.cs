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
                case DefenseChildState.WaitCounterattack:
                    break;
                case DefenseChildState.Counterattack:
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
                    ChildState = DefenseChildState.WaitCounterattack;
                    return;
                }
                break;
            case DefenseChildState.WaitCounterattack:
                if(Input.GetKeyUp(KeyCode.B))//如果松开按键,则退出
                {
                    ChildState = DefenseChildState.Exit;
                }
                break;
            case DefenseChildState.Counterattack:
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
