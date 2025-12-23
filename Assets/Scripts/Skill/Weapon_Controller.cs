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
        collider.enabled = false;
        this.enemeyTagList = enemeyTagList;
        this.onHitAction = onHitAction;
        meleeWeaponTrail.Emit = false;
    }

    public void StartSkillHit()
    {
        Debug.Log("Weapon Skill Hit Started");
        collider.enabled = true;
        meleeWeaponTrail.Emit = true;
    }

    public void StopSkillHit()
    {
        Debug.Log("Weapon Skill Hit Stopped");
        collider.enabled = false;
        enemyList.Clear();
        meleeWeaponTrail.Emit = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if(enemeyTagList.Contains(other.tag))
        {
            IHurt enemy = other.GetComponentInParent<IHurt>();
            //防止重复攻击
            if(enemy != null && !enemyList.Contains(enemy))
            {
                Debug.Log("Enemy Hit!");
                Debug.Log("ontriggerstay collider is " + collider.name);
                onHitAction?.Invoke(enemy, other.ClosestPoint(transform.position));
                enemyList.Add(enemy);
            }
        }
    }
}
