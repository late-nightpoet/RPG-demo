using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_IdleState : BossStateBase
{
    public override void Enter()
    {
        boss.PlayAnimation("Idle");
    }
}
