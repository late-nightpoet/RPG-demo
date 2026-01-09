using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
 
// 提示: 类名已从 "Boss_Walktate" 更正为 "Boss_WalkState"。请确保文件名也为 "Boss_WalkState.cs"。
public class Boss_WalkState : BossStateBase
{
    //是否处于对峙、警戒状态
    private bool isVigilant;
    public override void Enter()
    {
        boss.Model.ClearRootMotionAction();
        boss.PlayAnimation("Walk");
        boss.navMeshAgent.enabled = true;
        boss.navMeshAgent.updateRotation = true; // [修正] 默认开启旋转，防止从其他状态继承错误的设置。
        if(boss.anger) isVigilant = false;
        else isVigilant  = Random.Range(0,3) >= 1; // 2/3的概率拉近后是进行对峙而不是继续超玩家前进
        if(isVigilant)
        {
            boss.navMeshAgent.updateRotation = true;
            boss.navMeshAgent.speed = boss.vigilantSpeed;
            stopVigilantCoroutine = MonoManager.Instance.StartCoroutine(StopVigilance());
        }
        else
        {
            boss.navMeshAgent.speed = boss.walkSpeed;
        }

    }

    Coroutine stopVigilantCoroutine;

    IEnumerator StopVigilance()
    {
        //随机警惕时间，更灵动
        yield return new WaitForSeconds(Random.Range(0, boss.vigilantTime));
        isVigilant = false;
        // [修正] 此处是导致“背对奔跑”问题的根源。对峙结束后，Boss应恢复正常追踪，必须允许旋转，故删除此行。
        stopVigilantCoroutine = null;
    }

    public override void Update()
    {
        float distance = Vector3.Distance(boss.transform.position, boss.targetPlayer.transform.position);
        if(distance > boss.walkRange)
        {
            boss.ChangeState(BossState.Run);
            return;
        }
        if(isVigilant) // 朝向玩家，但是保持一个距离
        {
            ScreenLogger.Show("Boss State: isVigilant is " + isVigilant);
            Vector3 playerPos = boss.targetPlayer.transform.position;
            //boss.transform.LookAt(new Vector3(playerPos.x, boss.transform.position.y, playerPos.z));
            // [修正] 目标点计算应使用 vigilantRange (距离) 而非 vigilantSpeed (速度)。
            Vector3 targetPos = (boss.transform.position - playerPos).normalized * boss.vigilantRange + playerPos;
            if(Vector3.Distance(targetPos, boss.transform.position) < 0.5f)
            {
                ScreenLogger.Show("Boss State: isVigilant Idle ");
                boss.PlayAnimation("Idle",false);
                boss.navMeshAgent.isStopped = true; // [修正] 停止寻路，防止播放Idle动画时还在滑动。
            }
            else
            {
                ScreenLogger.Show("Boss State: isVigilant walk ");
                boss.navMeshAgent.isStopped = false; // [修正] 恢复寻路。
                boss.PlayAnimation("Walk",false); // [修正] 动画名 "Walk" 大小写。
                boss.navMeshAgent.SetDestination(targetPos);
            }
        }
        else
        {
            //常规追击玩家的逻辑，追击到就攻击
            if(distance <= boss.standAttackRange)
            {
                boss.ChangeState(BossState.Attack);
            }
            else
            {
                //每时每刻要根据玩家最新位置更新目的地
                boss.navMeshAgent.SetDestination(boss.targetPlayer.transform.position);
            }
        }
    }

    public override void Exit()
    {
        boss.navMeshAgent.enabled = false; 
        if(stopVigilantCoroutine != null)
        {
            MonoManager.Instance.StopCoroutine(stopVigilantCoroutine);
            stopVigilantCoroutine = null;
        }    
    }
}
