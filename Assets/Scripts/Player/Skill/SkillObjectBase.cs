using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 远程特效攻击时特效附着脚本
/// </summary>
public class SkillObjectBase : MonoBehaviour
{
     [SerializeField] private new Collider collider;

    private List<string> enemeyTagList;
    private List<IHurt> enemyList = new List<IHurt>();

    private Action<IHurt, Vector3> onHitAction;

    public virtual void Init(List<string> enemeyTagList, Action<IHurt, Vector3> onHitAction)
    {

        this.enemeyTagList = enemeyTagList;
        this.onHitAction = onHitAction;
        collider.enabled = false;
    }

    public virtual void StartSkillHit()
    {
        Debug.Log("触发trigger");
        collider.enabled = true;
    }

    public virtual void StopSkillHit()
    {
        collider.enabled = false;
        enemyList.Clear();
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        // 1. 如果碰到的是对方的武器（通常带有 Weapon_Controller 脚本），直接忽略，让它继续穿透去打身体
        if(other.GetComponent<Weapon_Controller>() != null) return;
        //敌人或者玩家之间任意有一个是触发器，就会进入该方法
        if(enemeyTagList == null) return;
        if(enemeyTagList.Contains(other.tag))
        {
            IHurt enemy = other.GetComponentInParent<IHurt>();
            //防止重复攻击
            if(enemy != null && !enemyList.Contains(enemy))
            {
                onHitAction?.Invoke(enemy, other.ClosestPoint(transform.position));
                enemyList.Add(enemy);
            }
        }
    }
}
