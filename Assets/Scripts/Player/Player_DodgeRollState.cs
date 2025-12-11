using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_DodgeRollState : PlayerStateBase
{
    private bool rollStarted;
    private Vector3 rollDirWorld = Vector3.forward;

    private Vector3 ComputeRollDirectionWorld()
    {
        var ctx = player.Ctx;
        // 直接复用 CalculateInput 已经算好的相机对齐方向，避免重复计算
        if (ctx.moveDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }
        return ctx.moveDirection.normalized;
    }

    private void StartRoll()
    {
        var ctx = player.Ctx;
        // 【关键修改】使用 Helper 里新写的 GetRawWorldDirection
        // 这样即使 SmoothedInput 还没反应过来，只要按键了，Raw 肯定有值
        Vector3 rawDir = player.MovementHelper.GetRawWorldDirection();

        // 逻辑分支：有输入则定向翻滚，无输入则后撤步/向后翻
        if (rawDir.sqrMagnitude > 0.001f)
        {
            Debug.Log("Raw input detected, performing Directional Roll");
            rollDirWorld = rawDir.normalized;
        }
        else
        {
            // 【类魂逻辑】如果没有按下方向键，通常是向角色背后的方向翻滚（后撤步）
            // 或者你可以设置为向前翻，取决于你的设计。这里演示向后：
            Debug.Log("No raw input, performing Backstep/Backward Roll");
            rollDirWorld = -ctx.transform.forward; 
            
            // 如果你的游戏设计是“不按方向键默认向前翻”，则用:
            // rollDirWorld = ctx.transform.forward;
        }
        ctx.rollRequested = false;
        ctx.isRolling = true;
        ctx.rollStartTime = Time.time;
        ctx.lastRollTime = Time.time;

        ctx.animator.ResetTrigger("RollTrigger");
        ctx.animator.SetBool("IsRollDone", false);
        ctx.animator.SetBool("IsRolling", true);
        ctx.animator.SetInteger("RollDir", 0); // 只用 roll_f 动画
        ctx.animator.SetTrigger("RollTrigger");

        Vector3 dir = rollDirWorld; dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            ctx.transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }

    private bool UpdateRoll(out float progress)
    {
        var ctx = player.Ctx;
        //因爲動畫是非rootmotion驅動的，所以需要distance\duration來設置每一幀移動的距離
        progress = Mathf.Clamp01((Time.time - ctx.rollStartTime) / ctx.rollDuration); // 0-1 的归一化进度
        float speedMul = ctx.rollSpeedCurve != null ? ctx.rollSpeedCurve.Evaluate(progress) : 1f; // 曲线决定速度随时间的变化
        float baseSpeed = ctx.rollDistance / Mathf.Max(ctx.rollDuration, 0.0001f); // 匀速情况下的基础速度
        Vector3 dir = rollDirWorld.normalized;

        ctx.velocity.x = dir.x * baseSpeed * speedMul;
        ctx.velocity.z = dir.z * baseSpeed * speedMul;

        // 同步动画参数用的移动速度，避免松键后残留旧值
        ctx.speed2D = new Vector3(ctx.velocity.x, 0f, ctx.velocity.z).magnitude;
        // Debug.Log("vertical speed during roll: " + ctx.velocity.y + " horizontal speed: " + ctx.speed2D + "movespeed is " + ctx.speed2D);
        player.MovementHelper.ApplyGravity();
        player.MovementHelper.Move();

        return progress >= 0.99f;
    }

    private void EndRoll()
    {
        var ctx = player.Ctx;
        ctx.isRolling = false;
        ctx.animator.SetBool("IsRollDone", true);
        ctx.animator.SetBool("IsRolling", false);
        ctx.velocity.x = ctx.velocity.z = 0f;
        ctx.speed2D = 0f;
    }

    public override void Enter()
    {
        // 进入翻滚：计算方向、触发动画、记录开始时间
        rollStarted = false;
        if (player.Ctx.isRolling) return;

        player.MovementHelper.CalculateInput();
        StartRoll();
        rollStarted = true;
    }

    public override void Update()
    {
        // 翻滚过程中驱动位移，结束时切回合适状态
        if (!rollStarted)
        {
            Debug.LogWarning("Roll not started properly, returning to Idle state.");
            player.ChangeState(PlayerState.Idle);
            return;
        }

        bool doneByTime = UpdateRoll(out float rollProgress);
        var stateInfo = player.Ctx.animator.GetCurrentAnimatorStateInfo(0);
        bool inRollState = stateInfo.IsName("Roll_F") || stateInfo.tagHash == Animator.StringToHash("Roll");
        float animNormalized = inRollState ? stateInfo.normalizedTime : 0f;
        //必須要加上這個判斷，因爲doneByTime結束的比動畫播放的進度快，如果不加animNormalized的判斷會導致角色翻到一半直接返回不播放剩下一半的roll動畫
        bool doneByAnim = inRollState && animNormalized >= 0.9f;
        bool done = doneByTime && doneByAnim;

        player.MovementHelper.Sync();

        if (done)
        {
            EndRoll();
            // decide next state based on input
            player.MovementHelper.CalculateInput();
            if (!player.Ctx.isGrounded)
            {
                Debug.Log($"[Roll] codeProgress={rollProgress:F3}, animNormalized={animNormalized:F3} -> Fall");
                player.ChangeState(PlayerState.Fall);
                return;
            }
            if (player.Ctx.SmoothedMoveInput.magnitude > 0.1f)
            {
                Debug.Log($"[Roll] codeProgress={rollProgress:F3}, animNormalized={animNormalized:F3} -> Locomotion");
                player.ChangeState(PlayerState.Locomotion);
            }
            else
            {
                Debug.Log($"[Roll] codeProgress={rollProgress:F3}, animNormalized={animNormalized:F3} -> Idle");
                player.ChangeState(PlayerState.Idle);
            }
        }
    }
}
