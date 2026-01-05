using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IStateMachineOwner
{
    
}
public class StateMachine
{
    private IStateMachineOwner owner;
    private StateBase currentState;

    public StateBase CurrentState { get=>currentState;}

    private Dictionary<Type, StateBase> stateDic = new Dictionary<Type, StateBase>();
    
    public Type CurrentStateType
    {
        get
        {
            if (currentState == null)
                return null;
            return currentState.GetType();
        }
    }

    public bool HasState {get { return currentState != null; } }

    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }

    public bool ChangeState<T>(bool reCurrstate = false) where T : StateBase, new()
    {
        Debug.Log($"StateMachine ChangeState to {typeof(T).Name}");
        //状态一致并且不需要刷新状态并且不需要切换
        if(HasState && CurrentStateType == typeof(T) && !reCurrstate)
            return false;
        //退出当前状态
        if (currentState != null)
        {
            currentState.Exit();
            MonoManager.Instance.RemoveUpdateListener(currentState.Update);
            MonoManager.Instance.RemoveFixedUpdateListener(currentState.FixedUpdate);  
            MonoManager.Instance.RemoveLateUpdateListener(currentState.LateUpdate);
        }

        //进入新状态
        currentState = GetState<T>();
        currentState.Enter();
        MonoManager.Instance.AddUpdateListener(currentState.Update);
        MonoManager.Instance.AddFixedUpdateListener(currentState.FixedUpdate);  
        MonoManager.Instance.AddLateUpdateListener(currentState.LateUpdate);
        return false;
    }

    private StateBase GetState<T>() where T : StateBase, new()
    {
        Type type = typeof(T);
        if (!stateDic.TryGetValue(type, out StateBase state))
        {
            state = new T();
            state.Init(owner);
            stateDic.Add(type, state);
        }
        return state;
    }

    public void Stop()
    {
        currentState?.Exit();
        MonoManager.Instance.RemoveUpdateListener(currentState.Update);
        MonoManager.Instance.RemoveFixedUpdateListener(currentState.FixedUpdate);  
        MonoManager.Instance.RemoveLateUpdateListener(currentState.LateUpdate);
        currentState = null;

        foreach (var state in stateDic.Values)
        {
            state.UnInit();
        }
        stateDic.Clear();
    }
}
