﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VFXTargetGunRecallManager : UdonSharpBehaviour
    {
        public VFXTargetGunVisibilityManager[] gunGroupsToManage;

        private VFXTargetGun FindGun(bool ensureGunGroupIsVisible)
        {
            VFXTargetGun firstUntouchedGun = null;
            VFXTargetGunVisibilityManager firstUntouchedGunGroup = null;
            VFXTargetGun lastHeldGun = null;
            VFXTargetGunVisibilityManager lastHeldGunGroup = null;
            var localPlayerId = Networking.LocalPlayer.playerId;
            foreach (var manager in gunGroupsToManage)
                foreach (var gun in manager.guns)
                {
                    if (float.IsNaN(gun.LastHeldTime)) // never been touched by anyone
                    {
                        if (firstUntouchedGun == null)
                        {
                            firstUntouchedGun = gun;
                            firstUntouchedGunGroup = manager;
                        }
                    }
                    else
                    {
                        if (gun.LastHoldingPlayerId == localPlayerId)
                        {
                            if (lastHeldGun == null || gun.LastHeldTime > lastHeldGun.LastHeldTime)
                            {
                                lastHeldGun = gun;
                                lastHeldGunGroup = manager;
                            }
                        }
                    }
                }
            var result = lastHeldGun != null ? lastHeldGun : firstUntouchedGun;
            if (ensureGunGroupIsVisible && result != null)
            {
                var group = lastHeldGunGroup != null ? lastHeldGunGroup : firstUntouchedGunGroup;
                group.SetVisible();
            }
            return result;
        }

        public void RecallGun()
        {
            var gun = FindGun(true);
            if (gun == null)
                return;
            var player = Networking.LocalPlayer;
            // if (player.GetPickupInHand(VRC_Pickup.PickupHand.Right) == null)
            //     player.SetPickupInHand(gun.Pickup, VRC_Pickup.PickupHand.Right);
            // else if (player.IsUserInVR() && player.GetPickupInHand(VRC_Pickup.PickupHand.Left) == null)
            //     player.SetPickupInHand(gun.Pickup, VRC_Pickup.PickupHand.Left);
            // else
            {
                var data = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
                var pickup = gun.Pickup; // UdonSharp being picky
                pickup.transform.position = data.position;
            }
        }
    }
}