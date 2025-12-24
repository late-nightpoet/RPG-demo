using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ModelBase : MonoBehaviour
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

    // AnimationEvent: called from footstep frames
    private void FootStep()
    {
        foorStepAction?.Invoke();
    }

    // AnimationEvent: enable weapon hitbox for the given weapon index
    private void StartSkillHit(int weaponIndex)
    {
        skillOwner.StartSkillHit(weaponIndex);
        weapons[weaponIndex].StartSkillHit();
    }

    // AnimationEvent: disable weapon hitbox for the given weapon index
    private void StopSkillHit(int weaponIndex)
    { 
        skillOwner.StopSkillHit(weaponIndex);
        weapons[weaponIndex].StopSkillHit();
    }

    // AnimationEvent: notify skill can be switched
    private void SkillCanSwitch()
    {
        skillOwner.SkillCanSwitch();
    }
    #endregion
}
