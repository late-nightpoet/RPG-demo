using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public abstract class CharacterBase : MonoBehaviour, IStateMachineOwner, ISkillOwner, IHurt
{
    [SerializeField]protected ModelBase model;
    public ModelBase Model { get { return model; } }

    public Transform ModelTransform => Model.transform;

    [SerializeField]protected CharacterController characterController;
    public CharacterController CharacterController { get { return characterController; } }

    public StateMachine stateMachine { get; private set; }

    [SerializeField]protected AudioSource audioSource;

    public AudioClip[] footStepAudioClips;

    public List<string> enemeyTagList;
    public SkillConfig[] standAttackConfigs;
    //技能的配置
    public List<SkillInfo> skillInfoList = new List<SkillInfo>();

    public Image HPFillImage;
    [SerializeField] protected float maxHP;

    protected float currentHP;

    protected float CurrentHP 
    { 
        get => currentHP; 
        set { 
            currentHP = value;
            if(currentHP < 0) 
            {
                currentHP = 0;
            //todo 弹出死亡提示框} 
            }
            else HPFillImage.fillAmount = currentHP / maxHP;
        }
    }



    public virtual void Init()
    {
        CurrentHP = maxHP;
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
        yield return new WaitForSeconds(spawnObj.Delay);
        //之所以不设置为相对父物体是因为父物体是Player,而旋转时旋转的是Player旗下的模型，而player本身并不会旋转
        GameObject skillObj = GameObject.Instantiate(spawnObj.Prefab, null);
        //设置相对于技能释放者所在的位置以及旋转
        //需要加上相对局部的坐标，免得角色转向，但是特效依旧在原本的方向生成
        skillObj.transform.position = Model.transform.position + Model.transform.TransformDirection(spawnObj.Position);
        skillObj.transform.localScale = spawnObj.Scale;
        skillObj.transform.eulerAngles = Model.transform.eulerAngles + spawnObj.Rotation;
        PlayAudio(spawnObj.AudioClip);

        // 查找是否有技能物体，如果有的话进行初始化
        if(skillObj.TryGetComponent<SkillObjectBase>(out SkillObjectBase skillObject))
        {
            skillObject.Init(enemeyTagList, OnHitForRealseData);
        }
    }

    //远程攻击时释放特效
    public virtual void OnHitForRealseData(IHurt target, Vector3 hitPostion)
    {
 
        // 拿到这一段攻击的数据
        Skill_AttackData attackData = CurrentSkillConfig.ReleaseData.AttackData;

        if (attackData == null)
        {
            Debug.LogError($"[CharacterBase] OnHitForRealseData: AttackData is null for skill '{CurrentSkillConfig.name}'. Please check the SkillConfig asset's ReleaseData.");
            return;
        }

        if (attackData.SkillHitEFConfig != null)
        {
            PlayAudio(attackData.SkillHitEFConfig.AudioClip); //通用音效
        }

        // 传递伤害数据
        if(target.Hurt(attackData.HitData, this))
        {
            // 生成基于命中配置的效果
            if (attackData.SkillHitEFConfig != null) StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig.SpawnObject, hitPostion));
            StartFreezeFrame(attackData.FreezeFrameTime);
            StartFreezeTime(attackData.FreezeGameTime);
        }
        else
        {
            //生成类似只狼里面格挡的刀光效果
            if (attackData.SkillHitEFConfig != null) StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig.FailSpawnObject, hitPostion));
        }

    }

    public virtual void OnHit(IHurt target, Vector3 hitPostion)
    {
 
        // 拿到这一段攻击的数据
        Skill_AttackData attackData = CurrentSkillConfig.AttackData[currentHitIndex];
        PlayAudio(attackData.SkillHitEFConfig.AudioClip);//通用音效
        // 传递伤害数据
        if(target.Hurt(attackData.HitData, this))
        {
            // 生成基于命中配置的效果
            StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig.SpawnObject, hitPostion));
            StartFreezeFrame(attackData.FreezeFrameTime);
            StartFreezeTime(attackData.FreezeGameTime);
        }
        else
        {
            //生成类似只狼里面格挡的刀光效果
            StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig.FailSpawnObject, hitPostion));
        }

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

    protected IEnumerator DoSkillHitEF(Skill_SpawnObj spawnObj, Vector3 spawnPoint)
    {
        //修改DoSkillHitEF使其能够通用生成failspawnobj和spawnobj
        Debug.Log("DoSkillHitEF");
        if(spawnObj == null) yield break;
        if(spawnObj != null && spawnObj.Prefab != null)
        {
            Debug.Log("DoSkillHitEF hitEFConfig.SpawnObject");
            yield return new WaitForSeconds(spawnObj.Delay);
            GameObject temp = Instantiate(spawnObj.Prefab);
            temp.transform.position = spawnPoint + spawnObj.Position;
            //一般情况下，效果需要朝向镜头显示
            temp.transform.LookAt(Camera.main.transform);
            temp.transform.eulerAngles += spawnObj.Rotation;
            temp.transform.localScale += spawnObj.Scale;
            PlayAudio(spawnObj.AudioClip);
        }
    }

    public void OnSkillOver()
    {
        canSwitchSkill = true;
    }
    #endregion

    public virtual void SetHurtData(Skill_HitData hitData, ISkillOwner hurtSource)
    {
        this.hitData = hitData;
        this.hurtSource = hurtSource;
    }

    public abstract bool Hurt(Skill_HitData hitData, ISkillOwner hurtSource);

    private string currentAnimationName;
   
     public void PlayAnimation(string animationName, bool reState = true,float fixedTransitionDuration = 0.25f)
    {
        //如果新播放的动画与上一次动画相同，并且不需要重置状态就提前返回
        if(currentAnimationName == animationName && !reState)
        {
            return;
        }
        currentAnimationName = animationName;
        Model.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration);
    
    }

    public void PlayAnimation(string animationName, float fixedTransitionDuration, float normalizedTimeOffset, int layer = 0)
    {
        if (Model.Animator == null) return;
        Model.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration, layer, normalizedTimeOffset);
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

    public virtual void UpdateHP(Skill_HitData hitData)
    {
        CurrentHP -= hitData.DamgeValue;
    }
}
