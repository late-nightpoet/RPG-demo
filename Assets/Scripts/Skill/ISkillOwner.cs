using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillOwner
{
    //旋转是基于模型的旋转而不是整个角色的旋转，因为整个角色设定它的旋转值一直为0
    Transform ModelTransform { get;}
    void StartSkillHit(int weaponIndex);

    void StopSkillHit(int weaponIndex);

    void SkillCanSwitch();

    void OnHit(IHurt target,Vector3 hitPostion);

    void OnFootStep();
}
