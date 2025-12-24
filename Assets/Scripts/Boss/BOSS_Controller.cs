using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BOSS_Controller : MonoBehaviour, IHurt
{
    public void Hurt()
    {
        Debug.Log("BOSS受到攻击！");
    }
}
