using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using R3;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

public static class CustomJobUtilities
{
    public static async Task<TReturnType> ScheduleFancyJob<TReturnType, T>(this T job,
        JobHandle dependsOn = default(JobHandle))
        where T : struct, IFancyJob<TReturnType>, IJob
    {
        var completionSource = new TaskCompletionSource<TReturnType>();

        job.OnBeforeSchedule();
        var handle = job.Schedule(dependsOn);

        var disposable = Observable.EveryUpdate()
            .Where(_ => handle.IsCompleted)
            .Take(1)
            .Subscribe(
                (_) =>
                {
                    handle.Complete();
                    var data = job.CompleteAndReturn();
                    completionSource.TrySetResult(data);
                });

        using (disposable)
        {
            return await completionSource.Task.ConfigureAwait(false);
        }
    }
}