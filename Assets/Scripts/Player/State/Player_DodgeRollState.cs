using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_DodgeRollState : PlayerStateBase
{
    //状态控制变量
    private bool isBehaviorDetermined ; //是否已经决定行为（翻滚还是后撤）
    
    
    // 新增：方向判定宽容时间，决策计时器
    private float decisionTimer; 
    private const float MAX_TOLERANCE_TIME = 0.1f; // 100ms的宽容窗口，足够覆盖人类的手速误差
    //行为标记
    private bool isBackstep;


    private void ExecuteRoll(Vector3 inputDir)
    {
        var ctx = player.Ctx;
       // 既然已经传进来了 finalDirection，就直接用
        isBehaviorDetermined = true;
        isBackstep = false;

        //状态标记
        ctx.isRolling = true;
        ctx.rollStartTime = Time.time;
        ctx.lastRollTime = Time.time;
        ctx.rollRequested = false;

        // 【关键】翻滚：先旋转角色朝向输入方向
        ctx.transform.rotation = Quaternion.LookRotation(inputDir.normalized);
        
        //触发动画
        ctx.animator.ResetTrigger("RollTrigger");
        ctx.animator.SetBool("IsRollDone", false);
        ctx.animator.SetBool("IsRolling", true);
        ctx.animator.SetInteger("RollDir", 0); // 只用 roll_f 动画
        ctx.animator.SetTrigger("RollTrigger");

    }

    private void ExecuteBackstep()
    {
        var ctx = player.Ctx;
        isBehaviorDetermined = true;
        isBackstep = true;

        //状态标记
        ctx.isRolling = true;
        ctx.rollStartTime = Time.time;
        ctx.lastRollTime = Time.time;
        ctx.rollRequested = false;

        // 【关键】后撤步：不旋转角色，保持当前朝向！
        
        // 动画：后撤步 (假设你在 Animator 里设置 RollDir=4 是后撤)
        //触发动画
        ctx.animator.ResetTrigger("RollTrigger");
        ctx.animator.SetBool("IsRollDone", false);
        ctx.animator.SetBool("IsRolling", true);
        ctx.animator.SetInteger("RollDir", 4); 
        ctx.animator.SetTrigger("RollTrigger");
    }

    private bool UpdateDodgePhysics(out float progress)
    {
        var ctx = player.Ctx;
        float duration = isBackstep ? ctx.dodgeDuration : ctx.dodgeRollDuration;
        // 计算时间进度
        float timeSinceStart = Time.time - ctx.rollStartTime;
        //因爲動畫是非rootmotion驅動的，所以需要distance\duration來設置每一幀移動的距離
        progress = Mathf.Clamp01(timeSinceStart / duration); // 0-1 的归一化进度

        // --- 修复滑步问题的核心逻辑 ---
        // 如果动画过渡还没完成（比如前0.05秒），强制不移动，或者移动得很慢
        // 这样能确保角色做出动作姿态后，才开始产生大位移
        if (timeSinceStart < 0.05f) 
        {
            ctx.velocity = Vector3.zero;
            ctx.speed2D = 0f;
            return false; 
        }
        // ---------------------------
        // 获取曲线值
        // 建议在 Inspector 把 rollSpeedCurve 设为 "Ease Out" (一开始快，后面慢)
        float speedMul = ctx.rollSpeedCurve != null ? ctx.rollSpeedCurve.Evaluate(progress) : 1f; // 曲线决定速度随时间的变化
        // 基础速度 = 距离 / 时间
        float distance = isBackstep ? ctx.dodgeDistance : ctx.rollDistance; // 后撤步距离通常短一点
        // 原来的公式是 匀速 * 曲线倍率，会导致总距离略有偏差，但在动作游戏中手感更重要
        // 只要把 duration 改回 0.6f，这里的 baseSpeed 就会变成正常的 4~5 m/s
        float baseSpeed = distance / Mathf.Max(duration, 0.0001f); // 匀速情况下的基础速度
        Vector3 moveDir;
        if (isBackstep)
        {
            // 后撤步：沿角色后方方向
            moveDir = -ctx.transform.forward;
        }
        else
        {
            // 翻滚位移方向：角色正前方（因为 ExecuteRoll 已经转过去了）
            moveDir = ctx.transform.forward;
        }

        ctx.velocity.x = moveDir.x * baseSpeed * speedMul;
        ctx.velocity.z = moveDir.z * baseSpeed * speedMul;

        // 同步动画参数用的移动速度，避免松键后残留旧值,更新 Speed2D (用于 Animator 混合树等，虽然闪避时不一定用)
        ctx.speed2D = new Vector3(ctx.velocity.x, 0f, ctx.velocity.z).magnitude;
       
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
        isBehaviorDetermined = false;
        decisionTimer = 0f;
        isBackstep = false;

        //重置状态, 这里重置了没关系，因为在onanyrollperformed下有判断ctx.rollRequested如果没被消费掉变成false就不会进来
        player.Ctx.isRolling = false;
        player.Ctx.animator.SetBool("IsRolling", false);


        // 瞬时判定：如果玩家进状态时已经按住了方向键，直接翻滚，不需要等待
        Vector3 instantInput = player.MovementHelper.GetRawWorldDirection();
        if (instantInput.sqrMagnitude > 0.01f)
        {
            ExecuteRoll(instantInput);
        }
        // 如果没有输入，不要急着后撤步，进入 Update 等待几帧
    }

    public override void Update()
    {
        // 第一阶段：等待方向输入确认（缓冲期）
        if (!isBehaviorDetermined)
        {
            decisionTimer += Time.deltaTime;
            
            // 持续检测输入
            Vector3 lateInput = player.MovementHelper.GetRawWorldDirection();
            
            // 情况A：在宽容时间内，玩家按下了方向键 -> 立即锁定方向并翻滚
            if (lateInput.sqrMagnitude > 0.01f)
            {
                ExecuteRoll(lateInput);
            
            }

            // 情况B：超时了（超过0.1秒）还是没按方向键 -> 认命吧，这就是个后撤步
            if (decisionTimer > MAX_TOLERANCE_TIME)
            {
                ExecuteBackstep();
               
            }
            
            // 如果还没超时也没输入，本帧什么都不做，单纯等待（Coyote Time）
            return;
        }

    // --- 阶段二：物理与动画更新 (Execution) ---
        bool doneByTime = UpdateDodgePhysics(out float rollProgress);
        var stateInfo = player.Ctx.animator.GetCurrentAnimatorStateInfo(0);
        // 只要是 Tag 为 Dodge 的动画，或者名字匹配
        // 建议在 Animator 里给 Roll_F 和 Backstep 都加上 "Dodge" 的 Tag
        bool isDodgeAnim = stateInfo.IsTag("Dodge") || stateInfo.IsName("Roll_F") || stateInfo.IsName("Backstep");
       
        // 只有当真正开始播放闪避动画后，才开始检查进度（防止刚SetTrigger动画还没切换的那一帧就退出了）
        bool animationStarted = isDodgeAnim && stateInfo.normalizedTime > 0.1f; 
        bool doneByAnim = animationStarted && stateInfo.normalizedTime >= 0.9f;

         //必須要加上這個判斷，因爲doneByTime結束的比動畫播放的進度快，如果不加animNormalized的判斷會導致角色翻到一半直接返回不播放剩下一半的roll動畫
        bool done = doneByTime && doneByAnim;

        player.MovementHelper.Sync();
        // 3. 退出条件：时间到了 且 动画播完了
        if (done)
        {
            EndRoll();
            // decide next state based on input
            player.MovementHelper.CalculateInput();
            if (!player.Ctx.isGrounded)
            {
                Debug.Log($"[Roll] codeProgress={rollProgress:F3}, animNormalized={stateInfo.normalizedTime:F3} -> Fall");
                player.ChangeState(PlayerState.Fall);
                return;
            }
            if (player.Ctx.SmoothedMoveInput.magnitude > 0.1f)
            {
                Debug.Log($"[Roll] codeProgress={rollProgress:F3}, animNormalized={stateInfo.normalizedTime:F3} -> Locomotion");
                player.ChangeState(PlayerState.Locomotion);
            }
            else
            {
                Debug.Log($"[Roll] codeProgress={rollProgress:F3}, animNormalized={stateInfo.normalizedTime:F3} -> Idle");
                player.ChangeState(PlayerState.Idle);
            }
        }
    }
}
