using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossState
{
    Idle,
    Hurt,

    HitStagger,
    KnockUp,
    KnockAirLoop,
    KnockDownLand,

    KnockDownRise,
}
