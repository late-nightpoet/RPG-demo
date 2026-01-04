using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBase : MonoBehaviour, IStateMachineOwner, ISkillOwner, IHurt
{
    [SerializeField]protected ModelBase model;
    public ModelBase Model { get { return model; } }

    public Transform ModelTransform => Model.transform;

    [SerializeField]protected CharacterController characterController;
    public CharacterController CharacterController { get { return characterController; } }

    protected StateMachine stateMachine;

    [SerializeField]protected AudioSource audioSource;

    public AudioClip[] footStepAudioClips;

    public List<string> enemeyTagList;
    public SkillConfig[] standAttackConfigs;

    public virtual void Init()
    {
        stateMachine = new StateMachine();
        stateMachine.Init(this);
        Model.Init(OnFootStep,this, enemeyTagList);
        canSwitchSkill = true;
    }

   

    #region  攻击技能
    public Skill_HitData hitData { get; protected set; }
    public ISkillOwner hurtSource { get; protected set; }
    public SkillConfig currentSkillConfig;
    protected int currentHitIndex = 0;
    //切换技能，主要用于判定前摇和后摇
    private bool canSwitchSkill;

    public bool CanSwitchSkill{ get=> canSwitchSkill;}
    public SkillConfig CurrentSkillConfig{ get=> currentSkillConfig;}

    public void StartAttack(SkillConfig skillConfig)
    {
        canSwitchSkill = false; //防止玩家立刻播放下一个技能
        currentSkillConfig = skillConfig;
        currentHitIndex = 0;
        PlayAnimation(currentSkillConfig.AnimationName);

        SpawnSkillObject(skillConfig.ReleaseData.SpawnObj);
        PlayAudio(currentSkillConfig.ReleaseData.AudioClip);
    }

    public void StartSkillHit(int weaponIndex)
    {
        SpawnSkillObject(currentSkillConfig.AttackData[currentHitIndex].SpawnObj);
        PlayAudio(currentSkillConfig.AttackData[currentHitIndex].AudioClip);
    }

    public void StopSkillHit(int weaponIndex)
    {
        currentHitIndex += 1;
    }



    public void SkillCanSwitch()
    {
        canSwitchSkill = true;
    }

    public void SpawnSkillObject(Skill_SpawnObj spawnObj)
    {
        if(spawnObj != null && spawnObj.Prefab != null)
        {
            StartCoroutine(DoSpawnObject(spawnObj));
        }
        
    }

    protected IEnumerator DoSpawnObject(Skill_SpawnObj spawnObj)
    {
        //先执行延迟事件
        yield return new WaitForSeconds(spawnObj.Time);
        //之所以不设置为相对父物体是因为父物体是Player,而旋转时旋转的是Player旗下的模型，而player本身并不会旋转
        GameObject skillObj = GameObject.Instantiate(spawnObj.Prefab, null);
        //设置相对于技能释放者所在的位置以及旋转
        //需要加上相对局部的坐标，免得角色转向，但是特效依旧在原本的方向生成
        skillObj.transform.position = Model.transform.position + Model.transform.TransformDirection(spawnObj.Position);
        skillObj.transform.localScale = spawnObj.Scale;
        skillObj.transform.eulerAngles = Model.transform.eulerAngles + spawnObj.Rotation;
        PlayAudio(spawnObj.AudioClip);
    }

    public virtual void OnHit(IHurt target, Vector3 hitPostion)
    {
        Debug.Log("this.name is " + this.name);
        Skill_AttackData attackData = currentSkillConfig.AttackData[currentHitIndex];
        StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig, hitPostion));
        StartFreezeFrame(attackData.FreezeFrameTime);
        StartFreezeTime(attackData.FreezeGameTime);
        //传递伤害数据
        //todo 传递更多伤害信息
        target.Hurt(attackData.HitData, this);
    }

    protected void StartFreezeFrame(float time)
    {
        if(time > 0) StartCoroutine(DoFreezeFrame(time));
    }
    protected IEnumerator DoFreezeFrame(float time)
    {
        Model.Animator.speed = 0;
        yield return new WaitForSeconds(time);
        Model.Animator.speed = 1;
    }

    protected void StartFreezeTime(float time)
    {
        if(time > 0) StartCoroutine(DoFreezeTime(time));
    }

    protected IEnumerator DoFreezeTime(float time)
    {
        Time.timeScale = 0;
        //防止timescale时停影响，需要使用真实的时间
        yield return new WaitForSecondsRealtime(time);
        Time.timeScale = 1;
    }

    protected IEnumerator DoSkillHitEF(SkillHitEFConfig hitEFConfig, Vector3 spawnPoint)
    {
        Debug.Log("DoSkillHitEF");
        if(hitEFConfig == null) yield break;
        PlayAudio(hitEFConfig.AudioClip);
        if(hitEFConfig.SpawnObject != null && hitEFConfig.SpawnObject.Prefab != null)
        {
            Debug.Log("DoSkillHitEF hitEFConfig.SpawnObject");
            yield return new WaitForSeconds(hitEFConfig.SpawnObject.Time);
            GameObject temp = Instantiate(hitEFConfig.SpawnObject.Prefab);
            temp.transform.position = spawnPoint + hitEFConfig.SpawnObject.Position;
            //一般情况下，效果需要朝向镜头显示
            temp.transform.LookAt(Camera.main.transform);
            temp.transform.eulerAngles += hitEFConfig.SpawnObject.Rotation;
            temp.transform.localScale += hitEFConfig.SpawnObject.Scale;
            PlayAudio(hitEFConfig.SpawnObject.AudioClip);
        }
    }

    public void OnSkillOver()
    {
        canSwitchSkill = true;
    }
    #endregion

    public virtual void Hurt(Skill_HitData hitData, ISkillOwner hurtSource)
    {
        this.hitData = hitData;
        this.hurtSource = hurtSource;
    }

     public void PlayAnimation(string animationName, float fixedTransitionDuration = 0.25f)
    {
        Model.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration);
    
    }

    public void PlayAudio(AudioClip audioClip)
    {
        if(audioClip != null)audioSource.PlayOneShot(audioClip);
    }

    public void OnFootStep()
    {
        if (footStepAudioClips.Length == 0) return;
        int index = UnityEngine.Random.Range(0, footStepAudioClips.Length);
        audioSource.PlayOneShot(footStepAudioClips[index]);
    }
}
