using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_IdleState : BossStateBase
{
    public override void Enter()
    {
        boss.PlayAnimation("Idle");
    }

    public override void Update()
    {
         boss.CharacterController.Move(new Vector3(0, boss.gravity * Time.deltaTime, 0));
        // TODO:临时攻击检测
        if (Input.GetKeyDown(KeyCode.J))
        {
            boss.ChangeState(BossState.Attack);
            return;
        }
    }
}
