using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.AssetImporters;
using UnityEngine;

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class BufferDataInfoAttribute : System.Attribute
{
    public Type DataType { get; private set; }
    public int ByteSize { get; set; }
    public int OffsetIndex { get; set; }
    public string DataName { get; set; }

    public BufferDataInfoAttribute() => ByteSize = -1;
    public BufferDataInfoAttribute(int size) => ByteSize = size;
    public BufferDataInfoAttribute(string name) : this() => DataName = name;
    public BufferDataInfoAttribute(Type type, int size) : this(size) => DataType = type;
    public BufferDataInfoAttribute(Type type, string name) : this(name) => DataType = type;
}

public interface IBufferData
{
    //public void GetBufferData(out ComputeBuffer computeBuffer, out int count);
    public int Count { get; }
    public void Clear();
    void SetDataToBuffer(ComputeBuffer computeBuffer, int index = 0);
}

public class BufferData<T> : IBufferData where T : struct
{
    public static BufferData<T> Instance => instance ?? (instance = new BufferData<T>());
    private static BufferData<T> instance;

    private List<T> elements = new List<T>();

    public T this[int index] { get => elements[index]; set => elements[index] = value; }

    public static int Add(T element)
    {
        RMBuffersManagerComponent.Instance.SetTypeBuffer(typeof(T), Instance);
        return Instance.AddElement(element);
    }

    private int AddElement(T element)
    {
        int pos = elements.Count;
        elements.Add(element);
        return pos;
    }

    public int Count => elements.Count;
    public void Clear() => elements.Clear();

    public void SetDataToBuffer(ComputeBuffer computeBuffer, int index = 0)
    {
        computeBuffer.SetData(elements, 0, index, Count);
    }
}

/*
[BufferDataInfo(typeof(Vector2),    "float2",   ByteSize = 2 * 4, OffsetIndex = 0)]
[BufferDataInfo(typeof(Vector3),    "float3",   ByteSize = 3 * 4)]
[BufferDataInfo(typeof(Vector4),    "float4",   ByteSize = 4 * 4)]
[BufferDataInfo(typeof(Color),      "float4",   ByteSize = 4 * 4, OffsetIndex = 1)]
[BufferDataInfo(typeof(Matrix4x4),  "float4x4", ByteSize = 4 * 4 * 4, OffsetIndex = 2)]
public static class RMBufferHelpers
{
    // STORED BUFFER INFORMATION
    private static Dictionary<Type, string> typeToBuffer = new Dictionary<Type, string>();
    private static Dictionary<Type, int> typeToBufferOffset = new Dictionary<Type, int>();
    private static List<int> bufferOffsets;
    private static Dictionary<string, int> bufferSize = new Dictionary<string, int>();

    static RMBufferHelpers()
    {
        int maxOffsetIndex = 0;
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsDefined(typeof(BufferDataInfoAttribute)));
        foreach (Type type in types)
        {
            foreach (BufferDataInfoAttribute attribute in type.GetCustomAttributes<BufferDataInfoAttribute>())
            {
                string bufferName = attribute.DataName ?? type.Name;
                int bufferSize = attribute.ByteSize;
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
                if (!RMBufferHelpers.bufferSize.TryAdd(bufferName, bufferSize))
                {
                    int maxSize = Math.Max(bufferSize, RMBufferHelpers.bufferSize[bufferName]);
                    int minSize = Math.Min(bufferSize, RMBufferHelpers.bufferSize[bufferName]);
                    if (minSize != -1 && minSize != maxSize)
                    {
                        Debug.LogError($"DUPLICATED BUFFERDATA SIZE FOR TYPE: {currentType.Name}({bufferName}) when trying to set to {bufferSize}");
                    }
                    RMBufferHelpers.bufferSize[bufferName] = maxSize;
                }
            }
        }
        bufferOffsets = new List<int>();
        while (bufferOffsets.Count <= maxOffsetIndex) bufferOffsets.Add(0);
    }

    public static int GetByteSize(Type type)
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

    public static int CalculateByteSize(Type type)
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
    private static Dictionary<Type, IBufferData> bufferedTypes = new Dictionary<Type, IBufferData>();
    private static Dictionary<string, ComputeBuffer> buffers = new Dictionary<string, ComputeBuffer>();

    internal static void SetTypeBuffer(Type type, IBufferData bufferData) => bufferedTypes.TryAdd(type, bufferData);

    
    private static Dictionary<string, (int Index, int Count)> bufferCount = new Dictionary<string, (int, int)>();
    public static void SetBuffers(Material rayMarchingMaterial)
    {
        bufferCount.Clear();
        foreach (Type type in bufferedTypes.Keys)
        {
            string name = typeToBuffer.ContainsKey(type) ? typeToBuffer[type] : type.Name;
            int count = bufferedTypes[type].Count;
            if (bufferCount.TryGetValue(name, out var previousCount))
            {
                bufferCount[name] = (Index : 0, Count: previousCount.Count + count);
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
            bufferCount[name] = (Index: bufferCount[name].Index + bufferedTypes[type].Count, Count : bufferCount[name].Count);
        }

        foreach (string name in buffers.Keys)
        {
            rayMarchingMaterial.SetBuffer($"_RMParams_{name}", buffers[name]);
            rayMarchingMaterial.SetInt($"_RMParams_{name}Count", bufferCount[name].Count);
        }

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

    public static void Clear()
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
}*/
