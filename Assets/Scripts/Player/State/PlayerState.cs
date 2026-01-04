using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState 
{
    Idle,
    Locomotion,
    Jump,
    Fall,
    crouching,
    Land,
    DodgeRoll,

    StandAttack,
    HitStagger,
    KnockUp,
    KnockAirLoop,
    KnockDownLand,

    KnockDownRise,
    Attack,
}
