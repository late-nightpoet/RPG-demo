using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_RunState : BossStateBase
{
    public override void Enter()
    {
        boss.PlayAnimation("Run");
        boss.navMeshAgent.enabled = true;
        boss.navMeshAgent.speed = boss.runSpeed;
        boss.Model.ClearRootMotionAction();
    }

    public override void Update()
    {
        float distance = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        if(distance <= boss.walkRange)
        {
            boss.ChangeState(BossState.Walk);
          
        }
        else
        {
            //每时每刻要根据玩家最新位置更新目的地
            boss.navMeshAgent.SetDestination(boss.targetPlayer.transform.position);
        }
    }

    public override void Exit()
    {
        boss.navMeshAgent.enabled = false;     
    }
}
