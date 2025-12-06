using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateBase : StateBase
{
    protected Player_Controller player;

    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        player = (Player_Controller)owner;
    }

    protected virtual bool CheckAnimatorStateName(string stateName,out float normalizedTime)
    {
        AnimatorStateInfo info = player.Model.Animator.GetCurrentAnimatorStateInfo(0);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
}
