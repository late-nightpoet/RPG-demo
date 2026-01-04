using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_KnockUpState : Player_HurtStateBase
{
    private const string AnimKnockUp = "KnockUp";

     // 【新增】平滑转向相关变量
    private Quaternion targetRotation;
    private bool isRotatingToAttacker = false;
    
    // 转向速度：建议设高一点，因为被击飞是一瞬间的事，太慢会像在“驾驶”而不是“受击”
    // 1500度/秒 意味着大约 0.1~0.2秒能转过身去，既有过程感又足够猛烈
    private const float TurnSpeed = 1500f;

    private float repelElapsed = 0;
    private float repelDuration = 0;

    //minrepel最短要是0.2f，是knockup动画的时间长度，不然如果repeltime默认没有被设置的话knockup状态会被跳过然后直接进入knockdownland状态
    private const float MinRepelTime = 0.2f;
    private const float MinRepelSqr = 0.0001f;

    public override void Enter()
    {
        base.Enter();
        repelElapsed = 0;
        repelDuration = Mathf.Max(MinRepelTime, hitData.RepelTime);

           // 初始化变量
        targetRotation = player.transform.rotation; // 默认目标是当前朝向
        isRotatingToAttacker = false;

        // 如果是击飞状态 且 存在攻击来源
        if (source != null)
        {
            // 计算指向攻击者的向量
            Vector3 dirToAttacker = source.ModelTransform.position - player.transform.position;
            dirToAttacker.y = 0; 

            // 如果方向有效，设定目标旋转
            if (dirToAttacker.sqrMagnitude > 0.001f)
            {
                targetRotation = Quaternion.LookRotation(dirToAttacker);
                
                // 只有当当前朝向和目标朝向夹角较大时，才启动转向
                if (Quaternion.Angle(player.transform.rotation, targetRotation) > 5f)
                {
                    isRotatingToAttacker = true;
                }
            }
        }

        Vector3 repelWorld = hitData.RepelVelocity;
        //需要将击飞的距离转换到攻击者方向的击飞距离，这样才符合物理规律
        if (source != null)
        {
            repelWorld = source.ModelTransform.TransformDirection(hitData.RepelVelocity);
        }

  
        horizontalVelocity = new Vector3(repelWorld.x, 0f, repelWorld.z) / repelDuration;
        if (player.Ctx != null)
        {
            //通过repeltime和repel distance得到速度
            player.Ctx.velocity = new Vector3(horizontalVelocity.x, repelWorld.y / repelDuration, horizontalVelocity.z);
        }

        ShrinkCapsule();
        //如果是击飞的话，默认先进入击飞knockup 状态
        player.PlayAnimation(AnimKnockUp);
    }

    public override void Update()
    {
         DoRotateToAttacker();
                UpdateRepel();
                //击飞时需要速度*time.delta得到击飞距离获取物理移动距离来播放动画
                UpdateMovement(true);
                if (repelElapsed >= repelDuration)
                {
                    if (IsGrounded())
                    {
                        player.ChangeState(PlayerState.KnockDownLand);
                    }
                    else
                    {
                        Debug.Log("进入StartAirLoop");
                        player.ChangeState(PlayerState.KnockAirLoop);
                    }
                }

    }

    private void DoRotateToAttacker()
    {
        // 【新增】平滑转向逻辑
                if (isRotatingToAttacker)
                {
                    // 使用 RotateTowards 进行匀速转动，比 Lerp 更适合这种角度修正
                    player.transform.rotation = Quaternion.RotateTowards(
                        player.transform.rotation, 
                        targetRotation, 
                        TurnSpeed * Time.deltaTime
                    );

                    // 如果角度极小，就停止计算，节省性能
                    if (Quaternion.Angle(player.transform.rotation, targetRotation) < 1f)
                    {
                        isRotatingToAttacker = false;
                    }
                }
    }

    private void UpdateRepel()
    {
        if (repelElapsed >= repelDuration)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        repelElapsed += Time.deltaTime;
        if (repelElapsed >= repelDuration)
        {
            horizontalVelocity = Vector3.zero;
        }
    }
}
