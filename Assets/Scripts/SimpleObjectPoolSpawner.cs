﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleObjectPoolSpawner : UdonSharpBehaviour
{
    public SimpleObjectPool objectPool;

    // public override void Interact()
    // {
    //     Networking.SetOwner(Networking.LocalPlayer, objectPool.gameObject);
    //     objectPool.TryToSpawn();
    // }
}