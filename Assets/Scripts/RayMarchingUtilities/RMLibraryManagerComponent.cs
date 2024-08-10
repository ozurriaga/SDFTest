using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways, Serializable]
public class RMLibraryManagerComponent : SerializedMonoBehaviour
{
    public static RMLibraryManagerComponent Instance => instance ?? new RMLibraryManagerComponent();
    private static RMLibraryManagerComponent instance;

    public bool HasToRefresh { get; internal set; }

    public string prefabsPath = "";

    [OdinSerialize, TableList(AlwaysExpanded = true), ]
    public Dictionary<string, RMOperationGroupComponent> prefabs = new Dictionary<string, RMOperationGroupComponent>();
    
    // Inspector utilities
    [Button("Set Singleton Instance")]
    private void SetSingletonInstance()
    {
        instance = this;
    }

    [Button("Force Refresh")]
    private void ForceRefresh()
    {
        HasToRefresh = true;
    }

    public RMLibraryManagerComponent()
    {
        instance = this;
    }

    void Update()
    {
        if (HasToRefresh)
        {
            GeneratePrefabsLibrary();
        }
    }

    private void GeneratePrefabsLibrary()
    {
        var loadedPrefabs = Resources.LoadAll<RMOperationGroupComponent>(prefabsPath);
        prefabs.Clear();
        foreach (var prefab in loadedPrefabs)
        {
            prefabs.Add(AssetDatabase.GetAssetPath(prefab.GetInstanceID()), prefab);
        }
    }
}
