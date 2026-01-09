
using System;
using System.Collections.Generic;
using UnityEngine;

public class Skill1 : SkillObjectBase
{
    public override void Init(List<string> enemeyTagList, Action<IHurt, Vector3> onHitAction)
    {
        base.Init(enemeyTagList, onHitAction);
        Destroy(gameObject, 4f);
        //这个延时时长是特效从出现到击中敌人（砸地）中间的时长
        Invoke(nameof(StartSkillHit), 0.2f);
        // [修正] 在伤害判定开始后一小段时间，调用StopSkillHit来禁用碰撞体。
        // 这可以防止在特效消失后，看不见的伤害判定区依然存在的问题。
        // 这里的0.3f意味着伤害判定持续0.1秒 (从0.2秒开始，到0.3秒结束)。
        Invoke(nameof(StopSkillHit), 0.3f);
    }
}
