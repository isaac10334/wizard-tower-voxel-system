using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using VoxelSystem;

public class VoxelBehavior : NetworkBehaviour
{
    [ServerRpc]
    protected void RpcGetChunk(Region region, NetworkConnection conn = null)
    {
        var chunk = VoxelManager.Instance.GetChunk(region);
        RpcReceiveChunk(conn, chunk);
    }

    [TargetRpc]
    private void RpcReceiveChunk(NetworkConnection conn, Chunk chunk)
    {
        OnReceiveChunk(chunk);
    }

    protected virtual void OnReceiveChunk(Chunk chunk)
    {
    }

    // [Client]
    // public async Task<Mesh> GetMeshForRegion(Region region)
    // {
    //     // we asynchronously get the data from the server, then mesh it. easy.
    //     // you cannot call this on the client. the only way to use voxelworld from the client is to be inside a networkbehavior and make an RPC.
    //     var chunk = GetChunk(region);
    //
    //     switch (chunk.AreaInformation)
    //     {
    //         case RegionInformation.Unmodified:
    //             throw new NotImplementedException("todo generate data locally, then mesh and return");
    //         case RegionInformation.Modified:
    //             throw new NotImplementedException("todo generate a mesh from chunk data return that");
    //             var meshingJob = CubicGreedyMeshCreator.Create(chunk);
    //             return await meshingJob.ScheduleFancyJob<Mesh, CubicGreedyMeshCreator>();
    //         default:
    //             return null;
    //     }
    // }
}