using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Config/SkillHitEFConfig")]
public class SkillHitEFConfig : ScriptableObject
{
    //格挡失败时产生的粒子物体，例如血液等
    public Skill_SpawnObj SpawnObject;

    //通用的音效，无论是否对方格挡住都会播放的音效
    public AudioClip AudioClip;

    //格挡成功时（hit fail)产生的粒子物体，例如格挡的闪光等
    public Skill_SpawnObj FailSpawnObject;
}
