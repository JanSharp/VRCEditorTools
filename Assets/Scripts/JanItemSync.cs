﻿
#define ItemSyncDebug

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

// TODO: desktop item rotation support (much easier said than done)

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class JanItemSync : UdonSharpBehaviour
{
    #if ItemSyncDebug
    [HideInInspector] public int debugIndex;
    [HideInInspector] public int debugNonIdleIndex;
    private DebugController debugController;
    #endif

    // set on Start
    private UpdateManager updateManager;
    private VRC_Pickup pickup;
    // NOTE: VRCPlayerApi.GetBoneTransform is not exposed so we have to use a dummy transform and teleport it around
    // because InverseTransformDirection and TransformDirection require an instance of a Transform
    private Transform dummyTransform;

    private const byte IdleState = 0; // the only state with CustomUpdate deregistered
    private const byte VRWaitingForConsistentOffsetState = 1;
    private const byte VRSendingState = 2; // attached to hand
    private const byte DesktopWaitingForHandToMoveState = 3;
    private const byte DesktopWaitingForConsistentOffsetState = 4;
    private const byte DesktopSendingState = 5; // attached to hand
    private const byte ExactSendingState = 6; // attached to hand
    private const byte ReceivingFloatingState = 7;
    private const byte ReceivingMovingToBoneState = 8; // attached to hand, but interpolating offset towards the actual attached position
    private const byte ReceivingAttachedState = 9; // attached to hand
    private byte state = IdleState;
    #if ItemSyncDebug
    public
    #else
    private
    #endif
    byte State
    {
        get => state;
        set
        {
            if (state != value)
            {
                #if ItemSyncDebug
                Debug.Log($"Switching from {StateToString(state)} to {StateToString(value)}.");
                if (debugController != null)
                {
                    if (state == IdleState)
                        debugController.RegisterNonIdle(this);
                    else if (value == IdleState)
                        debugController.DeregisterNonIdle(this);
                }
                #endif
                if (value == IdleState)
                    updateManager.Deregister(this);
                else if (state == IdleState)
                    updateManager.Register(this);
                state = value;
                #if ItemSyncDebug
                debugController.UpdateItemStatesText();
                #endif
            }
        }
    }
    #if ItemSyncDebug
    public
    #else
    private
    #endif
    string StateToString(byte state)
    {
        switch (state)
        {
            case IdleState:
                return "IdleState";
            case VRWaitingForConsistentOffsetState:
                return "VRWaitingForConsistentOffsetState";
            case VRSendingState:
                return "VRSendingState";
            case DesktopWaitingForHandToMoveState:
                return "DesktopWaitingForHandToMoveState";
            case DesktopWaitingForConsistentOffsetState:
                return "DesktopWaitingForConsistentOffsetState";
            case DesktopSendingState:
                return "DesktopSendingState";
            case ExactSendingState:
                return "ExactSendingState";
            case ReceivingFloatingState:
                return "ReceivingFloatingState";
            case ReceivingMovingToBoneState:
                return "ReceivingMovingToHandState";
            case ReceivingAttachedState:
                return "ReceivingAttachedState";
            default:
                return "InvalidState";
        }
    }

    ///<summary>
    ///First bit being 1 indicates the item is attached.
    ///Second bit is used when attached, 0 means attached to right hand, 1 means left hand.
    ///</summary>
    [UdonSynced] private byte syncedFlags;
    [UdonSynced] private Vector3 syncedPosition;
    [UdonSynced] private Quaternion syncedRotation;
    // 29 bytes (1 + 12 + 16) worth of data, and we get 84 bytes as the byte count in OnPostSerialization. I'll leave it at that

    // attachment data for both sending and receiving
    private VRCPlayerApi attachedPlayer;
    private HumanBodyBones attachedBone;
    private Vector3 attachedLocalOffset;
    private Quaternion attachedRotationOffset;

    // VRWaitingForConsistentOffsetState and DesktopWaitingForConsistentOffsetState
    #if ItemSyncDebug
    [HideInInspector] public float SmallMagnitudeDiff = 0.005f; // asdf
    [HideInInspector] public float SmallAngleDiff = 5f; // asdf
    [HideInInspector] public float ConsistentOffsetDuration = 0.3f; // asdf
    [HideInInspector] public int ConsistentOffsetFrameCount = 4; // asdf
    #else
    private const float SmallMagnitudeDiff = 0.005f;
    private const float SmallAngleDiff = 5f;
    private const float ConsistentOffsetDuration = 0.3f;
    private const int ConsistentOffsetFrameCount = 4;
    #endif
    private Vector3 prevPositionOffset;
    private Quaternion prevRotationOffset;
    private float consistentOffsetStopTime;
    private int stillFrameCount; // to prevent super low framerate from causing false positives

    // DesktopWaitingForHandToMoveState
    #if ItemSyncDebug
    [HideInInspector] public float HandMovementAngleDiff = 20f; // asdf
    #else
    private const float HandMovementAngleDiff = 20f;
    #endif
    private Quaternion initialBoneRotation;

    // ExactSendingState
    // NOTE: these really should be static fields, but UdonSharp 0.20.3 does not support them
    private Quaternion gripRotationOffset = Quaternion.Euler(0, 35, 0);
    private Quaternion gunRotationOffset = Quaternion.Euler(0, 305, 0);

    // ReceivingFloatingState and AttachedInterpolationState
    #if ItemSyncDebug
    [HideInInspector] public float InterpolationDuration = 0.2f; // asdf
    #else
    private const float InterpolationDuration = 0.2f;
    #endif
    private Vector3 posInterpolationDiff;
    private Quaternion interpolationStartRotation;
    private float interpolationStartTime;

    // for the update manager
    private int customUpdateInternalIndex;

    // properties for my laziness
    private Vector3 ItemPosition => this.transform.position;
    private Quaternion ItemRotation => this.transform.rotation;
    private Vector3 AttachedBonePosition => attachedPlayer.GetBonePosition(attachedBone);
    private Quaternion AttachedBoneRotation => attachedPlayer.GetBoneRotation(attachedBone);

    private void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        Debug.Assert(pickup != null, "JanItemSync must be on a GameObject with a VRC_Pickup component.");
        var updateManagerObj = GameObject.Find("/UpdateManager");
        if (updateManagerObj != null)
            updateManager = (UpdateManager)updateManagerObj.GetComponent(typeof(UdonBehaviour));
        Debug.Assert(updateManager != null, "JanItemSync requires a GameObject that must be at the root of the scene"
            + " with the exact name 'UpdateManager' which has the 'UpdateManager' UdonBehaviour."
        );
        dummyTransform = updateManagerObj.transform;

        #if ItemSyncDebug
        dummyTransform.GetComponent<MeshRenderer>().enabled = true;
        var debugControllerObj = GameObject.Find("/DebugController");
        if (debugControllerObj != null)
            debugController = (DebugController)debugControllerObj.GetComponent(typeof(UdonBehaviour));
        if (debugController != null)
            debugController.Register(this);
        #endif
    }

    private void MoveDummyToBone()
        => dummyTransform.SetPositionAndRotation(AttachedBonePosition, AttachedBoneRotation);
    private Vector3 GetLocalPositionToTransform(Transform transform, Vector3 worldPosition)
        => transform.InverseTransformDirection(worldPosition - transform.position);
    private Vector3 GetLocalPositionToBone(Vector3 worldPosition)
        => GetLocalPositionToTransform(dummyTransform, worldPosition);
    private Quaternion GetLocalRotationToTransform(Transform transform, Quaternion worldRotation)
        => Quaternion.Inverse(transform.rotation) * worldRotation;
    private Quaternion GetLocalRotationToBone(Quaternion worldRotation)
        => GetLocalRotationToTransform(dummyTransform, worldRotation);
    private bool IsReceivingState() => State >= ReceivingFloatingState;
    private bool IsAttachedSendingState() => State == VRSendingState || State == DesktopSendingState || State == ExactSendingState;

    public override void OnPickup()
    {
        if (!pickup.IsHeld)
            Debug.LogError("Picked up but not held?!");
        if (pickup.currentHand == VRC_Pickup.PickupHand.None)
            Debug.LogError("Held but not in either hand?!");

        attachedPlayer = pickup.currentPlayer;
        attachedBone = pickup.currentHand == VRC_Pickup.PickupHand.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

        // technically redundant because the VRCPickup script already does this, but I do not trust it nor do I trust order of operation
        Networking.SetOwner(attachedPlayer, this.gameObject);

        // if (pickup.orientation == VRC_Pickup.PickupOrientation.Gun)
        // {
        //     if (HandleExactOffset(pickup.ExactGun, gunRotationOffset))
        //         return;
        // }
        // else if (pickup.orientation == VRC_Pickup.PickupOrientation.Grip)
        // {
        //     if (HandleExactOffset(pickup.ExactGrip, gripRotationOffset))
        //         return;
        // }

        if (attachedPlayer.IsUserInVR())
        {
            prevPositionOffset = GetLocalPositionToBone(ItemPosition);
            prevRotationOffset = GetLocalRotationToBone(ItemRotation);
            stillFrameCount = 0;
            State = VRWaitingForConsistentOffsetState;
            consistentOffsetStopTime = Time.time + ConsistentOffsetDuration;
        }
        else
        {
            initialBoneRotation = AttachedBoneRotation;
            State = DesktopWaitingForHandToMoveState;
        }
    }

    private bool HandleExactOffset(Transform exact, Quaternion rotationOffset)
    {
        if (exact == null)
            return false;
        // figure out offset from exact transform to center of object
        // this is pretty much copied from CyanEmu, except it doesn't work
        // either I'm stupid and too tired to see it or - what I actually believe - I have to do it differently
        // what's great is that there is basically zero documentation about manipulation of quaternions. Yay!
        // TODO: fix exact offsets
        attachedRotationOffset = rotationOffset * Quaternion.Inverse(GetLocalRotationToTransform(exact, ItemRotation));
        attachedLocalOffset = attachedRotationOffset * GetLocalPositionToTransform(exact, ItemPosition);
        SendChanges();
        State = ExactSendingState;
        return true;
    }

    public override void OnDrop()
    {
        // if we already switched to receiving state before this player dropped this item don't do anything
        if (IsReceivingState())
            return;
        State = IdleState;
        SendChanges();
        #if ItemSyncDebug
        dummyTransform.SetPositionAndRotation(ItemPosition, ItemRotation);
        #endif
    }

    public void CustomUpdate()
    {
        if (State == IdleState)
        {
            Debug.LogError($"It should truly be impossible for CustomUpdate to run when an item is in IdleState. Item name: ${this.name}.");
            return;
        }
        if (IsReceivingState())
            UpdateReceiver();
        else
        {
            UpdateSender();
            #if ItemSyncDebug
            if (IsAttachedSendingState())
            {
                MoveDummyToBone();
                dummyTransform.SetPositionAndRotation(
                    AttachedBonePosition + dummyTransform.TransformDirection(attachedLocalOffset),
                    AttachedBoneRotation * attachedRotationOffset
                );
            }
            else
            {
                dummyTransform.SetPositionAndRotation(ItemPosition, ItemRotation);
            }
            #endif
        }
    }

    private bool ItemOffsetWasConsistent()
    {
        var posOffset = GetLocalPositionToBone(ItemPosition);
        var rotOffset = GetLocalRotationToBone(ItemRotation);
        #if ItemSyncDebug
        Debug.Log($"*WaitingForConsistentOffsetState: offset diff: {posOffset - prevPositionOffset}, "
            + $"offset diff magnitude {(posOffset - prevPositionOffset).magnitude}, "
            + $"angle diff: {Quaternion.Angle(rotOffset, prevRotationOffset)}.");
        #endif
        if ((posOffset - prevPositionOffset).magnitude <= SmallMagnitudeDiff
            && Quaternion.Angle(rotOffset, prevRotationOffset) <= SmallAngleDiff)
        {
            stillFrameCount++;
            #if ItemSyncDebug
            Debug.Log($"stillFrameCount: {stillFrameCount}, Time.time: {Time.time}, stop time: {consistentOffsetStopTime}.");
            #endif
            if (stillFrameCount >= ConsistentOffsetFrameCount && Time.time >= consistentOffsetStopTime)
            {
                #if ItemSyncDebug
                Debug.Log("Setting attached offset.");
                #endif
                attachedLocalOffset = posOffset;
                attachedRotationOffset = rotOffset;
                return true;
            }
        }
        else
        {
            #if ItemSyncDebug
            Debug.Log("Moved too much, resetting timer.");
            #endif
            stillFrameCount = 0;
            consistentOffsetStopTime = Time.time + ConsistentOffsetDuration;
        }

        prevPositionOffset = posOffset;
        prevRotationOffset = rotOffset;
        return false;
    }

    private void UpdateSender()
    {
        if (IsAttachedSendingState())
        {
            // I think this part still has to make sure the offset is about right, but we'll see
            // it'll definitely have to sync rotation in desktop mode, not sure if that's possible in VR
            // definitely nothing to do for ExactSendingState, it should probably not even be registered to Update anymore
            return;
        }

        MoveDummyToBone();

        if (State == VRWaitingForConsistentOffsetState)
        {
            if (ItemOffsetWasConsistent())
                State = VRSendingState;
        }
        else
        {
            if (State == DesktopWaitingForHandToMoveState)
            {
                #if ItemSyncDebug
                Debug.Log($"DesktopWaitingForHandToMoveState: angle diff: {Quaternion.Angle(AttachedBoneRotation, initialBoneRotation)}");
                #endif
                if (Quaternion.Angle(AttachedBoneRotation, initialBoneRotation) >= HandMovementAngleDiff)
                {
                    prevPositionOffset = GetLocalPositionToBone(ItemPosition);
                    prevRotationOffset = GetLocalRotationToBone(ItemRotation);
                    stillFrameCount = 0;
                    State = DesktopWaitingForConsistentOffsetState;
                    consistentOffsetStopTime = Time.time + ConsistentOffsetDuration;
                }
            }
            else
            {
                if (ItemOffsetWasConsistent())
                    State = DesktopSendingState;
            }
        }
        SendChanges(); // regardless of what happened, it has to sync
    }

    private void MoveItemToBoneWithOffset(Vector3 offset, Quaternion rotationOffset)
    {
        var bonePos = AttachedBonePosition;
        var boneRotation = AttachedBoneRotation;
        this.transform.SetPositionAndRotation(bonePos, boneRotation);
        this.transform.SetPositionAndRotation(
            bonePos + this.transform.TransformDirection(offset),
            boneRotation * rotationOffset
        );
    }

    private void UpdateReceiver()
    {
        // prevent this object from being moved by this logic if the local player is holding it
        // we might not have gotten the OnPickup event before the (this) Update event yet
        // not sure if that's even possible, but just in case
        if (pickup.IsHeld)
            return;

        if (State == ReceivingAttachedState)
            MoveItemToBoneWithOffset(attachedLocalOffset, attachedRotationOffset);
        else
        {
            var percent = (Time.time - interpolationStartTime) / InterpolationDuration;
            if (State == ReceivingFloatingState)
            {
                if (percent >= 1f)
                {
                    this.transform.SetPositionAndRotation(syncedPosition, syncedRotation);
                    State = IdleState;
                }
                else
                {
                    this.transform.SetPositionAndRotation(
                        syncedPosition - posInterpolationDiff * (1f - percent),
                        Quaternion.Lerp(interpolationStartRotation, syncedRotation, percent)
                    );
                }
            }
            else
            {
                if (percent >= 1f)
                {
                    MoveItemToBoneWithOffset(attachedLocalOffset, attachedRotationOffset);
                    State = ReceivingAttachedState;
                }
                else
                {
                    MoveItemToBoneWithOffset(
                        attachedLocalOffset - posInterpolationDiff * (1f - percent),
                        Quaternion.Lerp(interpolationStartRotation, attachedRotationOffset, percent)
                    );
                }
            }
        }
    }

    private void SendChanges()
    {
        RequestSerialization();
    }

    public override void OnPreSerialization()
    {
        if (IsReceivingState())
        {
            Debug.LogWarning("// TODO: uh idk what to do, shouldn't this be impossible?");
        }
        syncedFlags = 0;
        if (IsAttachedSendingState())
        {
            syncedFlags += 1; // set attached flag
            syncedPosition = attachedLocalOffset;
            syncedRotation = attachedRotationOffset;
            if (attachedBone == HumanBodyBones.LeftHand)
                syncedFlags += 2; // set left hand flag, otherwise it's right hand
        }
        else
        {
            // not attached, don't set the attached flag and just sync current position and rotation
            syncedPosition = ItemPosition;
            syncedRotation = ItemRotation;
        }
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        if (!result.success)
        {
            Debug.LogWarning($"Syncing request was dropped for {this.name}, trying again.");
            SendChanges(); // TODO: somehow test if this kind of retry even works or if the serialization request got reset right afterwards
        }
        // else
        //     Debug.Log($"Sending {result.byteCount} bytes");
    }

    public override void OnDeserialization()
    {
        if (State != IdleState && pickup.IsHeld) // did someone steal the item?
            pickup.Drop(); // drop it

        bool isAttached = (syncedFlags & 1) != 0;
        if (pickup.DisallowTheft)
            pickup.pickupable = !isAttached;

        if (isAttached)
        {
            attachedBone = (syncedFlags & 2) != 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            attachedLocalOffset = syncedPosition;
            attachedRotationOffset = syncedRotation;
            attachedPlayer = Networking.GetOwner(this.gameObject); // ensure it is up to date
            if (State != ReceivingAttachedState)
            {
                MoveDummyToBone();
                posInterpolationDiff = attachedLocalOffset - GetLocalPositionToBone(ItemPosition);
                interpolationStartRotation = GetLocalRotationToBone(ItemRotation);
                interpolationStartTime = Time.time;
                State = ReceivingMovingToBoneState;
            }
        }
        else // not attached
        {
            posInterpolationDiff = syncedPosition - ItemPosition;
            interpolationStartRotation = ItemRotation;
            interpolationStartTime = Time.time;
            State = ReceivingFloatingState;
        }
    }
}
