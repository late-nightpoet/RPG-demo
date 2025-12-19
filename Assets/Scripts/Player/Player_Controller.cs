using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : MonoBehaviour, IStateMachineOwner, ISkillOwner
{
    
    #region Player Settings Variables

    #region Scripts/Objects
    [SerializeField]private Player_Model playerModel;
    private PlayerInputReader inputReader;

    [SerializeField]private CharacterController characterController;

    [SerializeField]private Transform cameraTransform;
    private StateMachine stateMachine;

    [SerializeField]private AudioSource audioSource;

    [field:SerializeField]public PlayerBlackBoard Ctx { get; private set; }
    public PlayerMovementHelper MovementHelper { get; private set; }

    public Player_Model Model { get { return playerModel; } }

    public CharacterController CharacterController { get { return characterController; } }

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


    public AudioClip[] footStepAudioClips;

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

    #region Attack Settings

    public SkillConfig testSkillConfig;

    public List<string> enemeyTagList;

    public SkillConfig[] standAttackConfig;
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
        //inputReader.onWalkToggled += ToggleWalk;
        //inputReader.onSprintActivated += ActivateSprint;
        //inputReader.onSprintDeactivated += DeactivateSprint;
        //inputReader.onJumpPerformed += OnAnyJumpPerformed;
        //组装上下文
        Ctx = new PlayerBlackBoard
        {
            animator = playerModel.Animator,
            controller = characterController,
            inputReader = inputReader,
            cameraTransform = cameraTransform,
            playerModel = playerModel,
            transform = transform,
            groundLayerMask = groundLayerMask
        };
        MovementHelper = new PlayerMovementHelper(Ctx);
        stateMachine = new StateMachine();
        stateMachine.Init(this);
        playerModel.Init(OnFootStep,this, enemeyTagList);
        stateMachine.ChangeState<Player_IdleState>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            slowMotionEnabled = !slowMotionEnabled;
            ApplyTimeScale(slowMotionEnabled ? slowMotionScale : 1f);
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

    public void ChangeState(PlayerState playerState)
    {
        switch(playerState)
        {
            case PlayerState.Idle:
                stateMachine.ChangeState<Player_IdleState>();
                break;
            case PlayerState.Locomotion:
                Debug.Log("Change to Locomotion State");
                stateMachine.ChangeState<Player_LocomotionState>();
                break;
            case PlayerState.Fall:
                Debug.Log("Change to Fall State");
                stateMachine.ChangeState<Player_FallState>();
                break;
            case PlayerState.Land:
                Debug.Log("Change to Land State");
                stateMachine.ChangeState<Player_LandState>();
                break;
            case PlayerState.Jump:
                Debug.Log("Change to Jump State");
                stateMachine.ChangeState<Player_JumpState>();
                break;
            case PlayerState.DodgeRoll:
                Debug.Log("Change to Roll State");
                stateMachine.ChangeState<Player_DodgeRollState>();
                break;
            case PlayerState.StandAttack:
                Debug.Log("Change to StandAttack State");
                stateMachine.ChangeState<Player_StandAttackState>();
                break;
        }
    }

    #region  攻击技能

    private SkillConfig currentSkillConfig;
    private int currentHitIndex = 0;

    public void StartAttack(SkillConfig skillConfig)
    {
        currentSkillConfig = skillConfig;
        currentHitIndex = 0;
        PlayAnimation(currentSkillConfig.AnimationName);

        SpawnSkillObject(skillConfig.ReleaseData.SpawnObj);
        PlayAudio(currentSkillConfig.ReleaseData.AudioClip);
    }

    public void StartSkillHit(int weaponIndex)
    {
        SpawnSkillObject(currentSkillConfig.AttackData[currentHitIndex].SpawnObj);
        PlayAudio(currentSkillConfig.AttackData[currentHitIndex].AudioClip);
    }

    public void StopSkillHit(int weaponIndex)
    {
        currentHitIndex += 1;
    }

    public void SkillCanSwitch()
    {
        
    }

    private void SpawnSkillObject(Skill_SpawnObj spawnObj)
    {
        if(spawnObj != null && spawnObj.Prefab != null)
        {
            StartCoroutine(DoSpawnObject(spawnObj));
        }
        
    }

    private IEnumerator DoSpawnObject(Skill_SpawnObj spawnObj)
    {
        //先执行延迟事件
        yield return new WaitForSeconds(spawnObj.Time);
        //之所以不设置为相对父物体是因为父物体是Player,而旋转时旋转的是Player旗下的模型，而player本身并不会旋转
        GameObject skillObj = GameObject.Instantiate(spawnObj.Prefab, null);
        //设置相对于技能释放者所在的位置以及旋转
        skillObj.transform.position = Model.transform.position + spawnObj.Position;
        skillObj.transform.eulerAngles = Model.transform.eulerAngles + spawnObj.Rotation;
        PlayAudio(spawnObj.AudioClip);
    }

    #endregion

    public void PlayAnimation(string animationName, float fixedTransitionDuration = 0.25f)
    {
        playerModel.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration);
    
    }

    public void PlayAudio(AudioClip audioClip)
    {
        if(audioClip != null)audioSource.PlayOneShot(audioClip);
    }

    private void OnFootStep()
    {
        if (footStepAudioClips.Length == 0) return;
        int index = UnityEngine.Random.Range(0, footStepAudioClips.Length);
        audioSource.PlayOneShot(footStepAudioClips[index]);
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale;
    }

    
    public void OnHit(IHurt target, Vector3 hitPostion)
    {
        Debug.Log("Hit target: " + ((Component)target).gameObject.name);
    }
}
