using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public interface IFancyJob<out TReturnType> : IJob
{
    public void OnBeforeSchedule();
    public TReturnType CompleteAndReturn();
}