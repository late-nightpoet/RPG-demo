using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateBase
{
    /// <summary>
    /// 初始化状态
    /// </summary>
    /// <param name="owner">宿主</param>
    public virtual void Init(IStateMachineOwner owner){}

    public virtual void UnInit(){}

    public virtual void Enter(){}

    public virtual void Exit(){}

    public virtual void Update(){}

    public virtual void FixedUpdate(){}

    public virtual void LateUpdate(){}
}
