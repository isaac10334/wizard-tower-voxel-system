using System.Collections;
using System.Collections.Generic;
using CharacterCore.Controller;
using FishNet.Object;
using FOVCharacter;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [NotNull] public GameEvent onLocalPlayerSpawned;
    [NotNull] public PhysicsCharacterController physicsCharacterController;
    [NotNull] public BoxCharacterController boxCharacterController;
    [NotNull] public FOVCharacterController fovCharacterController;
    [NotNull] public GameObject playerCamera;
    [NotNull] public GameObject playerBody;

    public override void OnStartClient()
    {
        if (IsOwner)
        {
            physicsCharacterController.enabled = true;
            boxCharacterController.enabled = true;
            fovCharacterController.enabled = true;
            playerCamera.SetActive(true);
            playerBody.SetActive(false);
            onLocalPlayerSpawned.Raise();
        }
        else
        {
            physicsCharacterController.enabled = false;
            boxCharacterController.enabled = false;
            fovCharacterController.enabled = false;
            playerCamera.SetActive(false);
            playerBody.SetActive(true);
        }
    }
}