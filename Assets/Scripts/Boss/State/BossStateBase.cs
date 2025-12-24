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
}
