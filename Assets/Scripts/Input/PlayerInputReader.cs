using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputReader : MonoBehaviour, PlayerInput.IPlayerActions
{
    public Vector2 _mouseDelta;
    public Vector2 _moveComposite;

    public float _movementInputDuration;
    public bool _movementInputDetected;

    private PlayerInput playerInput;

    public Action onAimActivated;
    public Action onAimDeactivated;

    public Action onCrouchActivated;
    public Action onCrouchDeactivated;

    public Action onJumpPerformed;

    public Action onRollPerformed;

    public Action onLockOnToggled;

    public Action onSprintActivated;
    public Action onSprintDeactivated;

    public Action onWalkToggled;

    private void OnEnable()
    {
        if(playerInput == null)
        {
            playerInput = new PlayerInput();
            playerInput.Player.SetCallbacks(this);
        }
        playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        playerInput.Player.Disable();
    }

    public void OnLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _mouseDelta = context.ReadValue<Vector2>();
    }

    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _moveComposite = context.ReadValue<Vector2>();
        Debug.Log("playerinputreader Move Input: " + _moveComposite.ToString());
        _movementInputDetected = _moveComposite.magnitude > 0.1f;
    }

    public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(!context.performed) return;
        onJumpPerformed?.Invoke();
    }

    public void OnRoll(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(!context.performed) return;
        onRollPerformed?.Invoke();
    }
    
    public void OnToggleWalk(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(!context.performed) return;
        onWalkToggled?.Invoke();
    }

    public void OnSprint(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(context.started)
        {
            onSprintActivated?.Invoke();
        }
        else if(context.canceled)
        {
            onSprintDeactivated?.Invoke();
        }
    }

    public void OnCrouch(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(context.started)
        {
            onCrouchActivated?.Invoke();
        }
        else if(context.canceled)
        {
            onCrouchDeactivated?.Invoke();
        }
    }

    public void OnAim(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(context.started)
        {
            onAimActivated?.Invoke();
        }
        else if(context.canceled)
        {
            onAimDeactivated?.Invoke();
        }
    }




    public void OnLockOn(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(!context.performed) return;
        onLockOnToggled?.Invoke();
        onSprintDeactivated?.Invoke();
    }









}
