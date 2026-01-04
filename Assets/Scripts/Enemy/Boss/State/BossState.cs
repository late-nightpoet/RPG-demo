using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossState
{
    Idle,
    Walk,
    Run,
    Hurt,

    HitStagger,
    KnockUp,
    KnockAirLoop,
    KnockDownLand,

    KnockDownRise,
    Attack,
}
