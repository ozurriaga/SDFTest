using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RMObjectsManager : MonoBehaviour
{
    const int ROOT_OBJECT_PARENT_INDEX = -1;

    public List<RMObjectComponent> objectComponents = new List<RMObjectComponent>();
    public List<RMOperationGroupComponent> operationGroupComponents = new List<RMOperationGroupComponent>();

    public static RMObjectsManager Instance => instance ?? new RMObjectsManager();
    private static RMObjectsManager instance;

    public bool HasToRefresh { get; internal set; }

    public int SceneStartObject { get; set; }

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

    public RMObjectsManager()
    {
        instance = this;
    }

    void Update()
    {
        if (HasToRefresh)
        {
            RefreshObjectsList();
        }
    }

    private void RefreshObjectsList()
    {
        operationGroupComponents.Clear();
        foreach (var rootGO in gameObject.scene.GetRootGameObjects())
        {
            foreach (var operationtComponent in rootGO.GetComponentsInChildren<RMOperationGroupComponent>())
            {
                if (operationtComponent.isActiveAndEnabled && operationtComponent.gameObject.activeInHierarchy)
                {
                    operationGroupComponents.Add(operationtComponent);
                }
            }
        }

        objectComponents.Clear();
        foreach (var operationGroup in operationGroupComponents)
        {
            RMOperationGroupComponent.ExtractGameObjectsFromAllOperations(objectComponents, operationGroup);
        }

        foreach (var prefabPath in RMLibraryManagerComponent.Instance.prefabs.Keys)
        {
            var currentLibraryOperation = RMLibraryManagerComponent.Instance.prefabs[prefabPath];
            RMOperationGroupComponent.ExtractGameObjectsFromAllOperations(objectComponents, currentLibraryOperation, true);
        }

        HasToRefresh = false;
    }

    public void FillObjectsAndOperationsDataBuffers()
    {
        FillObjectsAndOperationsDataBuffersFromLibrary();
        SceneStartObject = BufferData<RMOperationData>.Add(RMOperationGroupComponent.GetEmptyObjectBufferData());
        BufferData<RMVolumeData>.Add(RMObjectComponent.GetEmptyObjectBufferData());
        FillObjectsAndOperationsDataBuffersFromScene();
    }

    public void FillObjectsAndOperationsDataBuffersFromLibrary()
    {
        foreach (var prefabPath in RMLibraryManagerComponent.Instance.prefabs.Keys)
        {
            var currentLibraryOperation = RMLibraryManagerComponent.Instance.prefabs[prefabPath];

            foreach (var objectComponent in currentLibraryOperation.GetComponentsInChildren<RMObjectComponent>())
            {
                var bufferData = objectComponent.GetBufferData();
                int currentObjectIndex = BufferData<RMVolumeData>.Add(bufferData);
                objectComponent.ReferenceId = currentObjectIndex;
            }

            bool referenceSet = false;
            foreach (var bufferOperationData in currentLibraryOperation.GetBufferData(true))
            {
                int currentOperationIndex = BufferData<RMOperationData>.Add(bufferOperationData);
                if (!referenceSet)
                {
                    currentLibraryOperation.ReferenceId = currentOperationIndex++;
                    referenceSet = true;
                }
            }
        }
    }

    public void FillObjectsAndOperationsDataBuffersFromScene()
    { 
        SceneStartObject = BufferData<RMOperationData>.Add(new RMOperationData() {operationType = RMOperationType.Group, operationInfo = operationGroupComponents.Count, operationBlend = 1});
        foreach (var operationGroupComponent in operationGroupComponents)
        {
            bool referenceSet = false;
            foreach (var bufferOperationData in operationGroupComponent.GetBufferData())
            {
                int currentOperationIndex = BufferData<RMOperationData>.Add(bufferOperationData);
                if (!referenceSet)
                {
                    operationGroupComponent.ReferenceId = currentOperationIndex++;
                    referenceSet = true;
                }
            }
        }

        foreach (var objectComponent in objectComponents)
        {
            var bufferData = objectComponent.GetBufferData();
            int currentObjectIndex = BufferData<RMVolumeData>.Add(bufferData);
            objectComponent.ReferenceId = currentObjectIndex;
        }
    }

    [Button("Debug Process")]
    private void DebugProcess()
    {
        string DebugLog = "";

        FillObjectsAndOperationsDataBuffers();

        Stack<int> branchElements = new Stack<int>();
        Stack<float> branchResults = new Stack<float>();
        Stack<int> firstOperationIndex = new Stack<int>();
        Stack<int> referenceIterator = new Stack<int>();
        Stack<int> referenceElements = new Stack<int>();
        
        float branchResult = float.MaxValue;
        bool hasToPushFirstOperation = false;

        DebugLog+="\n"+("- START -");
        for (int i = RMObjectsManager.Instance.SceneStartObject; i < BufferData<RMOperationData>.Instance.Count; i++)
        {
            DebugLog += "\n" + ($"NEXT - {i}");

            if (branchElements.Count > 0)
            {
                branchElements.Push(branchElements.Pop() - 1);
                DebugLog+="\n"+($"branchElements[{branchElements.Count-1}]-- -> {branchElements.Peek()}");
            }

            if (referenceElements.Count != 0)
            {
                referenceElements.Push(referenceElements.Pop() - 1);
                DebugLog += "\n" + ($"referenceElements[{referenceElements.Count - 1}]-- -> {referenceElements.Peek()}");
            }

            RMOperationData operationData = BufferData<RMOperationData>.Instance[i];
            if (operationData.operationType == RMOperationType.Group)
            {
                DebugLog+="\n"+($"OPERATION {i} is group");
                if (operationData.operationInfo > 0)
                {
                    branchElements.Push(operationData.operationInfo);
                    DebugLog+="\n"+($"PUSH branchElements {branchElements.Count-1} -> {operationData.operationInfo}");
                    branchResults.Push(branchResult);
                    DebugLog+="\n"+($"PUSH branchResults {branchResults.Count-1} -> branchResult {branchResult}");
                    branchResult = float.MaxValue;
                    hasToPushFirstOperation = true;

                    if (referenceElements.Count != 0)
                    {
                        referenceElements.Push(operationData.operationInfo + referenceElements.Pop());
                        DebugLog+="\n"+($"referenceElements[{referenceElements.Count-1}] += {operationData.operationInfo} -> {referenceElements.Peek()}");
                    }
                }
            }
            else if (operationData.operationInfo < 0)
            {
                DebugLog+="\n"+($"OPERATION {i} is reference");
                referenceIterator.Push(i);
                DebugLog+="\n"+($"PUSH referenceIterator {referenceIterator.Count-1} -> {i}");
                referenceElements.Push(1);
                DebugLog+="\n"+($"PUSH referenceElements {referenceElements.Count-1} -> {1}");
                i = -operationData.operationInfo - 1;
            }
            else
            {
                RMVolumeData objectData = BufferData<RMVolumeData>.Instance[operationData.operationInfo];
                DebugLog+="\n"+($"OPERATION {i} is volume: {objectData.volumeType}");

                if (objectData.volumeType == RMVolumeType.Reference)
                {
                    DebugLog += "\n" + ($"OBJECT {i} is reference {objectData.paramsId} of object {BufferData<ReferenceParameters>.Instance[objectData.paramsId].referenceId}");
                    referenceIterator.Push(i);
                    DebugLog += "\n" + ($"PUSH referenceIterator {referenceIterator.Count - 1} -> {i}");
                    referenceElements.Push(1);
                    DebugLog += "\n" + ($"PUSH referenceElements {referenceElements.Count - 1} -> {1}");
                    i = BufferData<ReferenceParameters>.Instance[objectData.paramsId].referenceId - 1;
                    continue;
                }

                float currentResult = operationData.operationInfo*10.0f;
                DebugLog+="\n"+($"Calculate currentResult {currentResult}");

                if (hasToPushFirstOperation)
                {
                    DebugLog+="\n"+($"Combine branchResult = current {currentResult}");
                    hasToPushFirstOperation = false;
                    firstOperationIndex.Push(i);
                    DebugLog+="\n"+($"PUSH firstOperationIndex {firstOperationIndex.Count-1} -> {i}");
                    branchResult = currentResult;
                }
                else
                {
                    //branchResult = Combine(branchResult, currentResult, operationData);
                    branchResult = Mathf.Min(branchResult, currentResult);
                    DebugLog+="\n"+($"Combine branchResult -> current: {branchResult} OP: {operationData}");
                }

                while (branchElements.Count > 0 && branchElements.Peek() <= 0)
                {
                    float previousBranchResults = branchResults.Pop();
                    branchResult = Mathf.Min(previousBranchResults, branchResult);
                    DebugLog+="\n"+($"POP branchResults {branchResults.Count} -> {previousBranchResults}");
                    if (referenceElements.Count == 0 || referenceElements.Peek() > 0)
                    {
                        if (firstOperationIndex.Count > 0)
                        {
                            int currentFirstOperationIndex = firstOperationIndex.Pop();
                            DebugLog+="\n"+($"POP firstOperationIndex {firstOperationIndex.Count} -> {currentFirstOperationIndex}");
                            DebugLog+="\n"+($"Combine POP(branchResults) -> branchResult {branchResult} (FO[{currentFirstOperationIndex}]): FOP: {BufferData<RMOperationData>.Instance[currentFirstOperationIndex]}");
                        } else
                        {
                            RMOperationData operation = new RMOperationData() { operationType = RMOperationType.Add, operationBlend = 1, operationSoftness = 0, operationInfo = 0 };
                            DebugLog+="\n"+($"Combine POP(branchResults) -> branchResult {branchResult} (FO - not found, falling to default ADD): {operation}");
                            DebugLog += "\n" + ($"SHOULD ONLY HAPPEN ONCE AT THE END");
                        }
                        //branchResult = Combine(STACK_POP(branchResults), branchResult, SB(RMOperationData)[STACK_POP(firstOperationIndex)]);
                    }
                    else
                    {
                        i = referenceIterator.Pop();
                        DebugLog+="\n"+($"i = POP referenceIterator {referenceIterator.Count} -> {i}");
                        operationData = BufferData<RMOperationData>.Instance[i];
                        DebugLog+="\n"+($"Combine POP(branchResults) -> branchResult {branchResult} (UPDATED - OP[{i}]): {operationData}");
                        //branchResult = Combine(STACK_POP(branchResults), branchResult, SB(RMOperationData)[i]);
                        DebugLog+="\n"+($"POP referenceElements {referenceElements.Count} -> {referenceElements.Pop()}");
                    }
                    DebugLog+="\n"+($"POP branchElements {branchElements.Count-1} -> {branchElements.Pop()}");
                }
            }
        }

        DebugLog += "\n" + ($"REST branchElements {branchElements.Count}");
        while (branchElements.Count > 0) DebugLog += "\n" + ($"branchElements[{branchElements.Count - 1}] {branchElements.Pop()}");
        DebugLog += "\n" + ($"REST branchResults {branchResults.Count}");
        while (branchResults.Count > 0) DebugLog += "\n" + ($"branchResults[{branchResults.Count - 1}] {branchResults.Pop()}");
        DebugLog += "\n" + ($"REST firstOperationIndex {firstOperationIndex.Count}");
        while (firstOperationIndex.Count > 0) DebugLog += "\n" + ($"firstOperationIndex[{firstOperationIndex.Count - 1}] {firstOperationIndex.Pop()}");
        DebugLog += "\n" + ($"REST referenceIterator {referenceIterator.Count}");
        while (referenceIterator.Count > 0) DebugLog += "\n" + ($"referenceIterator[{referenceIterator.Count - 1}] {referenceIterator.Pop()}");
        DebugLog += "\n" + ($"REST referenceElements {referenceElements.Count}");
        while (referenceElements.Count > 0) DebugLog += "\n" + ($"referenceElements[{referenceElements.Count - 1}] {referenceElements.Pop()}");
        /*
                for (int i = 1; i < STACK_DEPTH(branchResults); i++)
                {
                    RMOperationData operation;
                    operation.type = RMOperationTypeAdd;
                    operation.blend = 1;
                    operation.softness = 0;

                    if (STACK_DEPTH(firstOperationIndex) >= i) operation = SB(RMOperationData)[STACK_NAME(firstOperationIndex)[i] - 1];
                    globalResult = Combine(globalResult, STACK_NAME(branchResults)[i], operation);
                }
                */

        Debug.Log(DebugLog);
        BufferData<RMVolumeData>.Instance.Clear();
        BufferData<RMOperationData>.Instance.Clear();
    }
}
