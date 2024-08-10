using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class DataContainer<T>
{
    [HideInInspector, SerializeField]
    private CustomJsonSerializer customSerializer;

    [InlineProperty, LabelText("")]
    public T data;

    public DataContainer(T data = default(T))
    {
        customSerializer = new CustomJsonSerializer(this, "data");
        this.data = data;
    }

    public static implicit operator T(DataContainer<T> parameterData) => parameterData.data;
    public static implicit operator DataContainer<T>(T data) => new DataContainer<T>(data);
}