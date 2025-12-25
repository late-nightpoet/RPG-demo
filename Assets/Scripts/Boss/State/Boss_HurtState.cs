using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_HurtState : BossStateBase
{
    public override void Enter()
    {
        boss.PlayAnimation("Hurt");
    }
}
