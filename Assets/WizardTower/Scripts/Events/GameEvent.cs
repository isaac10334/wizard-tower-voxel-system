using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    public Action gameEvent;
    public void Raise() => gameEvent?.Invoke();
    public void AddListener(Action a) => gameEvent += a;
}