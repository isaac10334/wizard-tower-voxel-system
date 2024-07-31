using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using VoxelSystem;

public class GameManager : NetworkBehaviour
{
    public static NetworkObject LocalPlayer;
    public NetworkObject playerPrefab;

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        RpcSpawnPlayer();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcSpawnPlayer(NetworkConnection conn = null)
    {
        if (conn == null) Debug.LogError("conn null??");

        var player = Instantiate(playerPrefab);
        Spawn(player, conn);

        conn.SetFirstObject(player);

        player.GiveOwnership(conn);
        TargetOnPlayerCreated(conn, player);
    }

    [TargetRpc]
    private void TargetOnPlayerCreated(NetworkConnection conn, NetworkObject player)
    {
        LocalPlayer = player;
        InstanceFinder.ClientManager.Connection.SetFirstObject(player);
    }
}