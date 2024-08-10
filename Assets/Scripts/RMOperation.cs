using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class RMOperation
{
    [NonSerialized]
    public int ReferenceId = 0;

    public bool IsActive(bool ignoreHierarchy = false) =>
        active &&
        (IsGroup
        || (operationVolume != null &&
            operationVolume.enabled &&
            (ignoreHierarchy || operationVolume.gameObject.activeInHierarchy)));

    private bool HasOperationVolume => operationVolume != null;
    private bool HasToShowVolume => operationType != RMOperationType.Group;
    public bool IsGroup => operationType == RMOperationType.Group;
    private bool HasToShowOperations => IsGroup;
    private Color GetGUIColor => active ? Color.white : Color.gray;

    public static string GetPathToObject(Transform t)
    {
        return !(t.parent is null) ? $"{GetPathToObject(t.parent)}/" + t.name : t.name;
    }

    private string ObjectPath => operationVolume != null 
            ? $"{GetPathToObject(operationVolume.transform)} ({operationVolume.volumeType})"
            : "-";

    [SerializeReference, ShowIf("HasToShowVolume"), Title("$ObjectPath"), PropertyOrder(1), GUIColor("GetGUIColor")]
    public RMObjectComponent operationVolume;

    [ShowInInspector, PropertyOrder(3), GUIColor("GetGUIColor")]
    private RMOperationType OperationType
    {
        get => operationType;
        set
        {
            operationType = value;
            if (operationType == RMOperationType.Group) operations = operations ?? new List<RMOperation>();
            else if (operations != null && operations.Count == 0) operations = null;
        }
    }

    [SerializeField, HideInInspector]
    private RMOperationType operationType;

    [ShowIf("HasToShowOperations"), PropertyOrder(4), GUIColor("GetGUIColor")]
    public List<RMOperation> operations;

    [Range(0, 1)]
    [HideIf("HasToShowOperations"), PropertyOrder(5), GUIColor("GetGUIColor")]
    public float operationBlend = 1.0f;

    [Range(0, 2)]
    [HideIf("HasToShowOperations"), PropertyOrder(6), GUIColor("GetGUIColor")]
    public float operationSoftness = 0.0f;

    [PropertyOrder(7), GUIColor("GetGUIColor")]
    public bool active = true;

    public RMOperation(RMObjectComponent volumeComponent) : this(null as RMOperation[]) => operationVolume = volumeComponent;
    public RMOperation(params RMOperation[] newOperations)
    {
        operations = null;
        operationVolume = null;
        operationBlend = 1.0f;
        operationSoftness = 0.0f;
        operationType = RMOperationType.Add;

        if (newOperations != null)
        {
            operationType = RMOperationType.Group;
            operations = new List<RMOperation>(newOperations);
        }
    }

    public static void DoInAllOperations(RMOperation operation, Action<RMOperation> action, bool onlyDoOnActive = true)
    {
        if (onlyDoOnActive && !operation.active) return;
        action(operation);
        if (operation.operations == null) return;
        foreach (var childOperation in operation.operations)
        {
            DoInAllOperations(childOperation, action);
        }
    }

    public void GetBufferData(List<RMOperationData> bufferData, bool ignoreHierarchy = false)
    {
        if (!IsActive(ignoreHierarchy)) return;
        if (operationType == RMOperationType.Group)
        {
            bufferData.Add(new RMOperationData()
            {
                operationType = operationType,
                operationInfo = operations.Count(o => o.IsActive(ignoreHierarchy)),
                operationBlend = operationBlend,
                operationSoftness = operationSoftness
            });

            foreach (var operation in operations) operation.GetBufferData(bufferData, ignoreHierarchy);
        }
        else
        {
            int referenceId = HasOperationVolume ? operationVolume.ReferenceId : 0;
            bufferData.Add(new RMOperationData()
            {
                operationType = operationType,
                operationInfo = referenceId,
                operationBlend = operationBlend,
                operationSoftness = operationSoftness
            });
        }
    }
}