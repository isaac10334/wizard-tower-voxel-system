using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

public static class CustomJobUtilities
{
    public static async Task<TReturnType> ScheduleFancyJob<TReturnType, T>(this T job,
        JobHandle dependsOn = default(JobHandle))
        where T : struct, IFancyJob<TReturnType>, IJob
    {
        job.OnBeforeSchedule();
        var handle = job.Schedule(dependsOn);

        while (!handle.IsCompleted)
        {
            await Task.Yield();
        }

        handle.Complete();
        return job.CompleteAndReturn();
    }
}