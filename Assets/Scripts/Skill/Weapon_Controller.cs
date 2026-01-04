using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Controller : MonoBehaviour
{
    [SerializeField] private new Collider collider;
    [SerializeField] MeleeWeaponTrail meleeWeaponTrail;

    private List<string> enemeyTagList;
    private List<IHurt> enemyList = new List<IHurt>();

    private Action<IHurt, Vector3> onHitAction;

    public void Init(List<string> enemeyTagList, Action<IHurt, Vector3> onHitAction)
    {
        //要让武器在一般情况下是collider，不会穿模，在攻击时是trigger会穿模
        collider.isTrigger = false;
        this.enemeyTagList = enemeyTagList;
        this.onHitAction = onHitAction;
        meleeWeaponTrail.Emit = false;
    }

    public void StartSkillHit()
    {
        collider.isTrigger = true;
        meleeWeaponTrail.Emit = true;
    }

    public void StopSkillHit()
    {
        collider.isTrigger = false;
        enemyList.Clear();
        meleeWeaponTrail.Emit = false;
    }

    private void OnTriggerStay(Collider other)
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
                Debug.Log("Enemy Hit!");
                Debug.Log("ontriggerstay collider is " + other.name);
                onHitAction?.Invoke(enemy, other.ClosestPoint(transform.position));
                enemyList.Add(enemy);
            }
        }
    }
}
