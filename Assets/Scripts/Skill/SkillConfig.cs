using System;
using UnityEngine;

[CreateAssetMenu(menuName ="Config/Skill")]
public class SkillConfig : ScriptableObject
{
    // 片段名称
    public string AnimationName;

    public Skill_ReleaseData ReleaseData;

    public Skill_AttackData[] AttackData;
}


/// <summary>
/// 技能释放配置数据
/// </summary>
[Serializable]
public class Skill_ReleaseData
{
    public Skill_SpawnObj SpawnObj;

    //技能释放时的音效，与产生物体时的音效不同
    public AudioClip AudioClip;
}

/// <summary>
/// 技能释放时生成的物体
/// </summary>
[Serializable]
public class Skill_SpawnObj
{
    //技能释放时会生成的预制体
    public GameObject Prefab;

    //物体生成时的产生音效
    public AudioClip AudioClip;

    //生成物体的位置（相较于技能释放者的偏移位置）
    public Vector3 Position;

    //生成物体的偏移位置
    public Vector3 Rotation;

    //延迟生成物体的时间
    public float Time;
}
[Serializable]
public class Skill_AttackData
{
    public Skill_SpawnObj SpawnObj;

    //攻击音效
    public AudioClip  AudioClip;
}
