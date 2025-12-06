using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public sealed class PlayerBlackBoard
{
    #region Player Settings Variables
    #region  References
    public Animator animator;
    public CharacterController controller;
    public PlayerInputReader inputReader;
    public Transform cameraTransform;

    public Player_Model playerModel;

    public Transform transform;

    #endregion


    #region Locomotion Settings
    [SerializeField]
    public float walkSpeed = 2f;
    [SerializeField]
    public float runSpeed = 4f;
    [SerializeField]
    public float sprintSpeed = 7f;
    [Tooltip("Damping factor for changing speed")]
    [SerializeField]
    //速度阻尼因子，用于平滑变化速度
    public float speedChangeDamping = 10;

    //todo 不太清楚为什么要单独设置一个动画阻尼时间，并且把它用于currentMaxSped的系数
    public const float ANIMATION_DAMP_TIME = 5f;
    [Header("Shuffles")]
    [Tooltip("Threshold for button hold duration.")]
    [SerializeField]
    public float buttonHoldThreshold = 0.15f;

    #endregion

    #region In-AirSettings
    [Header("Player In-Air")]
    [Tooltip("Force applied when the player jumps.")]
    [SerializeField]
    public float jumpForce = 10f;

    [Tooltip("Multiplier for gravity when in the air.")]
    [SerializeField]
    public float gravityMultiplier = 2f;

    #endregion

    #region Grounded Srttings
            
    [Tooltip("Layer mask for checking ground.")]
    [SerializeField]
    public LayerMask groundLayerMask;

    [Tooltip("Useful for rough ground")]
    [SerializeField]
    public float groundedOffset = -0.14f;
    #endregion

    #endregion

    //共享给所有状态机状态使用的变量
    #region Runtime Variables

    //设置AnimatorController状态机的参数
    public PlayerState currentState = PlayerState.Idle;

    //默认在地面上
    public bool isGrounded = true;
    //标识在jumping状态中
    public bool isJumping = false;

    //标识在landing状态中,标志着落地动画是否播放完毕，防止落地动画没播完时animator就切换状态机，导致fsm卡在landstate脚本中（fsm和animator可以不同步，例如fsm处于land而animator已经切换到idle）
    public bool isLandingDone = false;

    public bool isCrouching = false;

    public bool isWalking = false;

    //是否在冲刺状态
    public bool isSprinting = false;
    //用于locomotion状态中判断是否停止移动，防止locomotion切换到idle状态变化过快人物动作僵硬
    public bool isStopped = true;

    //理论上应该用的最大速度
    public float targetMaxSpeed;

    //对targetMaxSpeed的平滑处理
    public float currentMaxSpeed;

    //赋值给animator的MoveSpeed参数，将xz轴的速度合并为一个值
    public float speed2D;

    //玩家输入的移动向量,wasd方向
    public Vector2 MoveInput;

    //角色当前的真实移动速度（包含水平x/z轴和垂直y轴（jump和fall时会有垂直的速度））
    public Vector3 velocity;

    //函数中的内部变量，放在外面是因为函数每帧调用，每帧new后再销毁影响性能，所以放在外部保持常态保存
    public Vector3 targetVelocity;

    //玩家移动方向
    public Vector3 moveDirection;

    //轻触
    public bool movementInputTapped;
    //短按
    public bool movementInputPressed;
    //长按
    public bool movementInputHeld;

    public GaitState currentGait = GaitState.Idle;

    //在空中开始掉落的时间
    public float fallStartTime;

    //在空中持续掉落的时间
    public float fallingDuration;

    #endregion

    public void ResetFallingTimer()
    {
        fallStartTime = Time.time;
        fallingDuration = 0f;
    }

    public void UpdateFallingTimer()
    {
        fallingDuration = Time.time - fallStartTime;
    }

}
