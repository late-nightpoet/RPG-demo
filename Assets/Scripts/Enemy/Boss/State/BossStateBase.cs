using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossStateBase : StateBase
{
    protected BOSS_Controller boss;
    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        boss = (BOSS_Controller)owner;
    }

    protected virtual bool CheckAnimatorStateName(string stateName,out float normalizedTime)
    {
        AnimatorStateInfo info = boss.Model.Animator.GetCurrentAnimatorStateInfo(0);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
}
