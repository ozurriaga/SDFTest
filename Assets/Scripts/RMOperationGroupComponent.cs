using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class RMOperationGroupComponent : SerializedMonoBehaviour
{
    [MenuItem("GameObject/Ray Marching/Operation Group Object", priority = 2)]
    private static void CreateObjectInHierarchy(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("New RM Operation", typeof(RMOperationGroupComponent));
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    [MenuItem("GameObject/Ray Marching/Operation Group And Volume Object", priority = 3)]
    private static void CreateComplexObjectInHierarchy(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("New RM Object/Operation", typeof(RMObjectComponent), typeof(RMOperationGroupComponent));
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    [NonSerialized]
    public int ReferenceId = 0;

    private void OnValidate()
    {
        RMObjectsManager.Instance.HasToRefresh = true;
    }

    [Button("AddAllChildren")]
    private void AddAllChildren()
    {
        foreach (var objectComponent in GetComponentsInChildren<RMObjectComponent>())
        {
            operations.Add(new RMOperation(objectComponent));
        }
    }

    [Button("AddAllChildrenWithGroups")]
    private void AddAllChildrenWithGroupsFromTransform()
    {
        AddAllChildrenWithGroupsFromTransform(operations, transform, this);
    }

    [Button("Clear")]
    private void Clear() => operations.Clear();

    public List<RMOperation> operations = new List<RMOperation>();

    public List<RMOperationData> GetBufferData(bool ignoreHierarchy = false)
    {
        List<RMOperationData> bufferData = new List<RMOperationData>();
        RMOperation tempOperation = new RMOperation(operations.ToArray());
        tempOperation.GetBufferData(bufferData, ignoreHierarchy);
        return bufferData;
    }

    public static void ExtractGameObjectsFromAllOperations(List<RMObjectComponent> extractedObjects, RMOperationGroupComponent operationGroup, bool ignoreActive = false)
    {
        if (operationGroup == null || ((!operationGroup.isActiveAndEnabled || !operationGroup.gameObject.activeInHierarchy) && !ignoreActive)) return;
        foreach (var operation in operationGroup.operations)
        {
            ExtractGameObjectsFromAllOperations(extractedObjects, operation);
        }
    }

    public static void ExtractGameObjectsFromAllOperations(List<RMObjectComponent> extractedObjects, RMOperation operation, bool ignoreHierarchy = false)
    {
        if (!operation.IsActive(ignoreHierarchy))
        {
            return;
        } 
        else if (operation.IsGroup)
        {
            foreach (var childOperation in operation.operations)
            {
                ExtractGameObjectsFromAllOperations(extractedObjects, childOperation);
            }
        } 
        else if (!extractedObjects.Contains(operation.operationVolume))
        {
            extractedObjects.Add(operation.operationVolume);
        }
    }

    public static RMOperationData GetEmptyObjectBufferData() => new RMOperationData() { operationBlend = 0, operationInfo = 0, operationType = 0, operationSoftness = 0 };

    private static void AddAllChildrenWithGroupsFromTransform(List<RMOperation> operationsList, Transform currentObject, RMOperationGroupComponent rootOperation = null)
    {
        RMOperation groupToAdd = null;
        RMObjectComponent currentVolume = currentObject.GetComponent<RMObjectComponent>();
        if (currentVolume != null)
        {
            operationsList.Add(new RMOperation(currentVolume));
        }

        for (int i = 0; i < currentObject.transform.childCount; i++)
        {
            var child = currentObject.transform.GetChild(i);
            if (currentVolume != null && child.GetComponent<RMObjectComponent>() != null)
            {
                groupToAdd = groupToAdd ?? new RMOperation(new RMOperation[]{ });
                if (!operationsList.Contains(groupToAdd)) operationsList.Add(groupToAdd);
                AddAllChildrenWithGroupsFromTransform(groupToAdd.operations, child, rootOperation);
            }
            else
            {
                AddAllChildrenWithGroupsFromTransform(operationsList, child, rootOperation);
            }
        }
    }
}
