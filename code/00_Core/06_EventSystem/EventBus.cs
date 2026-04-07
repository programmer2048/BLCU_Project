using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 发布-订阅事件总线，所有模块通过此类通信，实现解耦
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<GameEvent, Delegate> _eventTable = new Dictionary<GameEvent, Delegate>();

    // ── 无参数事件 ──
    public static void Subscribe(GameEvent evt, Action callback)
    {
        if (!_eventTable.ContainsKey(evt)) _eventTable[evt] = null;
        _eventTable[evt] = (Action)_eventTable[evt] + callback;
    }

    public static void Unsubscribe(GameEvent evt, Action callback)
    {
        if (_eventTable.ContainsKey(evt))
        {
            _eventTable[evt] = (Action)_eventTable[evt] - callback;
            if (_eventTable[evt] == null) _eventTable.Remove(evt);
        }
    }

    public static void Publish(GameEvent evt)
    {
        if (_eventTable.TryGetValue(evt, out var d))
            ((Action)d)?.Invoke();
    }

    // ── 单参数事件 ──
    public static void Subscribe<T>(GameEvent evt, Action<T> callback)
    {
        if (!_eventTable.ContainsKey(evt)) _eventTable[evt] = null;
        _eventTable[evt] = (Action<T>)_eventTable[evt] + callback;
    }

    public static void Unsubscribe<T>(GameEvent evt, Action<T> callback)
    {
        if (_eventTable.ContainsKey(evt))
        {
            _eventTable[evt] = (Action<T>)_eventTable[evt] - callback;
            if (_eventTable[evt] == null) _eventTable.Remove(evt);
        }
    }

    public static void Publish<T>(GameEvent evt, T arg)
    {
        if (_eventTable.TryGetValue(evt, out var d))
            ((Action<T>)d)?.Invoke(arg);
    }

    // ── 双参数事件 ──
    public static void Subscribe<T1, T2>(GameEvent evt, Action<T1, T2> callback)
    {
        if (!_eventTable.ContainsKey(evt)) _eventTable[evt] = null;
        _eventTable[evt] = (Action<T1, T2>)_eventTable[evt] + callback;
    }

    public static void Unsubscribe<T1, T2>(GameEvent evt, Action<T1, T2> callback)
    {
        if (_eventTable.ContainsKey(evt))
        {
            _eventTable[evt] = (Action<T1, T2>)_eventTable[evt] - callback;
            if (_eventTable[evt] == null) _eventTable.Remove(evt);
        }
    }

    public static void Publish<T1, T2>(GameEvent evt, T1 arg1, T2 arg2)
    {
        if (_eventTable.TryGetValue(evt, out var d))
            ((Action<T1, T2>)d)?.Invoke(arg1, arg2);
    }

    /// <summary>清空所有事件（场景切换时调用）</summary>
    public static void Clear()
    {
        _eventTable.Clear();
    }
}
