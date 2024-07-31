using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventListener : MonoBehaviour
{
    public GameEvent gameEvent;
    public UnityEvent onEventRaised;

    private void Awake()
    {
        gameEvent.AddListener(() => onEventRaised.Invoke());
    }
}