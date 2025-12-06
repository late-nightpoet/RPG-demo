using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Model : MonoBehaviour
{
    [SerializeField]private Animator animator;
    public Animator Animator { get { return animator; } }

    public void Init(Action footStepAction)
    {
        this.foorStepAction = footStepAction;
    }

    #region 根运动
    private Action<Vector3, Quaternion> rootMotionAction;

    public void SetRootMotionAction(Action<Vector3, Quaternion> action)
    {
        rootMotionAction = action;
    }

    public void ClearRootMotionAction()
    {
        rootMotionAction = null;
    }

    private void OnAnimatorMove()
    {
        if (rootMotionAction != null)
        {
            rootMotionAction.Invoke(animator.deltaPosition, animator.deltaRotation);
        }
    }
    #endregion

    #region 动画事件
    private Action foorStepAction;

    private void FootStep()
    {
        foorStepAction?.Invoke();
    }
    #endregion
}
