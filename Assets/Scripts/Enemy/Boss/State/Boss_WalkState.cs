using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class Boss_Walktate : BossStateBase
{
    public override void Enter()
    {
        boss.PlayAnimation("Walk");
        boss.navMeshAgent.enabled = true;
        boss.navMeshAgent.speed = boss.walkSpeed;
        //boss.Model.SetRootMotionAction(OnRootMotion);
        boss.Model.ClearRootMotionAction();
    }

    public override void Update()
    {
        float distance = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        if(distance > boss.walkRange)
        {
            boss.ChangeState(BossState.Run);
          
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

      private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        deltaPosition.y = boss.gravity * Time.deltaTime;
        boss.CharacterController.Move(deltaPosition);
    }
}
