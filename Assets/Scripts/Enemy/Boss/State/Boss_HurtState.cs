using UnityEngine;

public class Boss_HurtState : BossStateBase
{
    private Skill_HitData hitData => boss.hitData;
    private ISkillOwner source => boss.hurtSource;

    private enum HurtPhase
    {
        HitStagger,
        KnockUp,
        AirLoop,
        KnockDownLand,
        KnockDownEnd
    }

    private const string AnimHitStagger = "Hurt";
    private const string AnimKnockUp = "KnockUp";
    private const string AnimAirLoop = "KnockAirLoop";
    private const string AnimKnockDownLand = "KnockDownLand";
    private const string AnimKnockDownEnd = "KnockDownEnd";

    //minrepel最短要是0.2f，是knockup动画的时间长度，不然如果repeltime默认没有被设置的话knockup状态会被跳过然后直接进入knockdownland状态
    private const float MinRepelTime = 0.2f;
    private const float MinRepelSqr = 0.0001f;

    private HurtPhase phase;
    private float repelElapsed = 0;
    private float repelDuration = 0;
    private Vector3 horizontalVelocity = Vector3.zero;

    // 【新增】定义空中/倒地时的胶囊体参数
    // 建议高度设为原高度的一半或更小，模拟蜷缩状态
    private const float AerialHeight = 0.5f; 
    private readonly Vector3 AerialCenter = new Vector3(0, 0.26f, 0); 

    // 【新增】用于记录原始数值以便恢复
    private float defaultHeight;
    private Vector3 defaultCenter;
    private float defaultStepOffset;
    private float defaultRadius; // 建议也记录半径

    // 【新增配置】受击动画的“停顿点” (0.0 ~ 1.0)
    // 0.1 表示动画播放到 10% 的时候停顿。建议设为受击动作幅度最大的那一帧。
    private const float HitStaggerFreezePoint = 0.15f; 

    // 【新增标记】是否处于卡肉停顿中
    private bool isHitStopFrozen = false;
    public override void Enter()
    {


        currHardTime = 0;
        repelElapsed = 0;
        repelDuration = 0;
        horizontalVelocity = Vector3.zero;

        // 1. 先保存原始胶囊体数据（防止变不回去）
        if (boss.CharacterController != null)
        {
            defaultHeight = boss.CharacterController.height;
            defaultCenter = boss.CharacterController.center;
            defaultStepOffset = boss.CharacterController.stepOffset; // 保存 StepOffset
            defaultRadius = boss.CharacterController.radius;       // 保存 Radius
        }

        //禁止rootmotion
        boss.Model.ClearRootMotionAction();

        if (boss.Model != null && boss.Model.Animator != null)
        boss.Model.Animator.applyRootMotion = false;

        // 【新增】重置停顿标记
        isHitStopFrozen = false;

        // 【修改开始】使用配置中的 IsKnockUp 开关来决定流程
        // 原逻辑：bool hasRepel = hitData.RepelTime > 0 && hitData.RepelVelocity.sqrMagnitude > MinRepelSqr;
        
        bool isKnockUp = hitData.IsKnockUp;
        // 如果没有勾选击飞，则进入原地受击硬直状态
        if (!isKnockUp)
        {
            phase = HurtPhase.HitStagger;
            boss.PlayAnimation(AnimHitStagger);
            return;
        }

        Vector3 repelWorld = hitData.RepelVelocity;
        //需要将击飞的距离转换到攻击者方向的击飞距离，这样才符合物理规律
        if (source != null)
        {
            repelWorld = source.ModelTransform.TransformDirection(hitData.RepelVelocity);
        }

        repelDuration = Mathf.Max(MinRepelTime, hitData.RepelTime);
        horizontalVelocity = new Vector3(repelWorld.x, 0f, repelWorld.z) / repelDuration;
        if (boss.Ctx != null)
        {
            //通过repeltime和repel distance得到速度
            boss.Ctx.velocity = new Vector3(horizontalVelocity.x, repelWorld.y / repelDuration, horizontalVelocity.z);
        }

        // 2. 【核心修改】如果是击飞，立即压缩胶囊体！
        //不压缩胶囊体的话会导致胶囊体在空中还是竖直状态导致模型还在播放动画但是胶囊体过早触碰地面，从而有角色在空中就进入knockdownland的bug
        if (boss.CharacterController != null)
        {
            // 必须先修改 Step Offset！防止因为 Offset 过大导致高度修改被拒绝
            boss.CharacterController.stepOffset = 0.1f; 
            
            // 建议：稍微减小半径，或者让高度稍微大于 2*Radius
            // 这里为了安全，我们把半径设为 0.2，高度 0.5，这样就是个瘦长的胶囊，肯定合法
            boss.CharacterController.radius = 0.2f;
            boss.CharacterController.height = AerialHeight;
            boss.CharacterController.center = AerialCenter;
        }
        //如果是击飞的话，默认先进入击飞knockup 状态
        phase = HurtPhase.KnockUp;
        boss.PlayAnimation(AnimKnockUp);
    }

    private float currHardTime = 0;

    public override void Update()
    {
        switch (phase)
        {
            case HurtPhase.HitStagger:
                HandleHitStaggerLogic();
                break;
            case HurtPhase.KnockUp:
                UpdateRepel();
                //击飞时需要速度*time.delta得到击飞距离获取物理移动距离来播放动画
                UpdateMovement(true);
                if (repelElapsed >= repelDuration)
                {
                    if (IsGrounded())
                    {
                        StartKnockDownLand();
                    }
                    else
                    {
                        Debug.Log("进入StartAirLoop");
                        StartAirLoop();
                    }
                }
                break;
            case HurtPhase.AirLoop:
                //空中状态只需要y方向的速度
                UpdateMovement(false);
                if (IsGrounded())
                {
                    StartKnockDownLand();
                }
                break;
            case HurtPhase.KnockDownLand:
                UpdateMovement(false);
                if (CheckAnimatorStateName(AnimKnockDownLand, out float landTime) && landTime >= 1f)
                {
                    // 【逻辑修改】如果有硬直时间，需要等待
                    if (hitData.HardTime > 0)
                    {
                        // 累加等待时间
                        currHardTime += Time.deltaTime;

                        // 只有等待时间超过了设定的 HardTime，才允许起身
                        if (currHardTime >= hitData.HardTime)
                        {
                            StartKnockDownEnd();
                        }
                    }
                    else
                    {
                        // 如果 HardTime 为 0，则维持原有逻辑，动画播完立即起身
                        StartKnockDownEnd();
                    }
                }
                break;
            case HurtPhase.KnockDownEnd:
                UpdateMovement(false);
                if (CheckAnimatorStateName(AnimKnockDownEnd, out float endTime) && endTime >= 1f)
                {
                    // Time.timeScale = 0;
                    // Time.fixedDeltaTime = 0.02f * 0;
                    boss.ChangeState(BossState.Idle);
                }
                break;
        }
    }

    // 【核心逻辑】处理先播放、再停顿、再恢复
    private void HandleHitStaggerLogic()
    {
        if (boss.Model == null || boss.Model.Animator == null) return;

        Animator animator = boss.Model.Animator;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 阶段 1: 还没停顿，正在播放前摇
        if (!isHitStopFrozen)
        {
            // 只有当动画真正开始播放(IsName)，且播放进度超过了设定点(FreezePoint)时，才冻结
            if (stateInfo.IsName(AnimHitStagger) && stateInfo.normalizedTime >= HitStaggerFreezePoint)
            {
                // 进入停顿阶段
                animator.speed = 0f; // 冻结动画
                isHitStopFrozen = true;
                currHardTime = 0f;   // 开始计时硬直
            }
        }
        // 阶段 2: 处于停顿中 (HardTime 计时)
        else
        {
            currHardTime += Time.deltaTime;

            // 硬直时间结束
            if (currHardTime >= hitData.HardTime)
            {
                // 阶段 3: 恢复播放
                animator.speed = 1f;

                // 检查动画是否已经彻底播完 (比如 > 0.99 或者 > 1)
                // 注意：由于之前被冻结在 0.15，现在恢复后需要等它播完剩下部分
                if (stateInfo.normalizedTime >= 1f)
                {
                    boss.ChangeState(BossState.Idle);
                }
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

    private void UpdateMovement(bool allowHorizontal)
    {
        if (boss.Ctx == null || boss.MovementHelper == null)
        {
            return;
        }

        if (!allowHorizontal)
        {
            boss.Ctx.velocity.x = 0f;
            boss.Ctx.velocity.z = 0f;
        }
        else
        {
            boss.Ctx.velocity.x = horizontalVelocity.x;
            boss.Ctx.velocity.z = horizontalVelocity.z;
        }

        boss.MovementHelper.ApplyGravity();
        boss.MovementHelper.Move();
        boss.MovementHelper.GroundedCheck();
    }

    private void StartAirLoop()
    {
        phase = HurtPhase.AirLoop;
        boss.PlayAnimation(AnimAirLoop);
    }

    private void StartKnockDownLand()
    {
       // 【新增】重置计时器，确保从0开始计算躺地等待时间
        currHardTime = 0f;
        phase = HurtPhase.KnockDownLand;
        boss.PlayAnimation(AnimKnockDownLand);
    }

    private void StartKnockDownEnd()
    {
        phase = HurtPhase.KnockDownEnd;
        boss.PlayAnimation(AnimKnockDownEnd);
    }

    private bool IsGrounded()
    {
        return boss.CharacterController != null && boss.CharacterController.isGrounded;
    }

    public override void Exit()
    {
        boss.Model.ClearRootMotionAction();
        if (boss.Ctx != null)
        {
            boss.Ctx.velocity = Vector3.zero;
        }

        // 【非常重要】退出状态时，必须把速度改回 1
        // 如果不改，下次进入 Idle 或者其他状态时动画会不动
        if (boss.Model != null && boss.Model.Animator != null)
        {
            boss.Model.Animator.speed = 1f;
        }

        // 3. 恢复胶囊体
        if (boss.CharacterController != null)
        {
            float targetHeight = defaultHeight > 0.1f ? defaultHeight : 1.5f; 

            
            // 恢复顺序：先恢复高度和半径，最后恢复 Step Offset
            // (其实恢复顺序不那么严格，但是个好习惯)
            boss.CharacterController.radius = defaultRadius > 0 ? defaultRadius : 0.25f;
            boss.CharacterController.height = targetHeight;
            boss.CharacterController.center = defaultCenter;
            
            // 最后恢复 Step Offset，因为现在高度已经变高了，0.3 是合法的
            boss.CharacterController.stepOffset = defaultStepOffset > 0 ? defaultStepOffset : 0.3f;
        }
    }
    
}
