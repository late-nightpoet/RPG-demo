
using System;
using System.Collections.Generic;
using UnityEngine;

public class Skill2 : SkillObjectBase
{
    public override void Init(List<string> enemeyTagList, Action<IHurt, Vector3> onHitAction)
    {
        base.Init(enemeyTagList, onHitAction);
        Destroy(gameObject, 4f);
        //这个延时时长是特效从出现到击中敌人（砸地）中间的时长
        Invoke(nameof(StartSkillHit), 0.8f);
        Invoke(nameof(StopSkillHit), 0.9f);
    }
}
