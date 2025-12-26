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
    private const float MinRepelTime = 0.0001f;
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

        bool hasRepel = hitData.RepelTime > 0 &&
                        hitData.RepelVelocity.sqrMagnitude > MinRepelSqr;
        //判断一下是要硬直状态还是击飞状态，硬直与击飞不可同时存在
        if (!hasRepel)
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
                if (currHardTime >= hitData.HardTime)
                {
                    boss.ChangeState(BossState.Idle);
                }
                else
                {
                    currHardTime += Time.deltaTime;
                }
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
                    //等到躺地的动作完成之后再起身
                    StartKnockDownEnd();
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
