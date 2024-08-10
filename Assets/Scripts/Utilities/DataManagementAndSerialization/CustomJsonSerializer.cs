using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class CustomJsonSerializer : ISerializationCallbackReceiver
{
    private class DictionaryFieldNameString : Dictionary<string, String> { }

    private object target;
    private Type type;
    
    [SerializeField]
    private DictionaryFieldNameString serializeData = new DictionaryFieldNameString();

    private List<FieldInfo> fieldInfoList = new List<FieldInfo>();

    public CustomJsonSerializer(object target, params string[] fieldNames)
    {
        this.target = target;

        if (target == null) return;
        
        type = target.GetType();
        List<string> fieldNamesList = new List<string>(fieldNames);
        foreach (var fieldInfo in type.GetFields())
        {
            if (fieldNamesList.Contains(fieldInfo.Name))
            {
                fieldInfoList.Add(fieldInfo);
                serializeData.Add(fieldInfo.Name, string.Empty);
            }
        }
    }

    public void OnAfterDeserialize()
    {
        if (target == null) return;
        foreach (var fieldInfo in fieldInfoList)
        {
            fieldInfo.SetValue(target, JsonUtility.FromJson(serializeData[fieldInfo.Name], fieldInfo.FieldType));
        }
    }

    public void OnBeforeSerialize()
    {
        if (target == null) return;
        foreach (var fieldInfo in fieldInfoList)
        {
            serializeData[fieldInfo.Name] = JsonUtility.ToJson(fieldInfo.GetValue(target));
        }
    }
}