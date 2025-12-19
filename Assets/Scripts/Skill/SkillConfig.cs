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


/// <summary>
/// 多段攻击中的一个攻击单位
/// </summary>
[Serializable]
public class Skill_AttackData
{
    //播放粒子/产生的游戏物体，例如攻击时播放的刀光
    public Skill_SpawnObj SpawnObj;

    //攻击音效
    public AudioClip  AudioClip;

    //命中数据
    //伤害值
    public float DamgeValue;

    //硬直时间
    public float HardTime;

    //击退、击飞的距离方向
    public Vector3 RepelVelocity;

    //击退、击飞的过渡时间长度
    public float RepelTime;

    //屏幕震动效果值
    public float ScreenImpulseValue;

    //色差效果值
    public float ChromaticAberrationValue;

    //命中效果
    public SkillHitEFConfig SkillHitEFConfig;
}
