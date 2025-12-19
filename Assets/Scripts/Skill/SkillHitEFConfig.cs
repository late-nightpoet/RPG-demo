using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Config/SkillHitEFConfig")]
public class SkillHitEFConfig : ScriptableObject
{
    //产生的粒子物体，例如血液等
    public Skill_SpawnObj SpawnObject;

    //命中时音效，例如刀戳进人体时的stab的声音
    public AudioClip AudioClip;
}
