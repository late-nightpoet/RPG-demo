using System;
using System.Collections;
using System.Collections.Generic;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class PlayerAnimationController : MonoBehaviour
{
    #region enum

    private enum AnimationState
    {
        Base,
        Locomotion,
        Jump,
        Fall,
        Crounch
    }

    private enum GaitState
    {
        Idle,
        Walk,
        Run,
        Sprint
    }
    #endregion

    #region Player Settings Variables

    #region  Scripts/Objects
    private Animator animator;
    private CharacterController controller;
    private InputReader inputReader;

    public Transform cameraTransform;


    #endregion


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

    [Tooltip("Duration of falling.")]
    [SerializeField]
    //在空中持续掉落的时间
    private float fallingDuration;
    #endregion

    #region Grounded Srttings
            
    [Tooltip("Layer mask for checking ground.")]
    [SerializeField]
    private LayerMask groundLayerMask;

    [Tooltip("Useful for rough ground")]
    [SerializeField]
    private float groundedOffset = -0.14f;
    #endregion

    #endregion

    #region Runtime Variables

    //设置AnimatorController状态机的参数
    private AnimationState currentState = AnimationState.Base;

    //默认在地面上
    private bool isGrounded = true;

    private bool isJumping = false;

    private bool isCrouching = false;

    private bool isWalking = false;

    //是否在冲刺状态
    private bool isSprinting = false;

    //理论上应该用的最大速度
    private float targetMaxSpeed;

    //对targetMaxSpeed的平滑处理
    private float currentMaxSpeed;

    //角色当前的真实移动速度（包含水平x/z轴和垂直y轴（jump和fall时会有垂直的速度））
    private Vector3 velocity;

    //函数中的内部变量，放在外面是因为函数每帧调用，每帧new后再销毁影响性能，所以放在外部保持常态保存
    private Vector3 targetVelocity;

    //玩家移动方向
    private Vector3 moveDirection;

    //赋值给animator的MoveSpeed参数，将xz轴的速度合并为一个值
    private float speed2D;

    //轻触
    private bool movementInputTapped;
    //短按
    private bool movementInputPressed;
    //长按
    private bool movementInputHeld;

    private GaitState currentGait = GaitState.Idle;

    //在空中开始掉落的时间
    private float fallStartTime;



    #endregion

    #region Base State Variables
    //todo 不太清楚为什么要单独设置一个动画阻尼时间，并且把它用于currentMaxSped的系数
    private const float ANIMATION_DAMP_TIME = 5f;

    #endregion
    #region Animator Controller
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        inputReader = GetComponent<InputReader>();
        
        inputReader.onWalkToggled += ToggleWalk;
        inputReader.onSprintActivated += ActivateSprint;
        inputReader.onSprintDeactivated += DeactivateSprint;
        inputReader.onJumpPerformed += OnAnyJumpPerformed;
        SwitchState(AnimationState.Locomotion);

        
    }

    #endregion

    #region Shared State
    private void SwitchState(AnimationState newState)
    {
        ExitCurrentState();
        EnterState(newState);
    }



    private void ExitCurrentState()
    {
        switch (currentState)
        {
            case AnimationState.Base:
                //ExitBaseState();
                break;
            case AnimationState.Locomotion:
                ExitLocomotionState();
                break;
            case AnimationState.Jump:
                ExitJumpState();
                break;
        }

    }

    private void EnterState(AnimationState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case AnimationState.Base:
                //EnterBaseState();
                break;
            case AnimationState.Locomotion:
                EnterLocomotionState();
                break;
            case AnimationState.Jump:
                EnterJumpState();
                break;
            case AnimationState.Fall:
                EnterFallState();
                break;
        }
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        //2.2 垂直方向的移动
        float v = Input.GetAxis("Vertical");
        //animator.SetFloat("VerticalSpeed", v);
        //animator.SetFloat("HorizontalSpeed", h);
        CalculateInput();
        if (Input.GetKeyDown(KeyCode.T))
        {
            Time.timeScale = 0.1f;
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Time.timeScale = 1.0f;
        }
        
        

        switch (currentState)
        {
            case AnimationState.Locomotion:
                UpdateLocomotionState();
                break;
            case AnimationState.Jump:
                UpdateJumpState();
                break;
            case AnimationState.Fall:
                UpdateFallState();
                break;
        }

    }

    //更新animator的参数值，以使动画能够更新
    private void UpdateAnimatorControllerParameter()
    {
        animator.SetFloat("MoveSpeed", speed2D);
        animator.SetBool("IsJumping", isJumping);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetInteger("CurrentGait", (int)currentGait);
        animator.SetFloat("FallingDuration", fallingDuration);
    }
    #endregion

    #region Setup

    private void CalculateInput()
    {
        if (inputReader._movementInputDetected)
        {
            if (inputReader._movementInputDuration == 0)
            {
                movementInputTapped = true;
            }
            else if (inputReader._movementInputDuration > 0 && inputReader._movementInputDuration < buttonHoldThreshold)
            {
                movementInputTapped = false;
                movementInputPressed = true;
                movementInputHeld = false;
            }
            else
            {
                movementInputTapped = false;
                movementInputPressed = false;
                movementInputHeld = true; 
            }

            inputReader._movementInputDuration += Time.deltaTime;
        }
        else
        {
            inputReader._movementInputDuration = 0;
            movementInputTapped = false;
            movementInputPressed = false;
            movementInputHeld = false;
        }

        Vector2 move = Vector2.zero;
        if (inputReader != null)
            move = inputReader._moveComposite;
        else
            move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        // todo movedirection之后还会根据相机方向变化
        moveDirection = new Vector3(move.x, 0f, move.y);
          // 相机前后方向（Y 归零）
        Vector3 cameraForward = cameraTransform != null 
            ? new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized 
            : transform.forward;
        Vector3 cameraRight = cameraTransform != null 
            ? new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z).normalized 
            : transform.right;

        // 计算世界空间移动方向（相对相机）
        moveDirection = (cameraForward * move.y) + (cameraRight * move.x);
        animator.SetFloat("VerticalSpeed", move.y);
        animator.SetFloat("HorizontalSpeed", move.x);
    }
    #endregion

    #region Movement
    private void CalculateMoveDirection()
    {
        CalculateInput();
        if (!isGrounded)
        {
            //如果角色在空中，就不改变角色的理论最大速度
            targetMaxSpeed = currentMaxSpeed;
        }
        else if (isCrouching)
        {
            //如果角色 crouching，就用 walkSpeed
            targetMaxSpeed = walkSpeed;
        }
        else if (isSprinting)
        {
            //如果角色 sprinting，就用 runSpeed
            targetMaxSpeed = sprintSpeed;
        }
        else if (isWalking)
        {
            //如果角色 walk，就用 walkSpeed
            targetMaxSpeed = walkSpeed;
        }
        else
        {
            targetMaxSpeed = runSpeed;
        }
        currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetMaxSpeed, ANIMATION_DAMP_TIME * Time.deltaTime);
        targetVelocity.x = moveDirection.x * currentMaxSpeed;
        targetVelocity.z = moveDirection.z * currentMaxSpeed;


        velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, speedChangeDamping * Time.deltaTime);
        velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, speedChangeDamping * Time.deltaTime);


        //计算2D速度，用于animator的MoveSpeed参数
        speed2D = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        speed2D = Mathf.Round(speed2D * 1000f) / 1000f;
        CalculateGait();
    }

    private void CalculateGait()
    {
        float runThreshold = (walkSpeed + runSpeed) / 2f;
        float sprintThreshold = (runSpeed + sprintSpeed) / 2f;
        if (speed2D < 0.01)
        {
            currentGait = GaitState.Idle;
        }
        else if (speed2D < runThreshold)
        {
            currentGait = GaitState.Walk;
        }
        else if (speed2D < sprintThreshold)
        {
            currentGait = GaitState.Run;
        }
        else
        {
            currentGait = GaitState.Sprint;
        }
    }

    private void FaceMoveDirection()
    {
        // 取速度的水平分量作为朝向（而非输入方向）
        Vector3 faceDirection = new Vector3(velocity.x, 0f, velocity.z);

        if (faceDirection.magnitude < 0.01f)
        {
            return; // 无速度时不旋转
        }

        // 平滑旋转到速度方向
        Quaternion targetRotation = Quaternion.LookRotation(faceDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

        /// <summary>
        ///     Applies gravity to the player.
        /// </summary>
    private void ApplyGravity()
    {
        if (velocity.y > Physics.gravity.y)
        {
            velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    private void Move()
    {
        controller.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Ground Checks
    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(controller.transform.position.x, 
        controller.transform.position.y + groundedOffset,
        controller.transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, controller.radius, groundLayerMask, QueryTriggerInteraction.Ignore);
        //isGrounded = Physics.CheckSphere(spherePosition, 0.3f, groundLayerMask, QueryTriggerInteraction.Ignore);

    }

    #endregion

    #region walking state
    private void ToggleWalk()
    {
        EnableWalk(!isWalking);
    }

    private void EnableWalk(bool enable)
    {
        isWalking = enable && isGrounded && !isSprinting;
    }
    #endregion

    #region sprint state
    private void ActivateSprint()
    {
        //冲刺时不能是走路的速度
        EnableWalk(false);
        isSprinting = true;
    }

    private void DeactivateSprint()
    {
        isSprinting = false;
    }
    #endregion

    #region locomotion state
    private void EnterLocomotionState()
    {
        //只有在地面上运动时才能按空格键进行跳跃，在空中fall或者jump阶段按空格键无效
        //inputReader.onJumpPerformed += LocomotionToJumpState;
    }
    
    private void UpdateLocomotionState()
    {
        GroundedCheck();
        CalculateInput();
        if(!isGrounded)
        {
            SwitchState(AnimationState.Fall);
        }
        CalculateMoveDirection();
        FaceMoveDirection();
        UpdateAnimatorControllerParameter();
        Move();
    }

    private void ExitLocomotionState()
    {
       // inputReader.onJumpPerformed -= LocomotionToJumpState;
    }

    private void LocomotionToJumpState()
    {
        SwitchState(AnimationState.Jump);
    }
    #endregion

    #region Jump State
    private void OnAnyJumpPerformed()
    {
        if (!isGrounded || isJumping) return; // 已在空中或正在跳，忽略

         // 启动跳跃：设置标志并给垂直速度一个初始跳跃力
        isJumping = true;
        isGrounded = false;
        velocity.y = jumfForce;
       
        SwitchState(AnimationState.Jump);
    }
    private void EnterJumpState()
    {
        animator.SetBool("IsJumping", true);
        isJumping = true;
    
    }

    private void UpdateJumpState()
    {
        ApplyGravity();
        if(velocity.y <= 0f)
        {
            animator.SetBool("IsJumping", false);
            isJumping = false;
            SwitchState(AnimationState.Fall);
        }
        GroundedCheck();
        CalculateMoveDirection();
        UpdateAnimatorControllerParameter();
        Move();
    }

    private void ExitJumpState()
    {
        animator.SetBool("IsJumping", false);
        isJumping = false;
    }
    #endregion

    #region Fall State
    private void EnterFallState()
    {
        ResetFallingDuration();
        velocity.y = 0f;
    }

    private void UpdateFallState()
    {
        GroundedCheck();
        CalculateMoveDirection();
        ApplyGravity();
        Move();
        UpdateAnimatorControllerParameter();
        if(controller.isGrounded)
        {
            SwitchState(AnimationState.Locomotion);
        }
        UpdateFallingDuration();
    }
    #endregion

    #region Falling

    private void ResetFallingDuration()
    {
        fallStartTime = Time.time;
        fallingDuration = 0f;
    }

    private void UpdateFallingDuration()
    {
        fallingDuration = Time.time - fallStartTime;
    }
    #endregion

    #region Testing

     void OnDrawGizmosSelected()
    {
    if (controller == null) return;
    
    Gizmos.color = isGrounded ? Color.green : Color.red;
    Vector3 spherePosition = new Vector3(controller.transform.position.x, 
        controller.transform.position.y + groundedOffset,
        controller.transform.position.z);
    Gizmos.DrawWireSphere(spherePosition, controller.radius);
    }
    #endregion
}
