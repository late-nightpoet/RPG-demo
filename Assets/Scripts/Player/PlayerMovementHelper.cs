using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerMovementHelper
{
    private readonly PlayerBlackBoard ctx;

    // 新：本地记录 sprint 按键是否按下（由 InputReader 事件驱动）
    private bool sprintPressed = false;

    public PlayerMovementHelper(PlayerBlackBoard ctx)
    {
        this.ctx = ctx;
        BindInputEvents();
    }

     // 绑定/解除输入事件
    private void BindInputEvents()
    {
        if (ctx?.inputReader == null) return;
        ctx.inputReader.onSprintActivated += OnSprintActivated;
        ctx.inputReader.onSprintDeactivated += OnSprintDeactivated;
        ctx.inputReader.onWalkToggled += OnWalkToggled;
        ctx.inputReader.onJumpPerformed += OnAnyJumpPerformed;
    }

    private void UnbindInputEvents()
    {
        if (ctx?.inputReader == null) return;
        ctx.inputReader.onSprintActivated -= OnSprintActivated;
        ctx.inputReader.onSprintDeactivated -= OnSprintDeactivated;
        ctx.inputReader.onWalkToggled -= OnWalkToggled;
        ctx.inputReader.onJumpPerformed -= OnAnyJumpPerformed;
    }

    public void CalculateInput()
    {
        #region to-use
        if (ctx.inputReader._movementInputDetected)
        {
            if (ctx.inputReader._movementInputDuration == 0)
            {
                ctx.movementInputTapped = true;
            }
            else if (ctx.inputReader._movementInputDuration > 0 && ctx.inputReader._movementInputDuration < ctx.buttonHoldThreshold)
            {
                ctx.movementInputTapped = false;
                ctx.movementInputPressed = true;
                ctx.movementInputHeld = false;
            }
            else
            {
                ctx.movementInputTapped = false;
                ctx.movementInputPressed = false;
                ctx.movementInputHeld = true; 
            }

            ctx.inputReader._movementInputDuration += Time.deltaTime;
        }
        else
        {
            ctx.inputReader._movementInputDuration = 0;
            ctx.movementInputTapped = false;
            ctx.movementInputPressed = false;
            ctx.movementInputHeld = false;
        }
        #endregion

        if (ctx.cameraTransform == null)
        {
             #if UNITY_EDITOR
            Debug.LogWarning("[MovementHelper] cameraTransform is null, falling back to Camera.main");
            #endif
            if (Camera.main != null) ctx.cameraTransform = Camera.main.transform;
        }
        if (ctx.inputReader != null)
            ctx.RawMoveInput = ctx.inputReader._moveComposite;
        else
            ctx.RawMoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        // 2. 平滑插值
        ctx.SmoothedMoveInput = Vector2.Lerp(ctx.SmoothedMoveInput, ctx.RawMoveInput, PlayerBlackBoard.INPUT_SMOOTH_SPEED * Time.deltaTime);
        //防止SmoothedMoveInput指数级衰减但不为0导致的微小抖动
        if (ctx.SmoothedMoveInput.sqrMagnitude < 1e-4f)
        {
            ctx.SmoothedMoveInput = Vector2.zero;
        }
        // todo movedirection之后还会根据相机方向变化
        ctx.moveDirection = new Vector3(ctx.SmoothedMoveInput.x, 0f, ctx.SmoothedMoveInput.y);
        //Debug.Log("ctx.moveDirection before camera adjustment: " + ctx.moveDirection.ToString());
          // 相机前后方向（Y 归零）
        Vector3 cameraForward = ctx.cameraTransform != null 
            ? new Vector3(ctx.cameraTransform.forward.x, 0f, ctx.cameraTransform.forward.z).normalized 
            : ctx.transform.forward;
        Vector3 cameraRight = ctx.cameraTransform != null 
            ? new Vector3(ctx.cameraTransform.right.x, 0f, ctx.cameraTransform.right.z).normalized 
            : ctx.transform.right;
        //Debug.Log("cameraForward: " + cameraForward.ToString() + " | cameraRight: " + cameraRight.ToString());

        // 计算世界空间移动方向（相对相机）
        ctx.moveDirection = (cameraForward * ctx.SmoothedMoveInput.y) + (cameraRight * ctx.SmoothedMoveInput.x);
        //Debug.Log("ctx.moveDirection after camera adjustment: " + ctx.moveDirection.ToString());
        ctx.animator.SetFloat("VerticalSpeed", ctx.SmoothedMoveInput.y);
        ctx.animator.SetFloat("HorizontalSpeed", ctx.SmoothedMoveInput.x);
        // 新：每帧根据当前 move input 与 sprint 按键状态评估是否冲刺
        EvaluateSprint();
    }

    public void CalculateMoveDirection()
    {
        CalculateInput();
        if (!ctx.isGrounded)
        {
            // 空中：维持起跳时的水平速度，只允许少量修正
            if (ctx.SmoothedMoveInput.sqrMagnitude > 0.01f)
            {
                Vector3 desired = ctx.moveDirection.normalized * ctx.currentMaxSpeed;
                ctx.velocity.x = Mathf.Lerp(ctx.velocity.x, desired.x, PlayerBlackBoard.ANIMATION_DAMP_TIME * Time.deltaTime);
                ctx.velocity.z = Mathf.Lerp(ctx.velocity.z, desired.z, PlayerBlackBoard.ANIMATION_DAMP_TIME * Time.deltaTime);
            }
            //防止在空中speed2d变为0时，人物还在空中，但是姿态变成idle姿态
            ctx.speed2D = new Vector3(ctx.velocity.x, 0f, ctx.velocity.z).magnitude;
            ctx.speed2D = Mathf.Round(ctx.speed2D * 1000f) / 1000f;
            CalculateGait();
            return;
            // ctx.targetMaxSpeed = ctx.currentMaxSpeed;
        }
        else if (ctx.isCrouching)
        {
            //如果角色 crouching，就用 walkSpeed
            ctx.targetMaxSpeed = ctx.walkSpeed;
        }
        else if (ctx.isSprinting)
        {
            //如果角色 sprinting，就用 runSpeed
            ctx.targetMaxSpeed = ctx.sprintSpeed;
        }
        else if (ctx.isWalking)
        {
            //如果角色 walk，就用 walkSpeed
            ctx.targetMaxSpeed = ctx.walkSpeed;
        }
        else
        {
            ctx.targetMaxSpeed = ctx.runSpeed;
        }
        ctx.currentMaxSpeed = Mathf.Lerp(ctx.currentMaxSpeed, ctx.targetMaxSpeed, PlayerBlackBoard.ANIMATION_DAMP_TIME * Time.deltaTime);
        ctx.targetVelocity.x = ctx.moveDirection.x * ctx.currentMaxSpeed;
        ctx.targetVelocity.z = ctx.moveDirection.z * ctx.currentMaxSpeed;

        ctx.velocity.z = Mathf.Lerp(ctx.velocity.z, ctx.targetVelocity.z, ctx.speedChangeDamping * Time.deltaTime);
        ctx.velocity.x = Mathf.Lerp(ctx.velocity.x, ctx.targetVelocity.x, ctx.speedChangeDamping * Time.deltaTime);
        Debug.Log("Calculated Velocity: " + ctx.velocity.ToString() + " | Target Velocity: " + ctx.targetVelocity.ToString() + " ctx.moveDirection: " + ctx.moveDirection.ToString());
        //计算2D速度，用于animator的MoveSpeed参数
        ctx.speed2D = new Vector3(ctx.velocity.x, 0f, ctx.velocity.z).magnitude;
        ctx.speed2D = Mathf.Round(ctx.speed2D * 1000f) / 1000f;
        CalculateGait();

    }

    public void CalculateGait()
    {
        float runThreshold = (ctx.walkSpeed + ctx.runSpeed) / 2f;
        float sprintThreshold = (ctx.runSpeed + ctx.sprintSpeed) / 2f;
        if (ctx.speed2D < 0.01)
        {
            ctx.currentGait = GaitState.Idle;
        }
        else if (ctx.speed2D < runThreshold)
        {
            ctx.currentGait = GaitState.Walk;
        }
        else if (ctx.speed2D < sprintThreshold)
        {
            ctx.currentGait = GaitState.Run;
        }
        else
        {
            ctx.currentGait = GaitState.Sprint;
        }
    }

    public void FaceMoveDirection()
    {
        // 取速度的水平分量作为朝向（而非输入方向）
        Vector3 faceDirection = new Vector3(ctx.velocity.x, 0f, ctx.velocity.z);

        if (faceDirection.magnitude < 0.01f)
        {
            return; // 无速度时不旋转
        }

        // 平滑旋转到速度方向
        Quaternion targetRotation = Quaternion.LookRotation(faceDirection);
        ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    /// <summary>
    ///     Applies gravity to the player.
    /// </summary>
    public void ApplyGravity()
    {
        //ctx.velocity.y += Physics.gravity.y * ctx.gravityMultiplier * Time.deltaTime;
        if (ctx.velocity.y > Physics.gravity.y)
        {
            ctx.velocity.y += Physics.gravity.y * ctx.gravityMultiplier * Time.deltaTime;
        }
    }

    public void GroundedCheck()
    {
        // Vector3 spherePosition = new Vector3(ctx.controller.transform.position.x, 
        // ctx.controller.transform.position.y + ctx.groundedOffset,
        // ctx.controller.transform.position.z);
        // // Vector3 spherePosition = ctx.controller.transform.position + ctx.controller.center + Vector3.up * ctx.groundedOffset;
        // Debug.Log("ctx.groundLayerMask" + ctx.groundLayerMask.ToString());
        // ctx.isGrounded = Physics.CheckSphere(spherePosition, ctx.controller.radius, ctx.groundLayerMask, QueryTriggerInteraction.Ignore) && ctx.controller.isGrounded;
        // Debug.Log("GroundedCheck: isGrounded = " + ctx.isGrounded);
        //isGrounded = Physics.CheckSphere(spherePosition, 0.3f, groundLayerMask, QueryTriggerInteraction.Ignore);

        // draw debug visualization for the ground check sphere
        //DebugDrawGroundCheck(spherePosition, ctx.controller.radius, ctx.isGrounded);
        ctx.isGrounded = ctx.controller.isGrounded;

    }

    public void Move()
    {
        ctx.controller.Move(ctx.velocity * Time.deltaTime);
    }

    public void Sync()
    {
        ctx.animator.SetFloat("MoveSpeed", ctx.speed2D);
        ctx.animator.SetBool("IsJumping", ctx.isJumping);
        ctx.animator.SetBool("IsGrounded", ctx.isGrounded);
        ctx.animator.SetInteger("CurrentGait", (int)ctx.currentGait);
        ctx.animator.SetFloat("FallingDuration", ctx.fallingDuration);
    }

    public void ResetFallingDuration()
    {
        ctx.fallStartTime = Time.time;
        ctx.fallingDuration = 0f;
    }

    public void UpdateFallingDuration()
    {
        ctx.fallingDuration = Time.time - ctx.fallStartTime;
    }

      // debug: draw the ground-check sphere (call from GroundedCheck)
    private void DebugDrawGroundCheck(Vector3 center, float radius, bool hit)
    {
        Color c = hit ? Color.green : Color.red;
        int seg = 24;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float ang = (2 * Mathf.PI / seg) * i;
            Vector3 next = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Debug.DrawLine(prev, next, c);
            prev = next;
        }

        // vertical helper line downwards and a short up line
        Debug.DrawLine(center + Vector3.up * (radius * 0.5f), center - Vector3.up * (radius + 0.2f), c);
        Debug.DrawLine(center, center + Vector3.up * 0.05f, c);

        // small cross at center
        float s = Mathf.Min(0.05f, radius * 0.1f);
        Debug.DrawLine(center + Vector3.right * s, center - Vector3.right * s, c);
        Debug.DrawLine(center + Vector3.forward * s, center - Vector3.forward * s, c);
    }
    
     private void OnSprintActivated()
    {
        sprintPressed = true;
        EvaluateSprint(); // 立即评估（CalculateInput 也会每帧再次评估）
    }

    private void OnSprintDeactivated()
    {
        sprintPressed = false;
        ctx.isSprinting = false;
    }

    private void OnWalkToggled()
    {
        ctx.isWalking = !ctx.isWalking;
    }

    // 根据当前输入与按键状态决定实际是否冲刺
    private void EvaluateSprint()
    {
        bool hasMoveInput = ctx.SmoothedMoveInput.magnitude > 0.1f;
        ctx.isSprinting = sprintPressed && hasMoveInput;
    }

    private void OnAnyJumpPerformed()
    {
        if (!ctx.isGrounded || ctx.isJumping) return;
        ctx.isJumping = true;
        ctx.isGrounded = false;
    }

    public void DecelerateToStop()
    {
        if (!ctx.isGrounded) return;

        ctx.velocity.x = Mathf.Lerp(ctx.velocity.x, 0f, ctx.speedChangeDamping * Time.deltaTime);
        ctx.velocity.z = Mathf.Lerp(ctx.velocity.z, 0f, ctx.speedChangeDamping  * Time.deltaTime);
        ctx.speed2D = new Vector3(ctx.velocity.x, 0f, ctx.velocity.z).magnitude;
        ctx.speed2D = Mathf.Round(ctx.speed2D * 1000f) / 1000f;

        if (ctx.speed2D < 0.05f)
        {
            ctx.velocity.x = ctx.velocity.z = 0f;
            ctx.speed2D = 0f;
            ctx.isStopped = true;
        }
    }

    // 新：对外释放接口，供外部（比如 Player_Controller）在 OnDisable/OnDestroy 调用
    public void Dispose()
    {
        UnbindInputEvents();
    }
}
