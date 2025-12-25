using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillOwner
{
    void StartSkillHit(int weaponIndex);

    void StopSkillHit(int weaponIndex);

    void SkillCanSwitch();

    void OnHit(IHurt target,Vector3 hitPostion);

    void OnFootStep();
}
