using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MonoBehaviour 事件监听器组件，可在 Inspector 中配置
/// </summary>
public class EventListener : MonoBehaviour
{
    public GameEvent listenEvent;
    public UnityEvent response;

    void OnEnable()
    {
        EventBus.Subscribe(listenEvent, OnEventRaised);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(listenEvent, OnEventRaised);
    }

    void OnEventRaised()
    {
        response?.Invoke();
    }
}
