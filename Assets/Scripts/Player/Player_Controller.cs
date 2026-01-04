using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class Player_Controller : CharacterBase
{
    
    #region Player Settings Variables

    #region Scripts/Objects
    
    private PlayerInputReader inputReader;
    [SerializeField]private Transform cameraTransform;
    [field:SerializeField]public PlayerBlackBoard Ctx { get; private set; }
    public PlayerMovementHelper MovementHelper { get; private set; }

    [SerializeField] private CinemachineImpulseSource impulseSource;

    #endregion

    #region 配置类型信息

    #region Locomotion Settings
    [SerializeField]
    public float walkSpeed = 2f;
    [SerializeField]
    public float runSpeed = 4f;
    [SerializeField]
    private float sprintSpeed = 7f;
    [Tooltip("Damping factor for changing speed")]
    [SerializeField]
    //速度阻尼因子，用于平滑变化速度
    private float speedChangeDamping = 10;
    [Header("Shuffles")]
    [Tooltip("Threshold for button hold duration.")]
    [SerializeField]
    private float buttonHoldThreshold = 0.15f;

    #endregion

    #region In-AirSettings
    [Header("Player In-Air")]
    [Tooltip("Force applied when the player jumps.")]
    [SerializeField]
    public float jumfForce = 10f;

    [Tooltip("Multiplier for gravity when in the air.")]
    [SerializeField]
    private float gravityMultiplier = 2f;

    #region Grounded Srttings
            
    [Tooltip("Layer mask for checking ground.")]
    [SerializeField]
    private LayerMask groundLayerMask;

    [Tooltip("Useful for rough ground")]
    [SerializeField]
    private float groundedOffset = -0.14f;
    #endregion

    #endregion

    #endregion

    [Header("Debug")]
    [SerializeField] private float slowMotionScale = 0.2f;
    private bool slowMotionEnabled = false;

    private bool pauseGame = false;

    #region Attack Settings



 
    #endregion

    #endregion
    void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        //防止相机托错
        if (cameraTransform == null || cameraTransform.GetComponent<Camera>() == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.Log("[Player_Controller] cameraTransform auto-set to Camera.main");
            }
            else
            {
                Debug.LogWarning("[Player_Controller] No Camera.main found; please assign cameraTransform to the active camera.");
            }
        }
    }
    private void Start()
    {
        //锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        //inputReader.onWalkToggled += ToggleWalk;
        //inputReader.onSprintActivated += ActivateSprint;
        //inputReader.onSprintDeactivated += DeactivateSprint;
        //inputReader.onJumpPerformed += OnAnyJumpPerformed;
        //组装上下文
        Ctx = new PlayerBlackBoard
        {
            animator = Model.Animator,
            controller = characterController,
            inputReader = inputReader,
            cameraTransform = cameraTransform,
            playerModel = (Player_Model)Model,
            transform = transform,
            groundLayerMask = groundLayerMask
        };
        MovementHelper = new PlayerMovementHelper(Ctx);
        Init();
        stateMachine.ChangeState<Player_IdleState>();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            slowMotionEnabled = !slowMotionEnabled;
            ApplyTimeScale(slowMotionEnabled ? slowMotionScale : 1f);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            pauseGame = !pauseGame;
            ApplyTimeScale(pauseGame ? 0f : 1f);
        }
    }

    private void OnDestroy()
    {
        //inputReader.onWalkToggled -= ToggleWalk;
        //inputReader.onSprintActivated -= ActivateSprint;
        //inputReader.onSprintDeactivated -= DeactivateSprint;
        //inputReader.onJumpPerformed -= OnAnyJumpPerformed;
        MovementHelper?.Dispose();
        stateMachine?.Stop();
         if (slowMotionEnabled)
        {
            ApplyTimeScale(1f);
        }
        
    }

    public void ChangeState(PlayerState playerState, bool reCurrstate = false)
    {
        switch(playerState)
        {
            case PlayerState.Idle:
                stateMachine.ChangeState<Player_IdleState>(reCurrstate);
                break;
            case PlayerState.Locomotion:
                stateMachine.ChangeState<Player_LocomotionState>(reCurrstate);
                break;
            case PlayerState.Fall:
                stateMachine.ChangeState<Player_FallState>(reCurrstate);
                break;
            case PlayerState.Land:
                stateMachine.ChangeState<Player_LandState>(reCurrstate);
                break;
            case PlayerState.Jump:
                stateMachine.ChangeState<Player_JumpState>(reCurrstate);
                break;
            case PlayerState.DodgeRoll:
                stateMachine.ChangeState<Player_DodgeRollState>(reCurrstate);
                break;
            case PlayerState.StandAttack:
                stateMachine.ChangeState<Player_StandAttackState>(reCurrstate);
                break;
            case PlayerState.HitStagger:
                Debug.Log("player Change to HitStagger State");
                stateMachine.ChangeState<Player_HitStaggerState>(reCurrstate);
                break;
            case PlayerState.KnockUp:
                stateMachine.ChangeState<Player_KnockUpState>(reCurrstate);
                break;
            case PlayerState.KnockAirLoop:
                stateMachine.ChangeState<Player_KnockAirLoopState>(reCurrstate);
                break;
            case PlayerState.KnockDownLand:
                stateMachine.ChangeState<Player_KnockDownLandState>(reCurrstate);
                break;
            case PlayerState.KnockDownRise:
                stateMachine.ChangeState<Player_KnockDownRiseState>(reCurrstate);
                break;
        }
    }





    public void ScreenImpulse(float force)
    {
        impulseSource.GenerateImpulse(force * 2);
    }
  

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale;
    }

    public override void OnHit(IHurt target, Vector3 hitPostion)
    {
        Debug.Log("player 进入hit");
        // 拿到这一段攻击的数据
        Skill_AttackData attackData = CurrentSkillConfig.AttackData[currentHitIndex];
        // 生成基于命中配置的效果
        StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig, hitPostion));
        // 播放效果类
        if (attackData.ScreenImpulseValue != 0) ScreenImpulse(attackData.ScreenImpulseValue);
        if (attackData.ChromaticAberrationValue != 0) PostProcessManager.Instance.ChromaticAberrationEF(attackData.ChromaticAberrationValue);
        StartFreezeFrame(attackData.FreezeFrameTime);
        StartFreezeTime(attackData.FreezeGameTime);
        // 传递伤害数据
        target.Hurt(attackData.HitData, this);
    }

    public override void Hurt(Skill_HitData hitData, ISkillOwner hurtSource)
    {
        Debug.Log("进入player的hurt状态");
        base.Hurt(hitData, hurtSource);
        if (hitData.IsKnockUp)
        {
            // 击飞路线
            ChangeState(PlayerState.KnockUp, true);
        }
        else
        {
            // 原地受击路线
            ChangeState(PlayerState.HitStagger, true);
        }
    }

}
