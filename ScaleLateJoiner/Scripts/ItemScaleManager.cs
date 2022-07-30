﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UdonSharpEditor;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ItemScaleManager : UdonSharpBehaviour
    {
        public GameObject[] parentsForObjectPools;
        public GameObject[] items;
        public Vector3[] initialScales;

        private float[] scales;
        private ushort[] itemIds;
        private int itemsCount;
        [UdonSynced] private float[] syncedScales;
        [UdonSynced] private ushort[] syncedItemIds;

        private void Start()
        {
            scales = new float[16];
            itemIds = new ushort[16];
            itemsCount = 0;
        }

        public int GetIdForItem(GameObject obj)
        {
            for (int i = 0; i < items.Length; i++)
                if (items[i] == obj)
                {
                    // Debug.Log($"<dlt> id for item {obj.name} is {i}");
                    return i;
                }
            return -1;
        }

        public void SetScale(int id, Vector3 currentLocalScale)
        {
            if (id == -1)
                return;
            float scale = currentLocalScale.x / initialScales[id].x;
            // Debug.Log($"<dlt> scale: {scale} currentLocalScale.x: {currentLocalScale.x} initialScales[id].x: {initialScales[id].x}");
            ushort shortId = (ushort)id;
            for (int i = 0; i < itemsCount; i++)
            {
                if (itemIds[i] == shortId)
                {
                    scales[i] = scale;
                    return;
                }
            }
            if (itemsCount == itemIds.Length)
            {
                var newItemIds = new ushort[itemsCount * 2];
                itemIds.CopyTo(newItemIds, 0);
                itemIds = newItemIds;
                var newScales = new float[itemsCount * 2];
                scales.CopyTo(newScales, 0);
                scales = newScales;
            }
            itemIds[itemsCount] = shortId;
            scales[itemsCount] = scale;
            itemsCount++;
        }

        public override void OnPreSerialization()
        {
            if (syncedItemIds == null || syncedItemIds.Length != itemsCount)
            {
                syncedItemIds = new ushort[itemsCount];
                syncedScales = new float[itemsCount];
            }
            for (int i = 0; i < itemsCount; i++)
            {
                syncedItemIds[i] = itemIds[i];
                syncedScales[i] = scales[i];
            }
        }

        public override void OnDeserialization()
        {
            if (syncedItemIds == null) // just in case
                return;
            int count = 16;
            while (count < syncedItemIds.Length)
                count *= 2;
            itemIds = new ushort[count];
            scales = new float[count];
            syncedItemIds.CopyTo(itemIds, 0);
            syncedScales.CopyTo(scales, 0);
            itemsCount = syncedItemIds.Length;
            for (int i = 0; i < syncedItemIds.Length; i++)
            {
                ushort id = syncedItemIds[i];
                items[id].transform.localScale = initialScales[id] * syncedScales[i];
                // Debug.Log($"<dlt> initialScales[id]: {initialScales[id]} syncedScales[i]: {syncedScales[i]}");
            }
        }
    }

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(ItemScaleManager))]
    public class ItemScaleManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ItemScaleManager target = this.target as ItemScaleManager;
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;
            EditorGUILayout.Space();
            base.OnInspectorGUI(); // draws public/serializable fields
            if (GUILayout.Button(new GUIContent("Find all Items")))
            {
                List<GameObject> itemsList = new List<GameObject>();
                foreach (GameObject obj in target.parentsForObjectPools)
                {
                    foreach (VRCObjectPool op in obj.GetComponentsInChildren<VRCObjectPool>())
                    {
                        itemsList.AddRange(op.Pool);
                    }
                }
                target.items = itemsList.ToArray();
                target.initialScales = itemsList.Select(go => go.transform.localScale).ToArray();
                target.ApplyProxyModifications();
                EditorUtility.SetDirty(target);
            }
        }
    }
    #endif
}
