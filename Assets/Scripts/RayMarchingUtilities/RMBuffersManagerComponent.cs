using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

[ExecuteAlways]
public class RMBuffersManagerComponent : MonoBehaviour
{
    public static RMBuffersManagerComponent Instance => instance ?? new RMBuffersManagerComponent();
    private static RMBuffersManagerComponent instance;

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
    
    public bool HasToRefresh { get; internal set; }

    public RMBuffersManagerComponent()
    {
        instance = this;
    }

    void Update()
    {
        if (HasToRefresh)
        {
            InitBuffers();
        }
    }

    private void OnValidate()
    {
        HasToRefresh = true;
    }

    // CONFIG FILE
    public TextAsset DataDefinitionShaderFile;

    // STORED BUFFER INFORMATION
    private Dictionary<Type, string> typeToBuffer = new Dictionary<Type, string>();
    private Dictionary<Type, int> typeToBufferOffset = new Dictionary<Type, int>();
    private List<int> bufferOffsets = new List<int>();
    private Dictionary<string, int> bufferSize = new Dictionary<string, int>();

    private void InitBuffers()
    {
        Clear();
        typeToBuffer.Clear();
        typeToBufferOffset.Clear();
        bufferOffsets?.Clear();
        bufferSize.Clear();

        int maxOffsetIndex = 0;

        List<BufferDataInfoAttribute> shaderDefinedInfo = new List<BufferDataInfoAttribute>();

        if (DataDefinitionShaderFile != null)
        {
            string targetFile = AssetDatabase.GetAssetPath(DataDefinitionShaderFile);
            string DataDefinitionShaderFileText = File.ReadAllText(targetFile);
            //DataDefinitionShaderFile.GetData<string>()
            var matches = Regex.Matches(DataDefinitionShaderFileText, @"^.*?\#define (.*?)\(i\)\s*?SET_TYPE\(\s*(.+?)\s*,\s*(\d+?)\s*,.*$", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                string managedType = match.Groups[1].Value;
                string shaderType = match.Groups[2].Value;
                string offsetPos = match.Groups[3].Value;
#if RMDEBUG
                Debug.Log($"Type definition found in shader: {managedType} is in {shaderType} buffer with offset index of {offsetPos}");
#endif
                AddOrUpdateDataInfo(
                    GetTypeFromName(managedType),
                    shaderType,
                    -1,
                    int.Parse(offsetPos),
                    ref maxOffsetIndex);
            }
        }

        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsDefined(typeof(BufferDataInfoAttribute)));
        foreach (Type type in types)
        {
            foreach (BufferDataInfoAttribute attribute in type.GetCustomAttributes<BufferDataInfoAttribute>())
            {
                AddOrUpdateDataInfo(
                    attribute.DataType ?? type, 
                    attribute.DataName ?? type.Name, 
                    attribute.ByteSize, 
                    attribute.OffsetIndex, 
                    ref maxOffsetIndex);
            }
        }
        bufferOffsets = new List<int>();
        while (bufferOffsets.Count <= maxOffsetIndex) bufferOffsets.Add(0);
        
        HasToRefresh = false;
    }

    private Type GetTypeFromName(string name)
    {
        return Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name == name);
    }

    private void AddOrUpdateDataInfo(Type type, string bufferName, int currentSize, int offsetIndex, ref int maxOffsetIndex)
    {
        if (type == null)
        {
            Debug.LogError($"TYPE NOT FOUND! When trying to set to {bufferName}");
            return;
        }

        maxOffsetIndex = Math.Max(maxOffsetIndex, offsetIndex);
        if (!typeToBuffer.TryAdd(type, bufferName))
        {
            Debug.LogError($"DUPLICATED BUFFERDATA NAME FOR TYPE: {type.Name} when trying to set to {bufferName}");
        }
        if (!typeToBufferOffset.TryAdd(type, offsetIndex))
        {
            Debug.LogError($"DUPLICATED BUFFERDATA OFFSET FOR TYPE: {type.Name} when trying to set to {offsetIndex}");
        }
        if (!bufferSize.TryAdd(bufferName, currentSize))
        {
            int maxSize = Math.Max(currentSize, bufferSize[bufferName]);
            int minSize = Math.Min(currentSize, bufferSize[bufferName]);
            if (minSize != -1 && minSize != maxSize)
            {
                Debug.LogError($"DUPLICATED BUFFERDATA SIZE FOR TYPE: {type.Name}({bufferName}) when trying to set to {bufferSize}");
            }
            bufferSize[bufferName] = maxSize;
        }
    }

    /*private void InitBuffers()
    {
        Clear();
        typeToBuffer.Clear();
        typeToBufferOffset.Clear();
        bufferOffsets.Clear();
        bufferSize.Clear();
        
        int maxOffsetIndex = 0;
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsDefined(typeof(BufferDataInfoAttribute)));
        foreach (Type type in types)
        {
            foreach (BufferDataInfoAttribute attribute in type.GetCustomAttributes<BufferDataInfoAttribute>())
            {
                string bufferName = attribute.DataName ?? type.Name;
                int currentSize = attribute.ByteSize;
                int offsetIndex = attribute.OffsetIndex;
                maxOffsetIndex = Math.Max(maxOffsetIndex, offsetIndex);
                Type currentType = attribute.DataType ?? type;
                if (!typeToBuffer.TryAdd(currentType, bufferName))
                {
                    Debug.LogError($"DUPLICATED BUFFERDATA NAME FOR TYPE: {currentType.Name} when trying to set to {bufferName}");
                }
                if (!typeToBufferOffset.TryAdd(currentType, offsetIndex))
                {
                    Debug.LogError($"DUPLICATED BUFFERDATA OFFSET FOR TYPE: {currentType.Name} when trying to set to {offsetIndex}");
                }
                if (!bufferSize.TryAdd(bufferName, currentSize))
                {
                    int maxSize = Math.Max(currentSize, bufferSize[bufferName]);
                    int minSize = Math.Min(currentSize, bufferSize[bufferName]);
                    if (minSize != -1 && minSize != maxSize)
                    {
                        Debug.LogError($"DUPLICATED BUFFERDATA SIZE FOR TYPE: {currentType.Name}({bufferName}) when trying to set to {bufferSize}");
                    }
                    bufferSize[bufferName] = maxSize;
                }
            }
        }
        bufferOffsets = new List<int>();
        while (bufferOffsets.Count <= maxOffsetIndex) bufferOffsets.Add(0);
    }*/

    public int GetByteSize(Type type)
    {
        string name = type.Name;
        if (!typeToBuffer.TryAdd(type, name)) name = typeToBuffer[type];
        if (!bufferSize.ContainsKey(name))
        {
            bufferSize.Add(name, CalculateByteSize(type));
        }
        else if (bufferSize[name] < 0)
        {
            bufferSize[name] = CalculateByteSize(type);
        }
        return bufferSize[name];
    }

    public int CalculateByteSize(Type type)
    {
        if (type.IsPrimitive || type.IsEnum)
            return 4;

        if (type.IsClass || type.IsValueType)
        {
            int size = 0;
            foreach (var field in type.GetFields())
            {
                if (field.IsPublic && !field.IsLiteral && !field.IsStatic && !field.IsInitOnly)
                {
                    size += GetByteSize(field.FieldType);
                }
            }
            return size;
        }

        return 0;
    }

    // DYNAMIC DATA BUFFER CONSTRUCTION
    private Dictionary<Type, IBufferData> bufferedTypes = new Dictionary<Type, IBufferData>();
    private Dictionary<string, ComputeBuffer> buffers = new Dictionary<string, ComputeBuffer>();

    internal void SetTypeBuffer(Type type, IBufferData bufferData) => bufferedTypes.TryAdd(type, bufferData);


    private Dictionary<string, (int Index, int Count)> bufferCount = new Dictionary<string, (int, int)>();

    public void SetBuffers(Material rayMarchingMaterial)
    {
        bufferCount.Clear();
        foreach (Type type in bufferedTypes.Keys)
        {
            string name = typeToBuffer.ContainsKey(type) ? typeToBuffer[type] : type.Name;
            int count = bufferedTypes[type].Count;
            if (bufferCount.TryGetValue(name, out var previousCount))
            {
                bufferCount[name] = (Index: 0, Count: previousCount.Count + count);
            }
            else
            {
                bufferCount.Add(name, (Index: 0, Count: count));
            }
        }

        foreach (Type type in bufferedTypes.Keys)
        {
            string name = typeToBuffer.ContainsKey(type) ? typeToBuffer[type] : type.Name;
            int index = bufferCount[name].Index;
            int count = bufferCount[name].Count;
            ComputeBuffer computeBuffer;
            if (index == 0)
            {
                computeBuffer = new ComputeBuffer(count, GetByteSize(type), ComputeBufferType.Default);
                if (buffers.TryGetValue(name, out ComputeBuffer oldBuffer) && (oldBuffer != computeBuffer))
                {
                    oldBuffer.Release();
                    buffers.Remove(name);
                }
                buffers.TryAdd(name, computeBuffer);
            }
            else
            {
                computeBuffer = buffers[name];
            }

            bufferedTypes[type].SetDataToBuffer(computeBuffer, index);
            if (typeToBufferOffset.TryGetValue(type, out int offsetIndex))
            {
                bufferOffsets[offsetIndex] = bufferCount[name].Index;
            }
            bufferCount[name] = (Index: bufferCount[name].Index + bufferedTypes[type].Count, Count: bufferCount[name].Count);
        }

        foreach (string name in buffers.Keys)
        {
            rayMarchingMaterial.SetBuffer($"_RMParams_{name}", buffers[name]);
            rayMarchingMaterial.SetInt($"_RMParams_{name}Count", bufferCount[name].Count);
        }

        if (bufferOffsets.Count > 0)
        {
            ComputeBuffer offsetsComputeBuffer = new ComputeBuffer(bufferOffsets.Count, 4, ComputeBufferType.Default);
            if (buffers.TryGetValue(string.Empty, out ComputeBuffer oldOffsetBuffer) && (oldOffsetBuffer != offsetsComputeBuffer))
            {
                oldOffsetBuffer.Release();
                buffers.Remove(string.Empty);
            }
            buffers.TryAdd(string.Empty, offsetsComputeBuffer);
            offsetsComputeBuffer.SetData(bufferOffsets);

            rayMarchingMaterial.SetBuffer($"_BufferOffset", offsetsComputeBuffer);
            rayMarchingMaterial.SetInt($"_BufferOffsetCount", bufferOffsets.Count);
        }
    }

    public void Clear()
    {
        foreach (var bufferData in bufferedTypes.Values)
        {
            bufferData.Clear();
        }

        foreach (ComputeBuffer buffer in buffers.Values)
        {
            buffer?.Release();
        }

        bufferedTypes.Clear();
        buffers.Clear();
    }
}