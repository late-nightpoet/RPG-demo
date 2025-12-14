using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Controller : MonoBehaviour
{
    [SerializeField] private new Collider collider;

    private List<string> enemeyTagList;
    private List<IHurt> enemyList = new List<IHurt>();

    private Action<IHurt, Vector3> onHitAction;

    public void Init(List<string> enemeyTagList, Action<IHurt, Vector3> onHitAction)
    {
        collider.enabled = false;
        this.enemeyTagList = enemeyTagList;
        this.onHitAction = onHitAction;
    }

    public void StartSkillHit()
    {
        collider.enabled = true;
    }

    public void StopSkillHit()
    {
        collider.enabled = false;
        enemyList.Clear();
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
                onHitAction?.Invoke(enemy, other.ClosestPoint(transform.position));
                enemyList.Add(enemy);
            }
        }
    }
}
