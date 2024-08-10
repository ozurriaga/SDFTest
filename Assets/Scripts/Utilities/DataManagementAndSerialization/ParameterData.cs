using System;

[Serializable]
public class ParameterData<T> : DataContainer<T>, IParameterData where T : struct
{
    public int AddToBuffer()
    {
        return BufferData<T>.Add(data);
    }
}