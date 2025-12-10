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
        Vector2 input = ctx.SmoothedMoveInput;
        Vector3 camForward = ctx.cameraTransform != null
            ? new Vector3(ctx.cameraTransform.forward.x, 0f, ctx.cameraTransform.forward.z).normalized
            : ctx.transform.forward;
        Vector3 camRight = ctx.cameraTransform != null
            ? new Vector3(ctx.cameraTransform.right.x, 0f, ctx.cameraTransform.right.z).normalized
            : ctx.transform.right;

        Vector3 worldDir = camForward * input.y + camRight * input.x;
        if (worldDir.sqrMagnitude < 0.0001f)
        {
            return camForward; // 无输入时默认按相机朝前翻滚
        }
        return worldDir.normalized;
    }

    private void StartRoll()
    {
        var ctx = player.Ctx;
        rollDirWorld = ComputeRollDirectionWorld();
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

        bool doneByAnim = inRollState && animNormalized >= 0.9f;
        bool done = doneByTime || doneByAnim;

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
