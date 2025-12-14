using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Model : MonoBehaviour
{
    [SerializeField]private Animator animator;
    public Animator Animator { get { return animator; } }
    
    private ISkillOwner skillOwner;

    [SerializeField] private Weapon_Controller[] weapons;
    public void Init(Action footStepAction, ISkillOwner skillOwner, List<string> enemeyTagList)
    {
        this.foorStepAction = footStepAction;
        this.skillOwner = skillOwner;
        foreach (var weapon in weapons)
        {
            weapon.Init(enemeyTagList, skillOwner.OnHit);
        }
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

    private void StartSkillHit(int weaponIndex)
    {
        skillOwner.StartSkillHit(weaponIndex);
        weapons[weaponIndex].StartSkillHit();
    }

    private void StopSkillHit(int weaponIndex)
    { 
        skillOwner.StopSkillHit(weaponIndex);
        weapons[weaponIndex].StopSkillHit();
    }

    private void SkillCanSwitch()
    {
        skillOwner.SkillCanSwitch();
    }
    #endregion
}
