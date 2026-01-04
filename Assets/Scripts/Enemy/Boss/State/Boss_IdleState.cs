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
        float distance = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        // TODO:临时攻击检测
        if (Input.GetKeyDown(KeyCode.J))
        {
            boss.ChangeState(BossState.Attack);
            return;
        }
        if(distance < boss.walkRange)
        {
            boss.ChangeState(BossState.Walk);
            return;
        }
        else
        {
            boss.ChangeState(BossState.Run);
            return;
        }
    }
}
